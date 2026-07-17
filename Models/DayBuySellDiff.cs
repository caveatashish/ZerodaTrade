using System;

namespace ZerodaTrade.Models
{
    public class DayBuySellDiff
    {
        public DateTime Date { get; set; }
        public string Instrument { get; set; } = string.Empty;

        // Buy -> Sell
        public decimal? BuyToSell_BuyPrice { get; set; }
        public decimal? BuyToSell_SellPrice { get; set; }
        public decimal? BuyToSell_Diff { get; set; }
        public decimal? BuyToSell_Percent { get; set; }

        // Sell -> Buy
        public decimal? SellToBuy_SellPrice { get; set; }
        public decimal? SellToBuy_BuyPrice { get; set; }
        public decimal? SellToBuy_Diff { get; set; }
        public decimal? SellToBuy_Percent { get; set; }
    }
}
