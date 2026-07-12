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
            var scripts = await _context.Scripts.Where(s => s.GroupId == groupId).OrderBy(s => s.Name).ToListAsync();
            return PartialView("_StockList", scripts);
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
    }
}
