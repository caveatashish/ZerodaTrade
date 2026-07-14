using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ZerodaTrade.Data;
using ZerodaTrade.Models;
using System.Linq;

namespace ZerodaTrade.Controllers
{
    public class TradeReconciliationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TradeReconciliationController> _logger;
        private readonly IConfiguration _configuration;

        public TradeReconciliationController(ApplicationDbContext context, ILogger<TradeReconciliationController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        // GET: /TradeReconciliation
        public async Task<IActionResult> Index(string? instrument, int page = 1, int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 20;

            var query = _context.Trades.AsQueryable();

            if (!string.IsNullOrWhiteSpace(instrument))
            {
                query = query.Where(d => d.Instrument == instrument);
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var trades = await query
                .OrderByDescending(d => d.FillTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var instruments = await _context.Trades
                .Select(d => d.Instrument)
                .Distinct()
                .OrderBy(i => i)
                .ToListAsync();

            var model = new TradeListViewModel
            {
                Trades = trades,
                Instruments = instruments,
                SelectedInstrument = instrument,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalItems = totalItems
            };

            return View(model);
        }

        // GET: /TradeReconciliation/Edit/{id}
        // Accepts either int Id (primary key) or legacy TradeId (long)
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            // Try parse as primary key (int)
            if (int.TryParse(id, out var intId) && intId > 0)
            {
                var tradeById = await _context.Trades.FindAsync(intId);
                if (tradeById != null)
                    return View(tradeById);
            }

            // Try parse as legacy TradeId (long)
            if (long.TryParse(id, out var legacyTradeId))
            {
                var tradeByTradeId = await _context.Trades.FirstOrDefaultAsync(t => t.TradeId == legacyTradeId);
                if (tradeByTradeId != null)
                    return View(tradeByTradeId);
            }

            return NotFound();
        }

        // POST: /TradeReconciliation/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,TradeId,FillTime,Type,Instrument,CNC,Qty,AvgPrice")] Trade updated)
        {
            if (updated == null) return BadRequest();
            if (id != updated.Id) return BadRequest();



            // Resolve Script and ScriptId from the provided Instrument so server-side model validation passes
            if (!string.IsNullOrWhiteSpace(updated.Instrument))
            {
                var instrumentNormalized = updated.Instrument.Trim();

                // Read optional mappings from configuration: InstrumentScriptMap: { "Instrument": "ScriptNameOrId" }
                Dictionary<string, string>? mappings = _configuration.GetSection("InstrumentScriptMap").Get<Dictionary<string, string>>();

                string? mappedScriptValue = null;
                if (mappings != null)
                {
                    // Try direct key then case-insensitive match
                    if (!mappings.TryGetValue(instrumentNormalized, out mappedScriptValue))
                    {
                        var kv = mappings.FirstOrDefault(k => k.Key.Equals(instrumentNormalized, StringComparison.OrdinalIgnoreCase));
                        if (!string.IsNullOrEmpty(kv.Key)) mappedScriptValue = kv.Value;
                    }
                }

                ZerodaTrade.Models.Script? scriptEntity = null;

                if (!string.IsNullOrWhiteSpace(mappedScriptValue))
                {
                    // If mapping is numeric treat as Id, otherwise treat as script Name
                    if (int.TryParse(mappedScriptValue, out var mappedId))
                    {
                        scriptEntity = await _context.Scripts.FindAsync(mappedId);
                    }
                    else
                    {
                        var mappedName = mappedScriptValue.Trim();
                        scriptEntity = await _context.Scripts.FirstOrDefaultAsync(s => s.Name.ToLower() == mappedName.ToLower());
                    }
                }

                // If no mapping or mapping failed, fallback to matching Instrument -> Script by name
                if (scriptEntity == null)
                {
                    scriptEntity = await _context.Scripts.FirstOrDefaultAsync(s => s.Name.ToLower() == instrumentNormalized.ToLower());
                }

                if (scriptEntity != null)
                {
                    // set values on the incoming model so validation sees them
                    updated.ScriptId = scriptEntity.Id;
                    updated.Script = scriptEntity;

                    // update ModelState so the required-field error is cleared
                    ModelState.SetModelValue("Script", new Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderResult(scriptEntity.Name ?? string.Empty));
                    ModelState.SetModelValue("ScriptId", new Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderResult(scriptEntity.Id.ToString()));

                    ModelState["Script"]?.Errors.Clear();
                    ModelState["ScriptId"]?.Errors.Clear();
                    _logger.LogInformation("Resolved script '{Name}' (Id={Id}) for instrument '{Instrument}'", scriptEntity.Name, scriptEntity.Id, updated.Instrument);
                }
                else
                {
                    // If no matching script found, add a validation error so user can correct Instrument
                    ModelState.AddModelError("Script", "No script found for the selected instrument.");
                    _logger.LogWarning("No script found for instrument '{Instrument}'", updated.Instrument);
                }
            }

            // Ignore CreatedDate and ModifiedDate for validation
            ModelState.Remove("CreatedDate");
            ModelState.Remove("ModifiedDate");

       



            //if (!ModelState.IsValid)
            //{
            //    // Return the view with the incoming values so the user can correct them
            //    return View(updated);
            //}

            var existing = await _context.Trades.FindAsync(id);
            if (existing == null) return NotFound();

            // Map allowed properties from the incoming model to the tracked entity
            existing.FillTime = updated.FillTime;
            existing.Type = updated.Type;
            existing.Instrument = updated.Instrument;
            existing.CNC = updated.CNC;
            existing.Qty = updated.Qty;
            existing.AvgPrice = updated.AvgPrice;
            // Map resolved script values
            existing.ScriptId = updated.ScriptId;
            existing.Script = updated.Script;
            existing.ModifiedDate = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { instrument = existing.Instrument });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TradeExists(existing.Id))
                    return NotFound();
                else
                    throw;
            }
        }

        // GET: /TradeReconciliation/Delete/{id}
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            // Try parse as primary key (int)
            if (int.TryParse(id, out var intId) && intId > 0)
            {
                var tradeById = await _context.Trades.FindAsync(intId);
                if (tradeById != null)
                    return View(tradeById);
            }

            // Try parse as legacy TradeId (long)
            if (long.TryParse(id, out var legacyTradeId))
            {
                var tradeByTradeId = await _context.Trades.FirstOrDefaultAsync(t => t.TradeId == legacyTradeId);
                if (tradeByTradeId != null)
                    return View(tradeByTradeId);
            }

            return NotFound();
        }

        // POST: /TradeReconciliation/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var trade = await _context.Trades.FindAsync(id);
            if (trade == null) return NotFound();

            _context.Trades.Remove(trade);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TradeExists(int id)
        {
            return _context.Trades.Any(e => e.Id == id);
        }
    }
}
