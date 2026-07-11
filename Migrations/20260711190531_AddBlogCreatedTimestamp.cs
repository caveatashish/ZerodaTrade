using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZerodaTrade.Migrations
{
    /// <inheritdoc />
    public partial class AddBlogCreatedTimestamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trades_Scripts_Instrument",
                table: "Trades");

            migrationBuilder.DropIndex(
                name: "IX_Trades_Instrument",
                table: "Trades");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Scripts_Name",
                table: "Scripts");

            migrationBuilder.AddColumn<int>(
                name: "ScriptId",
                table: "Trades",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Trades_ScriptId",
                table: "Trades",
                column: "ScriptId");

            migrationBuilder.AddForeignKey(
                name: "FK_Trades_Scripts_ScriptId",
                table: "Trades",
                column: "ScriptId",
                principalTable: "Scripts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trades_Scripts_ScriptId",
                table: "Trades");

            migrationBuilder.DropIndex(
                name: "IX_Trades_ScriptId",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "ScriptId",
                table: "Trades");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Scripts_Name",
                table: "Scripts",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Trades_Instrument",
                table: "Trades",
                column: "Instrument");

            migrationBuilder.AddForeignKey(
                name: "FK_Trades_Scripts_Instrument",
                table: "Trades",
                column: "Instrument",
                principalTable: "Scripts",
                principalColumn: "Name",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
