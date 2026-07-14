using System.Collections.Generic;

namespace ZerodaTrade.Models
{
    public class TradeListViewModel
    {
        public IEnumerable<Trade> Trades { get; set; } = new List<Trade>();
        public IEnumerable<string> Instruments { get; set; } = new List<string>();
        public string? SelectedInstrument { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
    }
}
