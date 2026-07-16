using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZerodaTrade.Data;
using ZerodaTrade.Models;
using System.Globalization;

namespace ZerodaTrade.Controllers
{
    public class StockTradeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StockTradeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // show groups with checkboxes
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var groups = await _context.Groups.OrderBy(g => g.Name).ToListAsync();
            return View(groups);
        }

        // return scripts for group as partial
        [HttpGet]
        public async Task<IActionResult> GetStocks(int groupId)
        {
            // return only active scripts for the group
            var scripts = await _context.Scripts
                .Where(s => s.GroupId == groupId && s.Status)
                .OrderBy(s => s.Name)
                .ToListAsync();
            return PartialView("_StockList", scripts);
        }

        

        // return recent trades summary for a specific group (across all instruments in the group)
        [HttpGet]
        public async Task<IActionResult> GetRecentTradesByGroup(int groupId, int days = 10)
        {
            if (days <= 0) days = 5;
            var since = DateTime.UtcNow.Date.AddDays(-days + 1);

            // get instruments belonging to the group
            var instruments = await _context.Scripts.Where(s => s.GroupId == groupId).Select(s => s.Name).ToListAsync();

            var trades = await _context.Trades
                .Where(t => instruments.Contains(t.Instrument) && t.FillTime >= since)
                .ToListAsync();

            var summary = trades
                .GroupBy(t => new { Date = t.FillTime.Date, t.Instrument })
                .Select(g => new StockTradeSummary
                {
                    Date = g.Key.Date,
                    Instrument = g.Key.Instrument,
                    BuyQty = g.Where(x => x.Type.Equals("buy", StringComparison.OrdinalIgnoreCase)).Sum(x => x.Qty),
                    BuyTotal = g.Where(x => x.Type.Equals("buy", StringComparison.OrdinalIgnoreCase)).Sum(x => x.AvgPrice * x.Qty),
                    SellQty = g.Where(x => x.Type.Equals("sell", StringComparison.OrdinalIgnoreCase)).Sum(x => x.Qty),
                    SellTotal = g.Where(x => x.Type.Equals("sell", StringComparison.OrdinalIgnoreCase)).Sum(x => x.AvgPrice * x.Qty),
                })
                .OrderByDescending(r => r.Date)
                .ThenBy(r => r.Instrument)
                .ToList();

            foreach (var row in summary)
            {
                row.BuyAverage = row.BuyQty != 0 ? row.BuyTotal / row.BuyQty : 0;
                row.SellAverage = row.SellQty != 0 ? row.SellTotal / row.SellQty : 0;
            }

            return PartialView("_TradeTable", summary);
        }

        [HttpGet]
        public async Task<IActionResult> StockReconcile(string instrument)
        {
            if (string.IsNullOrWhiteSpace(instrument)) return BadRequest();
            var trades = await _context.Trades.Where(t => t.Instrument == instrument).OrderByDescending(t => t.FillTime).ToListAsync();
            return View(trades);
        }

        // return recent trades summary across all instruments for the last N days
        [HttpGet]
        public async Task<IActionResult> GetRecentTrades(int days = 10)
        {
            if (days <= 0) days = 5;
            var since = DateTime.UtcNow.Date.AddDays(-days + 1);

            var trades = await _context.Trades
                .Where(t => t.FillTime >= since)
                .ToListAsync();

            var summary = trades
                .GroupBy(t => new { Date = t.FillTime.Date, t.Instrument })
                .Select(g => new StockTradeSummary
                {
                    Date = g.Key.Date,
                    Instrument = g.Key.Instrument,
                    BuyQty = g.Where(x => x.Type.Equals("buy", StringComparison.OrdinalIgnoreCase)).Sum(x => x.Qty),
                    BuyTotal = g.Where(x => x.Type.Equals("buy", StringComparison.OrdinalIgnoreCase)).Sum(x => x.AvgPrice * x.Qty),
                    SellQty = g.Where(x => x.Type.Equals("sell", StringComparison.OrdinalIgnoreCase)).Sum(x => x.Qty),
                    SellTotal = g.Where(x => x.Type.Equals("sell", StringComparison.OrdinalIgnoreCase)).Sum(x => x.AvgPrice * x.Qty),
                })
                .OrderByDescending(r => r.Date)
                .ThenBy(r => r.Instrument)
                .ToList();

            foreach (var row in summary)
            {
                row.BuyAverage = row.BuyQty != 0 ? row.BuyTotal / row.BuyQty : 0;
                row.SellAverage = row.SellQty != 0 ? row.SellTotal / row.SellQty : 0;
            }

            return PartialView("_TradeTable", summary);
        }


        // return trades summary for an instrument grouped by date
        [HttpGet]
        public async Task<IActionResult> GetTrades(string instrument)
        {
            if (string.IsNullOrWhiteSpace(instrument)) return BadRequest();

            var trades = await _context.Trades
                .Where(t => t.Instrument == instrument)
                .ToListAsync();

            var summary = trades
                .GroupBy(t => t.FillTime.Date)
                .OrderByDescending(g => g.Key)
                .Select(g => new Models.StockTradeSummary
                {
                    Date = g.Key,
                    Instrument = instrument,
                    BuyQty = g.Where(x => x.Type.Equals("buy", System.StringComparison.OrdinalIgnoreCase)).Sum(x => x.Qty),
                    BuyTotal = g.Where(x => x.Type.Equals("buy", System.StringComparison.OrdinalIgnoreCase)).Sum(x => x.AvgPrice * x.Qty),
                    SellQty = g.Where(x => x.Type.Equals("sell", System.StringComparison.OrdinalIgnoreCase)).Sum(x => x.Qty),
                    SellTotal = g.Where(x => x.Type.Equals("sell", System.StringComparison.OrdinalIgnoreCase)).Sum(x => x.AvgPrice * x.Qty),
                })
                .ToList();

            // compute averages
            foreach (var row in summary)
            {
                row.BuyAverage = row.BuyQty != 0 ? row.BuyTotal / row.BuyQty : 0;
                row.SellAverage = row.SellQty != 0 ? row.SellTotal / row.SellQty : 0;
            }

            return PartialView("_TradeTable", summary);
        }

        // return detailed trades for a given instrument and date (date format yyyy-MM-dd)
        [HttpGet]
        public async Task<IActionResult> GetDayTrades(string instrument, string date)
        {
            if (string.IsNullOrWhiteSpace(instrument) || string.IsNullOrWhiteSpace(date)) return BadRequest();

            if (!DateTime.TryParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var theDate))
            {
                return BadRequest("Invalid date");
            }

            var start = theDate.Date;
            var end = start.AddDays(1);

            // Load trades for the instrument for that specific date (server-side range filter)
            var trades = await _context.Trades
                          .Where(t => t.Instrument == instrument && t.FillTime >= start && t.FillTime < end)
                          .OrderByDescending(t => t.FillTime)
                          .ToListAsync();

            // Group trades by Date part of FillTime, Type, Instrument and AvgPrice and sum Qty
            var grouped = trades
                .GroupBy(t => new { Date = t.FillTime.Date, Type = (t.Type ?? string.Empty).ToLowerInvariant(), t.Instrument, t.AvgPrice })
                .Select(g => new Trade
                {
                    // use the earliest time for the representative FillTime (keeps ordering by time)
                    FillTime = g.Min(x => x.FillTime),
                    Type = g.First().Type,
                    Instrument = g.Key.Instrument,
                    CNC = g.First().CNC,
                    Qty = g.Sum(x => x.Qty),
                    AvgPrice = g.Key.AvgPrice,
                    // keep CreatedDate as earliest created date in the group (optional)
                    CreatedDate = g.Min(x => x.CreatedDate)
                })
                .OrderByDescending(t => t.FillTime)
                .ToList();

            return PartialView("_DayTrades", grouped);
        }



    }
}
