using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EkoTwin.Migrations
{
    /// <inheritdoc />
    public partial class RefineQuestions12And16 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000012"),
                column: "Text",
                value: "AB Yeşil Mutabakatı ve SKDM (CBAM) kriterlerine uyum düzeyiniz nedir?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000016"),
                column: "Text",
                value: "Tedarik zincirinizde geri dönüştürülmüş veya çevre dostu malzeme kullanılıyor mu?");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000012"),
                column: "Text",
                value: "AB Yeşil Mutabakatı (CBAM) veya ulusal yeşil mutabakat kriterleri hakkında bilgi sahibi ve uyumlu musunuz?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000016"),
                column: "Text",
                value: "Tedarik zincirinizde geri dönüştürülmüş veya çevre dostu malzeme kullanım oranı nedir?");
        }
    }
}
