using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankCoreApi.Migrations
{
    /// <inheritdoc />
    public partial class AddHesapAuthFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Hesaplar",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SifreHash",
                table: "Hesaplar",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE "Hesaplar"
                SET "Email" = 'legacy-' || "Id"::text || '@placeholder.local'
                WHERE "Email" = '' OR "Email" IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Hesaplar_Email",
                table: "Hesaplar",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Hesaplar_Email",
                table: "Hesaplar");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Hesaplar");

            migrationBuilder.DropColumn(
                name: "SifreHash",
                table: "Hesaplar");
        }
    }
}
