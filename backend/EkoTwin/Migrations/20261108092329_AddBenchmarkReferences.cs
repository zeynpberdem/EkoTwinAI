using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EkoTwin.Migrations
{
    /// <inheritdoc />
    public partial class AddBenchmarkReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BenchmarkReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    SizeClass = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Indicator = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SimpleIndicator = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    QuestionNumber = table.Column<int>(type: "integer", nullable: true),
                    MinAnswerScore = table.Column<int>(type: "integer", nullable: true),
                    Source = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenchmarkReferences", x => x.Id);
                    table.CheckConstraint("CK_BenchmarkReferences_Answer", "(\"QuestionNumber\" IS NULL AND \"MinAnswerScore\" IS NULL) OR (\"QuestionNumber\" IS NOT NULL AND \"MinAnswerScore\" BETWEEN 1 AND 5)");
                    table.CheckConstraint("CK_BenchmarkReferences_Percent", "\"Percent\" BETWEEN 0 AND 100");
                });

            migrationBuilder.InsertData(
                table: "BenchmarkReferences",
                columns: new[] { "Id", "Indicator", "IsVerified", "MinAnswerScore", "Percent", "QuestionNumber", "SimpleIndicator", "SizeClass", "Source", "Year" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-7000-8004-000000000001"), "ERP yazılımı kullanan girişim oranı", false, 4, 23.6m, 1, "işlerini birbirine bağlı tek bir programda yürütüyor", "Small", "TÜİK, Girişimlerde Bilişim Teknolojileri Kullanım Araştırması, 2025", 2025 },
                    { new Guid("00000000-0000-7000-8004-000000000002"), "ERP yazılımı kullanan girişim oranı", false, 4, 46.0m, 1, "işlerini birbirine bağlı tek bir programda yürütüyor", "Medium", "TÜİK, Girişimlerde Bilişim Teknolojileri Kullanım Araştırması, 2025", 2025 },
                    { new Guid("00000000-0000-7000-8004-000000000003"), "Ücretli bulut bilişim hizmeti kullanan girişim oranı", false, null, 16.7m, null, "programlarını internet üzerinden ücretli olarak kullanıyor", "Small", "TÜİK, Girişimlerde Bilişim Teknolojileri Kullanım Araştırması, 2026", 2026 },
                    { new Guid("00000000-0000-7000-8004-000000000004"), "Ücretli bulut bilişim hizmeti kullanan girişim oranı", false, null, 33.3m, null, "programlarını internet üzerinden ücretli olarak kullanıyor", "Medium", "TÜİK, Girişimlerde Bilişim Teknolojileri Kullanım Araştırması, 2026", 2026 },
                    { new Guid("00000000-0000-7000-8004-000000000005"), "Bilgi ve iletişim teknolojileri güvenlik önlemi alan girişim oranı", false, 2, 83.0m, 3, "bilgisayarları için en az bir güvenlik önlemi alıyor", null, "TÜİK, Girişimlerde Bilişim Teknolojileri Kullanım Araştırması, 2026", 2026 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BenchmarkReferences");
        }
    }
}
