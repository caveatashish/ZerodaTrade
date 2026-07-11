using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ZerodaTrade.Data;
using ZerodaTrade.Models;

namespace ZerodaTrade.Pages.DailyTrades
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<DailyTrade> DailyTrades { get; set; } = new List<DailyTrade>();

        public async Task OnGetAsync()
        {
            DailyTrades = await _context.DailyTrades
                .OrderByDescending(d => d.FillTime)
                .ToListAsync();
        }
    }
}
