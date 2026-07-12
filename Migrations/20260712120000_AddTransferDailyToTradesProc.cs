using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZerodaTrade.Migrations
{
    public partial class AddTransferDailyToTradesProc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql = @"CREATE PROCEDURE TransferDailyToTrades
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Trades (TradeId, FillTime, Type, Instrument, CNC, Qty, AvgPrice, CreatedDate, ModifiedDate)
    SELECT TradeId, FillTime, Type, Instrument, CNC, Qty, AvgPrice, CreatedDate, ModifiedDate
    FROM DailyTrades;
END";
            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS TransferDailyToTrades");
        }
    }
}
