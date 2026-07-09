using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankCoreApi.Migrations
{
    /// <inheritdoc />
    public partial class AddSanalKartlar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SanalKartlar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HesapId = table.Column<Guid>(type: "uuid", nullable: false),
                    KartAdi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AylikLimit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    KartNoSifreli = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    SonKullanmaAy = table.Column<int>(type: "integer", nullable: false),
                    SonKullanmaYil = table.Column<int>(type: "integer", nullable: false),
                    CvvSifreli = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Durum = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SanalKartlar", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SanalKartlar");
        }
    }
}
