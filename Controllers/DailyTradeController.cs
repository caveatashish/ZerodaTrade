
using Microsoft.AspNetCore.Mvc;
using ZerodaTrade.Data;
using ZerodaTrade.Models;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;


namespace ZerodaTrade.Controllers
{
    public class DailyTradeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DailyTradeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var rows = await _context.DailyTrades.OrderByDescending(d => d.CreatedDate).Take(500).ToListAsync();
            return View(rows);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ViewBag.Error = "Please select a CSV file.";
                // after import show all rows (or recently imported)
                var recent = await _context.DailyTrades.OrderByDescending(d => d.CreatedDate).Take(500).ToListAsync();
                return View(recent);
            }

            var trades = new List<DailyTrade>();
            using (var stream = file.OpenReadStream())
            using (var reader = new System.IO.StreamReader(stream))
            {
                // read header
                var headerLine = await reader.ReadLineAsync();
                // simple CSV parsing: supports quoted values
                long counter = 0;
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = ParseCsvLine(line);

                    // Expected columns (order): FillTime,Type,Instrument,CNC,Qty,AvgPrice
                    if (parts.Length < 6) continue;

                    var tradeId = Convert.ToInt64( parts[0]);

                    DateTime fillTime;
                    var dateStr = parts[1].Trim();
                    // Try dd-MM-yyyy formats first (day-month-year)
                    var dateFormats = new[] { "dd-MM-yyyy", "d-M-yyyy", "dd-MM-yyyy HH:mm:ss", "d-M-yyyy H:m:s", "dd/MM/yyyy", "d/M/yyyy" };
                    if (!DateTime.TryParseExact(dateStr, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out fillTime))
                    {
                        // fallback to general parse
                        DateTime.TryParse(dateStr, out fillTime);
                    }

                    var type = parts[2];
                    var instrument = parts[3];

                    bool? cnc = null;
                    if (!string.IsNullOrWhiteSpace(parts[4]))
                    {
                        if (bool.TryParse(parts[3], out var b)) cnc = b;
                        else if (int.TryParse(parts[3], out var iVal)) cnc = iVal != 0;
                    }

                    int qty = 0;
                    int.TryParse(parts[5], out qty);

                    decimal avgPrice = 0;
                    decimal.TryParse(parts[6], NumberStyles.Any, CultureInfo.InvariantCulture, out avgPrice);

                    counter++;
                    var trade = new DailyTrade
                    {
                        // TradeId is not DB-generated in this model; generate unique value
                        TradeId = tradeId,
                        FillTime = fillTime,
                        Type = type,
                        Instrument = instrument,
                        CNC = cnc,
                        Qty = qty,
                        AvgPrice = avgPrice,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow
                    };

                    trades.Add(trade);
                }
            }

            if (trades.Count > 0)
            {
                await _context.DailyTrades.AddRangeAsync(trades);
                await _context.SaveChangesAsync();
                ViewBag.Success = $"Imported {trades.Count} rows.";
            }
            else
            {
                ViewBag.Error = "No valid rows found in CSV.";
            }

            // after import show all rows (or recently imported)
            var all = await _context.DailyTrades.OrderByDescending(d => d.CreatedDate).Take(500).ToListAsync();
            return View(all);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TransferAndTruncate()
        {
            // check if there are rows to transfer
            var hasRows = await _context.DailyTrades.AnyAsync();
            if (!hasRows)
            {
                TempData["Error"] = "No rows in DailyTrades to transfer.";
                return RedirectToAction("Index");
            }

            try
            {
                // Execute stored procedure to transfer rows to Trades
                await _context.Database.ExecuteSqlRawAsync("EXEC TransferDailyToTrades");

                // Truncate the DailyTrades table
                await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE DailyTrades");

                TempData["Success"] = "Transferred rows to Trades and truncated DailyTrades.";
            }
            catch (Exception ex)
            {
                // log or return error
                TempData["Error"] = "Error during transfer: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // very small CSV parser supporting quoted values
        private static string[] ParseCsvLine(string line)
        {
            var parts = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (c == '"')
                {
                    // peek next char for escaped quote
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++; // skip escaped quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            parts.Add(current.ToString());
            return parts.Select(p => p.Trim()).ToArray();
        }
    }
}
