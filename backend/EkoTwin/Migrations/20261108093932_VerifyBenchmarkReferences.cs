using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EkoTwin.Migrations
{
    /// <inheritdoc />
    public partial class VerifyBenchmarkReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "BenchmarkReferences",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8004-000000000001"),
                columns: new[] { "IsVerified", "Source" },
                values: new object[] { true, "TÜİK, Girişimlerde Bilişim Teknolojileri Kullanım Araştırması, 2025 (https://veriportali.tuik.gov.tr/tr/press/54012)" });

            migrationBuilder.UpdateData(
                table: "BenchmarkReferences",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8004-000000000002"),
                columns: new[] { "IsVerified", "Source" },
                values: new object[] { true, "TÜİK, Girişimlerde Bilişim Teknolojileri Kullanım Araştırması, 2025 (https://veriportali.tuik.gov.tr/tr/press/54012)" });

            migrationBuilder.UpdateData(
                table: "BenchmarkReferences",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8004-000000000003"),
                columns: new[] { "IsVerified", "Source" },
                values: new object[] { true, "TÜİK, Girişimlerde Bilişim Teknolojileri Kullanım Araştırması, 2026 (https://veriportali.tuik.gov.tr/tr/press/58021)" });

            migrationBuilder.UpdateData(
                table: "BenchmarkReferences",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8004-000000000004"),
                columns: new[] { "IsVerified", "Source" },
                values: new object[] { true, "TÜİK, Girişimlerde Bilişim Teknolojileri Kullanım Araştırması, 2026 (https://veriportali.tuik.gov.tr/tr/press/58021)" });

            migrationBuilder.UpdateData(
                table: "BenchmarkReferences",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8004-000000000005"),
                columns: new[] { "IsVerified", "Percent", "SizeClass", "Source" },
                values: new object[] { true, 81.2m, "Small", "TÜİK, Girişimlerde Bilişim Teknolojileri Kullanım Araştırması, 2026 (https://veriportali.tuik.gov.tr/tr/press/58021)" });

            migrationBuilder.InsertData(
                table: "BenchmarkReferences",
                columns: new[] { "Id", "Indicator", "IsVerified", "MinAnswerScore", "Percent", "QuestionNumber", "SimpleIndicator", "SizeClass", "Source", "Year" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-7000-8004-000000000006"), "Bilgi ve iletişim teknolojileri güvenlik önlemi alan girişim oranı", true, 2, 90.5m, 3, "bilgisayarları için en az bir güvenlik önlemi alıyor", "Medium", "TÜİK, Girişimlerde Bilişim Teknolojileri Kullanım Araştırması, 2026 (https://veriportali.tuik.gov.tr/tr/press/58021)", 2026 },
                    { new Guid("00000000-0000-7000-8004-000000000007"), "Enerji tüketimini azaltmak için bilgi ve iletişim teknolojileri kullanan girişim oranı", true, null, 15.0m, null, "enerjiyi azaltmak için yazılım veya akıllı sistem kullanıyor", "Small", "TÜİK, Girişimlerde Bilişim Teknolojileri Kullanım Araştırması, 2025 (https://veriportali.tuik.gov.tr/tr/press/54012)", 2025 },
                    { new Guid("00000000-0000-7000-8004-000000000008"), "Enerji tüketimini azaltmak için bilgi ve iletişim teknolojileri kullanan girişim oranı", true, null, 24.3m, null, "enerjiyi azaltmak için yazılım veya akıllı sistem kullanıyor", "Medium", "TÜİK, Girişimlerde Bilişim Teknolojileri Kullanım Araştırması, 2025 (https://veriportali.tuik.gov.tr/tr/press/54012)", 2025 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BenchmarkReferences",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8004-000000000006"));

            migrationBuilder.DeleteData(
                table: "BenchmarkReferences",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8004-000000000007"));

            migrationBuilder.DeleteData(
                table: "BenchmarkReferences",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8004-000000000008"));

            migrationBuilder.UpdateData(
                table: "BenchmarkReferences",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8004-000000000001"),
                columns: new[] { "IsVerified", "Source" },
                values: new object[] { false, "TÜİK, Girişimlerde Bilişim Teknolojileri Kullanım Araştırması, 2025" });

            migrationBuilder.UpdateData(
                table: "BenchmarkReferences",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8004-000000000002"),
                columns: new[] { "IsVerified", "Source" },
                values: new object[] { false, "TÜİK, Girişimlerde Bilişim Teknolojileri Kullanım Araştırması, 2025" });

            migrationBuilder.UpdateData(
                table: "BenchmarkReferences",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8004-000000000003"),
                columns: new[] { "IsVerified", "Source" },
                values: new object[] { false, "TÜİK, Girişimlerde Bilişim Teknolojileri Kullanım Araştırması, 2026" });

            migrationBuilder.UpdateData(
                table: "BenchmarkReferences",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8004-000000000004"),
                columns: new[] { "IsVerified", "Source" },
                values: new object[] { false, "TÜİK, Girişimlerde Bilişim Teknolojileri Kullanım Araştırması, 2026" });

            migrationBuilder.UpdateData(
                table: "BenchmarkReferences",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8004-000000000005"),
                columns: new[] { "IsVerified", "Percent", "SizeClass", "Source" },
                values: new object[] { false, 83.0m, null, "TÜİK, Girişimlerde Bilişim Teknolojileri Kullanım Araştırması, 2026" });
        }
    }
}
