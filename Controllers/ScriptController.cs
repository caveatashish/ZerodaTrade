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
        public async Task<IActionResult> List()
        {
            var scripts = await _context.Scripts.Include(s => s.Group).ToListAsync();
            return PartialView("_ScriptTable", scripts);
        }

        // Return scripts for a specific group as partial table rows
        [HttpGet]
        public async Task<IActionResult> ScriptsByGroup(int groupId)
        {
            var scripts = await _context.Scripts.Where(s => s.GroupId == groupId).Include(s => s.Group).ToListAsync();
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
    }

}
