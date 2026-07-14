using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZerodaTrade.Data;
using ZerodaTrade.Models;

namespace ZerodaTrade.Controllers
{
    public class ScriptController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ScriptController(ApplicationDbContext context)
        {
            _context = context;
        }

        // READ: List all scripts
        public async Task<IActionResult> Index()
        {
            var scripts = await _context.Scripts.Include(s => s.Group).ToListAsync();
            var groups = await _context.Groups.ToListAsync();

            ViewBag.Groups = groups; // pass to view
            return View(scripts);
        }

        // Return partial table rows for AJAX refresh
        [HttpGet]
        public async Task<IActionResult> List(string q)
        {
            var query = _context.Scripts.Include(s => s.Group).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(s => s.Name.Contains(q));
            }
            var scripts = await query.ToListAsync();
            return PartialView("_ScriptTable", scripts);
        }

        // Return scripts for a specific group as partial table rows
        [HttpGet]
        public async Task<IActionResult> ScriptsByGroup(int groupId)
        {
            var scripts = await _context.Scripts
    .Where(s => s.GroupId == groupId)
    .Include(s => s.Group)
    .OrderBy(s => s.Name)   // ascending order
    .ToListAsync();

            return PartialView("_ScriptTable", scripts);
        }

        // Return edit form as partial (loaded into modal)
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var script = await _context.Scripts.FindAsync(id);
            if (script == null) return NotFound();

            var groups = await _context.Groups.ToListAsync();
            ViewBag.Groups = groups;
            return PartialView("_Edit", script);
        }

        // Return a single table row partial for the given script id
        [HttpGet]
        public async Task<IActionResult> Row(int id)
        {
            var script = await _context.Scripts.Include(s => s.Group).FirstOrDefaultAsync(s => s.Id == id);
            if (script == null) return NotFound();
            return PartialView("_ScriptRow", script);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Script script)
        {
            // navigation property 'Group' may be null when binding from JSON; ignore it for validation
            ModelState.Remove("Group");
            if (ModelState.IsValid)
            {
                _context.Add(script);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Script created successfully" });
            }
            return Json(new { success = false, message = "Validation failed" });
        }

        [HttpPost]
        public async Task<IActionResult> Edit([FromBody] Script script)
        {
            // navigation property 'Group' may be null when binding from JSON; ignore it for validation
            ModelState.Remove("Group");
            if (ModelState.IsValid)
            {
                // load tracked entity and apply changes to avoid issues with alternate keys
                var existing = await _context.Scripts.FindAsync(script.Id);
                if (existing == null) return Json(new { success = false, message = "Script not found" });

                // update scalar properties explicitly
                existing.Name = script.Name;
                existing.Status = script.Status;
                existing.GroupId = script.GroupId;

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Script updated successfully" });
            }
            return Json(new { success = false, message = "Validation failed" });
        }

        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] int id)
        {
            var script = await _context.Scripts.FindAsync(id);
            if (script != null)
            {
                _context.Scripts.Remove(script);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Script deleted successfully" });
            }
            return Json(new { success = false, message = "Script not found" });
        }

        // show aggregated trades summary for an instrument
        [HttpGet]
        public async Task<IActionResult> Trades(string instrument)
        {
            if (string.IsNullOrWhiteSpace(instrument)) return BadRequest();

            var trades = await _context.Trades.Where(t => t.Instrument == instrument).ToListAsync();

            var summary = trades
                .GroupBy(t => t.FillTime.Date)
                .OrderByDescending(g => g.Key)
                .Select(g => new Models.StockTradeSummary
                {
                    Date = g.Key,
                    Instrument = instrument,
                    BuyQty = g.Where(x => x.Type.Equals("buy", StringComparison.OrdinalIgnoreCase)).Sum(x => x.Qty),
                    BuyTotal = g.Where(x => x.Type.Equals("buy", StringComparison.OrdinalIgnoreCase)).Sum(x => x.AvgPrice * x.Qty),
                    SellQty = g.Where(x => x.Type.Equals("sell", StringComparison.OrdinalIgnoreCase)).Sum(x => x.Qty),
                    SellTotal = g.Where(x => x.Type.Equals("sell", StringComparison.OrdinalIgnoreCase)).Sum(x => x.AvgPrice * x.Qty),
                })
                .ToList();

            foreach (var row in summary)
            {
                row.BuyAverage = row.BuyQty != 0 ? row.BuyTotal / row.BuyQty : 0;
                row.SellAverage = row.SellQty != 0 ? row.SellTotal / row.SellQty : 0;
            }

            return PartialView("_TradeTable", summary);
        }

        // get day trades for instrument/date
        [HttpGet]
        public async Task<IActionResult> GetDayTrades(string instrument, string date)
        {
            if (string.IsNullOrWhiteSpace(instrument) || string.IsNullOrWhiteSpace(date)) return BadRequest();
            if (!DateTime.TryParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var theDate))
            {
                return BadRequest("Invalid date");
            }

            var trades = await _context.Trades.Where(t => t.Instrument == instrument && t.FillTime.Date == theDate.Date).OrderBy(t => t.FillTime).ToListAsync();
            return PartialView("_DayTrades", trades);
        }

        // edit trade GET
        [HttpGet]
        public async Task<IActionResult> EditTrade(int id)
        {
            var trade = await _context.Trades.FindAsync(id);
            if (trade == null) return NotFound();
            return PartialView("_EditTrade", trade);
        }

        // edit trade POST
        [HttpPost]
        public async Task<IActionResult> EditTrade([FromBody] Models.Trade trade)
        {
            if (trade == null) return Json(new { success = false, message = "Invalid data" });
            var existing = await _context.Trades.FindAsync(trade.Id);
            if (existing == null) return Json(new { success = false, message = "Trade not found" });

            existing.Type = trade.Type;
            existing.Qty = trade.Qty;
            existing.AvgPrice = trade.AvgPrice;
            existing.CNC = trade.CNC;
            // do not change FillTime here unless desired

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTrade([FromBody] int id)
        {
            var trade = await _context.Trades.FindAsync(id);
            if (trade == null) return Json(new { success = false, message = "Trade not found" });
            _context.Trades.Remove(trade);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }

}
