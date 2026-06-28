namespace ZerodaTrade.Models
{
    public class Watchlist
    {
        public int Id { get; set; }
        public string ScriptName { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}
