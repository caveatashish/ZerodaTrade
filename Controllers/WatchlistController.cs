using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZerodaTrade.Data;
using ZerodaTrade.Models;

namespace ZerodaTrade.Controllers
{
    public class WatchlistController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WatchlistController> _logger;

        public WatchlistController(ApplicationDbContext context, ILogger<WatchlistController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var watchlists = await _context.Watchlists.OrderBy(w => w.GroupName).ThenBy(w => w.ScriptName).ToListAsync();
                return View(watchlists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving watchlists");
                return BadRequest("Error retrieving watchlists");
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ScriptName,GroupName,Description,Price,Status")] Watchlist watchlist)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    watchlist.CreatedDate = DateTime.Now;
                    watchlist.ModifiedDate = DateTime.Now;
                    _context.Add(watchlist);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating watchlist entry");
                ModelState.AddModelError("", "Error creating watchlist entry");
            }
            return View(watchlist);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var watchlist = await _context.Watchlists.FindAsync(id);
            if (watchlist == null)
            {
                return NotFound();
            }
            return View(watchlist);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ScriptName,GroupName,Description,Price,Status,CreatedDate")] Watchlist watchlist)
        {
            if (id != watchlist.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    watchlist.ModifiedDate = DateTime.Now;
                    _context.Update(watchlist);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Error updating watchlist entry");
                    ModelState.AddModelError("", "Error updating watchlist entry");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred");
                    ModelState.AddModelError("", "An error occurred");
                }
            }
            return View(watchlist);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var watchlist = await _context.Watchlists
                .FirstOrDefaultAsync(m => m.Id == id);
            if (watchlist == null)
            {
                return NotFound();
            }

            return View(watchlist);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var watchlist = await _context.Watchlists.FindAsync(id);
                if (watchlist != null)
                {
                    _context.Watchlists.Remove(watchlist);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting watchlist entry");
                return BadRequest("Error deleting watchlist entry");
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var watchlist = await _context.Watchlists
                .FirstOrDefaultAsync(m => m.Id == id);
            if (watchlist == null)
            {
                return NotFound();
            }

            return View(watchlist);
        }
    }
}
