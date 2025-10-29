using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Novibet.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CurrencyRates_Currency_Date",
                table: "CurrencyRates");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Wallets",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Wallets",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "CurrencyRates",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Wallets");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Wallets",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "CurrencyRates",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyRates_Currency_Date",
                table: "CurrencyRates",
                columns: new[] { "Currency", "Date" },
                unique: true);
        }
    }
}
