using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EkoTwin.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionsAndAssessmentDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CarbonManagementScore",
                table: "Assessments",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DigitalInfrastructureScore",
                table: "Assessments",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "EnergyEfficiencyScore",
                table: "Assessments",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SupplyChainScore",
                table: "Assessments",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalScore",
                table: "Assessments",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    Dimension = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentDetails", x => x.Id);
                    table.CheckConstraint("CK_AssessmentDetails_Score", "\"Score\" BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_AssessmentDetails_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssessmentDetails_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Questions",
                columns: new[] { "Id", "Dimension", "DisplayOrder", "IsActive", "Text" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-7000-8000-000000000001"), "DigitalInfrastructure", 1, true, "İş süreçlerinizi (satış, stok, muhasebe vb.) entegre bir yazılım (ERP vb.) ile yönetiyor musunuz?" },
                    { new Guid("00000000-0000-7000-8000-000000000002"), "DigitalInfrastructure", 2, true, "Üretim/operasyon verileriniz sensörler veya otomasyon sistemleriyle dijital olarak toplanıyor mu?" },
                    { new Guid("00000000-0000-7000-8000-000000000003"), "DigitalInfrastructure", 3, true, "Verileriniz bulut ortamında güvenli şekilde saklanıyor ve düzenli olarak yedekleniyor mu?" },
                    { new Guid("00000000-0000-7000-8000-000000000004"), "DigitalInfrastructure", 4, true, "Karar alma süreçlerinizde veri analitiği veya yapay zekâ araçlarından yararlanıyor musunuz?" },
                    { new Guid("00000000-0000-7000-8000-000000000005"), "EnergyEfficiency", 5, true, "Enerji tüketiminizi sayaç veya otomasyon sistemleriyle düzenli olarak ölçüp izliyor musunuz?" },
                    { new Guid("00000000-0000-7000-8000-000000000006"), "EnergyEfficiency", 6, true, "Yüksek verimli ekipman (verimli motor, LED aydınlatma, inverter vb.) kullanıyor musunuz?" },
                    { new Guid("00000000-0000-7000-8000-000000000007"), "EnergyEfficiency", 7, true, "Yenilenebilir enerji kaynaklarından (GES, yeşil enerji sertifikası vb.) yararlanıyor musunuz?" },
                    { new Guid("00000000-0000-7000-8000-000000000008"), "EnergyEfficiency", 8, true, "Enerji verimliliği hedefleri belirleyip bunları takip ediyor musunuz (örn. ISO 50001)?" },
                    { new Guid("00000000-0000-7000-8000-000000000009"), "CarbonManagement", 9, true, "Kapsam 1 ve Kapsam 2 sera gazı emisyonlarınızı hesaplıyor musunuz?" },
                    { new Guid("00000000-0000-7000-8000-000000000010"), "CarbonManagement", 10, true, "Karbon azaltım hedefleriniz ve bu hedeflere yönelik bir eylem planınız var mı?" },
                    { new Guid("00000000-0000-7000-8000-000000000011"), "CarbonManagement", 11, true, "AB Sınırda Karbon Düzenleme Mekanizması (SKDM/CBAM) gibi düzenlemelere hazırlık yapıyor musunuz?" },
                    { new Guid("00000000-0000-7000-8000-000000000012"), "CarbonManagement", 12, true, "Atık yönetimi ve geri dönüşüm uygulamalarınızı kayıt altına alıyor musunuz?" },
                    { new Guid("00000000-0000-7000-8000-000000000013"), "SupplyChain", 13, true, "Tedarikçilerinizi çevresel ve sosyal kriterlere göre değerlendiriyor musunuz?" },
                    { new Guid("00000000-0000-7000-8000-000000000014"), "SupplyChain", 14, true, "Tedarik zinciri süreçleriniz (sipariş, sevkiyat, stok) dijital platformlar üzerinden izlenebiliyor mu?" },
                    { new Guid("00000000-0000-7000-8000-000000000015"), "SupplyChain", 15, true, "Lojistik süreçlerinizde karbon ayak izini azaltmaya yönelik uygulamalarınız var mı?" },
                    { new Guid("00000000-0000-7000-8000-000000000016"), "SupplyChain", 16, true, "Ürünlerinizde kullanılan hammaddelerin kaynağı ve izlenebilirliği kayıt altında mı?" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentDetails_AssessmentId_QuestionId",
                table: "AssessmentDetails",
                columns: new[] { "AssessmentId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentDetails_QuestionId",
                table: "AssessmentDetails",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_DisplayOrder",
                table: "Questions",
                column: "DisplayOrder",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssessmentDetails");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropColumn(
                name: "CarbonManagementScore",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "DigitalInfrastructureScore",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "EnergyEfficiencyScore",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "SupplyChainScore",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "TotalScore",
                table: "Assessments");
        }
    }
}
