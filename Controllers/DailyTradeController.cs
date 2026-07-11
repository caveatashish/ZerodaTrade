using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using ZerodaTrade.Data;
using ZerodaTrade.Models;

namespace ZerodaTrade.Controllers
{
    public class DailyTradeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DailyTradeController> _logger;

        public DailyTradeController(ApplicationDbContext context, ILogger<DailyTradeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "Please select a CSV file to upload.");
                return View("Upload");
            }

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "Only .csv files are allowed.");
                return View("Upload");
            }

            var imported = 0;
            var updated = 0;
            var failed = 0;
            var errors = new List<string>();

            try
            {
                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);

                string? line;
                var lineNumber = 0;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    lineNumber++;
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Skip header if detected
                    if (lineNumber == 1 && (line.Contains("Trade", StringComparison.OrdinalIgnoreCase) || line.Contains("Fill", StringComparison.OrdinalIgnoreCase)))
                        continue;

                    var parts = SplitCsvLine(line);

                    // Expecting columns: Trade ID, Fill time, Type, Instrument, CNC, Qty., Avg. Price
                    if (parts.Length < 7)
                    {
                        errors.Add($"Line {lineNumber}: Unexpected column count ({parts.Length}).");
                        failed++;
                        continue;
                    }

                    try
                    {
                        var tradeIdRaw = parts[0].Trim();
                        if (!long.TryParse(tradeIdRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var tradeId))
                        {
                            errors.Add($"Line {lineNumber}: Invalid Trade ID '{tradeIdRaw}'.");
                            failed++;
                            continue;
                        }

                        var fillTimeRaw = parts[1].Trim();
                        if (!DateTime.TryParse(fillTimeRaw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fillTime))
                        {
                            // Try common formats
                            var formats = new[] { "M/d/yyyy H:mm", "M/d/yyyy H:mm:ss", "M/d/yyyy" };
                            if (!DateTime.TryParseExact(fillTimeRaw, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out fillTime))
                            {
                                errors.Add($"Line {lineNumber}: Invalid Fill Time '{fillTimeRaw}'.");
                                failed++;
                                continue;
                            }
                        }

                        var type = parts[2].Trim();
                        var instrument = parts[3].Trim();

                        // CNC column may be empty or contain 'CNC' or 'Yes'
                        var cncRaw = parts[4].Trim();
                        bool? cnc = null;
                        if (!string.IsNullOrEmpty(cncRaw))
                        {
                            if (cncRaw.Equals("CNC", StringComparison.OrdinalIgnoreCase) || cncRaw.Equals("Yes", StringComparison.OrdinalIgnoreCase) || cncRaw == "1")
                                cnc = true;
                            else if (cncRaw.Equals("No", StringComparison.OrdinalIgnoreCase) || cncRaw == "0")
                                cnc = false;
                        }

                        var qtyRaw = parts[5].Trim();
                        if (!int.TryParse(qtyRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var qty))
                        {
                            errors.Add($"Line {lineNumber}: Invalid Qty '{qtyRaw}'.");
                            failed++;
                            continue;
                        }

                        var priceRaw = parts[6].Trim();
                        if (!decimal.TryParse(priceRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var avgPrice))
                        {
                            errors.Add($"Line {lineNumber}: Invalid AvgPrice '{priceRaw}'.");
                            failed++;
                            continue;
                        }

                        // Upsert logic
                        var existing = await _context.DailyTrades.FindAsync(tradeId);
                        if (existing != null)
                        {
                            existing.FillTime = fillTime;
                            existing.Type = type;
                            existing.Instrument = instrument;
                            existing.CNC = cnc;
                            existing.Qty = qty;
                            existing.AvgPrice = avgPrice;
                            existing.ModifiedDate = DateTime.UtcNow;
                            _context.DailyTrades.Update(existing);
                            updated++;
                        }
                        else
                        {
                            var dt = new DailyTrade
                            {
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
                            await _context.DailyTrades.AddAsync(dt);
                            imported++;
                        }
                    }
                    catch (Exception exRow)
                    {
                        _logger.LogError(exRow, "Error processing line {LineNumber}", lineNumber);
                        errors.Add($"Line {lineNumber}: {exRow.Message}");
                        failed++;
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing uploaded CSV");
                ModelState.AddModelError(string.Empty, "An error occurred while processing the CSV file.");
                return View("Upload");
            }

            TempData["UploadResult"] = $"Imported: {imported}, Updated: {updated}, Failed: {failed}";
            if (errors.Any())
                TempData["UploadErrors"] = string.Join("\n", errors);

            return RedirectToAction(nameof(Upload));
        }

        private static string[] SplitCsvLine(string line)
        {
            var fields = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    // Handle escaped quote
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++; // skip the escaped quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }

            fields.Add(sb.ToString());
            return fields.ToArray();
        }
    }
}
