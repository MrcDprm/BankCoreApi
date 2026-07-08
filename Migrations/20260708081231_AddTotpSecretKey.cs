using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankCoreApi.Migrations
{
    /// <inheritdoc />
    public partial class AddTotpSecretKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TotpSecretKey",
                table: "Hesaplar",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotpSecretKey",
                table: "Hesaplar");
        }
    }
}
