using System.Data.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ZerodaTrade.Data;
using ZerodaTrade.Models;

namespace ZerodaTrade.Controllers
{
    public class ScriptStatsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ScriptStatsController> _logger;

        private const string CacheKey = "InstrumentScriptStats";

        public ScriptStatsController(ApplicationDbContext context, IMemoryCache cache, IConfiguration configuration, ILogger<ScriptStatsController> logger)
        {
            _context = context;
            _cache = cache;
            _configuration = configuration;
            _logger = logger;
        }

        // Public endpoint to force refresh cache (can be called from admin UI or background job)
        [HttpPost]
        public async Task<IActionResult> RefreshCache()
        {
            var list = await LoadFromViewAsync();
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6)
            };
            _cache.Set(CacheKey, list, options);
            return Ok(new { Count = list.Count });
        }

        // Return cached list (refreshes from DB when missing)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            if (!_cache.TryGetValue<List<ScriptStat>>(CacheKey, out var list) || list == null)
            {
                list = await LoadFromViewAsync();
                _cache.Set(CacheKey, list, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6) });
            }

            return Json(list);
        }

        // Return a single script stat from cache (or DB if cache miss)
        [HttpGet]
        public async Task<IActionResult> GetByInstrument(string instrument)
        {
            if (string.IsNullOrWhiteSpace(instrument)) return BadRequest();

            if (!_cache.TryGetValue<List<ScriptStat>>(CacheKey, out var list) || list == null)
            {
                list = await LoadFromViewAsync();
                _cache.Set(CacheKey, list, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6) });
            }

            var found = list.FirstOrDefault(s => string.Equals(s.ScriptName, instrument, StringComparison.OrdinalIgnoreCase));
            if (found == null) return NotFound();
            // return a server-rendered partial so the UI can inject it directly
            return PartialView("_ScriptStatPartial", found);
        }

        // Internal helper: read from DB view using raw SQL
        private async Task<List<ScriptStat>> LoadFromViewAsync()
        {
            var viewName = "ScriptWeightedAvgPrices";
            var result = new List<ScriptStat>();

            var sql = $"SELECT ScriptName, AvgBuyPrice, AvgSellPrice, TotalBuyQty, TotalSellQty, RowCount1, MinBuyPrice,MaxSellPrice FROM [{viewName}]";

            var conn = _context.Database.GetDbConnection();
            try
            {
                if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.CommandType = System.Data.CommandType.Text;
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var stat = new ScriptStat
                    {
                        ScriptName = reader.GetString(reader.GetOrdinal("ScriptName")),
                        AvgBuyPrice = reader.IsDBNull(reader.GetOrdinal("AvgBuyPrice")) ? 0 : reader.GetDecimal(reader.GetOrdinal("AvgBuyPrice")),
                        AvgSellPrice = reader.IsDBNull(reader.GetOrdinal("AvgSellPrice")) ? 0 : reader.GetDecimal(reader.GetOrdinal("AvgSellPrice")),
                        TotalBuyQty = reader.IsDBNull(reader.GetOrdinal("TotalBuyQty")) ? 0 : reader.GetInt32(reader.GetOrdinal("TotalBuyQty")),
                        TotalSellQty = reader.IsDBNull(reader.GetOrdinal("TotalSellQty")) ? 0 : reader.GetInt32(reader.GetOrdinal("TotalSellQty")),
                        MinBuyPrice = reader.IsDBNull(reader.GetOrdinal("MinBuyPrice")) ? 0 : reader.GetDecimal(reader.GetOrdinal("MinBuyPrice")),
                        MaxSellPrice = reader.IsDBNull(reader.GetOrdinal("MaxSellPrice")) ? 0 : reader.GetDecimal(reader.GetOrdinal("MaxSellPrice")),
                        RowCount = 1
                    };
                    result.Add(stat);
                }
            }
            finally
            {
                try { await conn.CloseAsync(); } catch { }
            }

            _logger.LogInformation("Loaded {Count} script stats from view '{ViewName}'", result.Count, viewName);
            return result;
        }
    }
}
