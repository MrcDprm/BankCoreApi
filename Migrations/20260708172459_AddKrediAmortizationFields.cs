using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankCoreApi.Migrations
{
    /// <inheritdoc />
    public partial class AddKrediAmortizationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AylikFaizOrani",
                table: "Krediler",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AylikTaksit",
                table: "Krediler",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "KrediTuru",
                table: "Krediler",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AylikFaizOrani",
                table: "Krediler");

            migrationBuilder.DropColumn(
                name: "AylikTaksit",
                table: "Krediler");

            migrationBuilder.DropColumn(
                name: "KrediTuru",
                table: "Krediler");
        }
    }
}
