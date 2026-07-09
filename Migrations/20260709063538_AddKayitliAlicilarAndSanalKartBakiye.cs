using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankCoreApi.Migrations
{
    /// <inheritdoc />
    public partial class AddKayitliAlicilarAndSanalKartBakiye : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Bakiye",
                table: "SanalKartlar",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "KayitliAlicilar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HesapId = table.Column<Guid>(type: "uuid", nullable: false),
                    KarsiHesapId = table.Column<Guid>(type: "uuid", nullable: false),
                    Iban = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    KayitliAd = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KayitliAlicilar", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KayitliAlicilar_HesapId_KarsiHesapId",
                table: "KayitliAlicilar",
                columns: new[] { "HesapId", "KarsiHesapId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KayitliAlicilar");

            migrationBuilder.DropColumn(
                name: "Bakiye",
                table: "SanalKartlar");
        }
    }
}
