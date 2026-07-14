using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZerodaTrade.Models
{
    public class Group
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(50, ErrorMessage = "Name cannot exceed 50 characters")]
        public string Name { get; set; }

        [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
        public string Description { get; set; }

        public bool Status { get; set; }

        public ICollection<Script> Scripts { get; set; } = new List<Script>();
    }

    // DTO for script statistics (backed by a DB view)
    public class ScriptStat
    {
        public string ScriptName { get; set; } = string.Empty;
        public decimal AvgBuyPrice { get; set; }
        public decimal AvgSellPrice { get; set; }
        public int TotalBuyQty { get; set; }
        public int TotalSellQty { get; set; }
        public int RowCount { get; set; }
    }
    public class Script
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string Name { get; set; } = string.Empty;
        public bool Status { get; set; }
        // Foreign key: Every script belongs to one group
        public int GroupId { get; set; }
        // Navigation property: Reference to the group
        public Group Group { get; set; }

        public ICollection<Trade> Trades { get; set; } = new List<Trade>();

    }
    public class Trade
    {
        // New numeric primary key with identity (auto-increment)
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // Legacy trade identifier (kept for compatibility) — not the EF primary key
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long TradeId { get; set; }

        [Required]
        public DateTime FillTime { get; set; }

        [Required]
        [StringLength(20)]
        public string Type { get; set; } = string.Empty; // e.g., buy/sell

        [Required]
        [StringLength(20)]
        public string Instrument { get; set; } = string.Empty;

        // CNC flag: true when "CNC" is applicable, otherwise false/null
        public bool? CNC { get; set; }

        [Required]
        public int Qty { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AvgPrice { get; set; }

        // Optional audit fields
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public int ScriptId { get; set; }
        // Navigation property: Reference to the group
        public Script Script { get; set; }
    }

    public class DailyTrade
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long TradeId { get; set; }

        [Required]
        public DateTime FillTime { get; set; }

        [Required]
        [StringLength(20)]
        public string Type { get; set; } = string.Empty; // e.g., buy/sell

        [Required]
        [StringLength(200)]
        public string Instrument { get; set; } = string.Empty;

        // CNC flag: true when "CNC" is applicable, otherwise false/null
        public bool? CNC { get; set; }

        [Required]
        public int Qty { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AvgPrice { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

    }


}


