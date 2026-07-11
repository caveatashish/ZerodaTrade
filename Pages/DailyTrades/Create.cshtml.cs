using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ZerodaTrade.Data;
using ZerodaTrade.Models;

namespace ZerodaTrade.Pages.DailyTrades
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public DailyTrade DailyTrade { get; set; } = new DailyTrade();

        public IActionResult OnGet()
        {
            DailyTrade.FillTime = DateTime.Now;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            DailyTrade.CreatedDate = DateTime.Now;
            DailyTrade.ModifiedDate = DateTime.Now;

            _context.DailyTrades.Add(DailyTrade);
            await _context.SaveChangesAsync();

            return RedirectToPage("/DailyTrades/Index");
        }
    }
}
