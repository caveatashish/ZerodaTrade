using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZerodaTrade.Migrations
{
    /// <inheritdoc />
    public partial class AddTradeIdPrimaryKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Trades",
                table: "Trades");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Trades",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Trades",
                table: "Trades",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Trades",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Trades");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Trades",
                table: "Trades",
                column: "TradeId");
        }
    }
}
