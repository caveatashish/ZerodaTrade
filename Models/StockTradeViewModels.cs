using System;

namespace ZerodaTrade.Models
{
    public class StockTradeSummary
    {
        public DateTime Date { get; set; }
        public string Instrument { get; set; }
        public int BuyQty { get; set; }
        public decimal BuyAverage { get; set; }
        public decimal BuyTotal { get; set; }
        public int SellQty { get; set; }
        public decimal SellAverage { get; set; }
        public decimal SellTotal { get; set; }
    }
}
