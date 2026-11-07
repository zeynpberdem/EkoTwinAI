using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EkoTwin.Migrations
{
    /// <inheritdoc />
    public partial class AddCarbonCalculator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CarbonFootprints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Scope1Tonnes = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    Scope2Tonnes = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    TotalTonnes = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    EmployeeCount = table.Column<int>(type: "integer", nullable: false),
                    TonnesPerEmployee = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarbonFootprints", x => x.Id);
                    table.CheckConstraint("CK_CarbonFootprints_Year", "\"Year\" BETWEEN 2000 AND 2100");
                    table.ForeignKey(
                        name: "FK_CarbonFootprints_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmissionFactors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    Source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Scope = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    KgCo2ePerUnit = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    Reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ReferenceYear = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmissionFactors", x => x.Id);
                    table.CheckConstraint("CK_EmissionFactors_Positive", "\"KgCo2ePerUnit\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "CarbonFootprintItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    CarbonFootprintId = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Scope = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FactorKgCo2ePerUnit = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    FactorReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EmissionTonnes = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarbonFootprintItems", x => x.Id);
                    table.CheckConstraint("CK_CarbonFootprintItems_Quantity", "\"Quantity\" >= 0");
                    table.ForeignKey(
                        name: "FK_CarbonFootprintItems_CarbonFootprints_CarbonFootprintId",
                        column: x => x.CarbonFootprintId,
                        principalTable: "CarbonFootprints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "EmissionFactors",
                columns: new[] { "Id", "IsActive", "KgCo2ePerUnit", "Reference", "ReferenceYear", "Scope", "Source", "Unit" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-7000-8002-000000000001"), true, 0.478m, "T.C. Enerji ve Tabii Kaynaklar Bakanlığı, 2022 Yılı Elektrik Üretim ve Tüketim Noktası Emisyon Faktörleri: dağıtım hattına bağlı tüketim noktası 0,478 tCO2e/MWh.", 2022, "Scope2", "Electricity", "kWh" },
                    { new Guid("00000000-0000-7000-8002-000000000002"), true, 1.9372m, "IPCC 2006 Rehberi Cilt 2: Tablo 1.2 (alt ısıl değer) ve Tablo 2.3 (imalat sanayi sabit yanma); GWP: IPCC AR5 (CH4 28, N2O 265). Doğal gaz: 9.155 kcal/Sm³ üst ısıl değer (EPDK/BOTAŞ referansı), ALD = ÜID × 0,90.", 2006, "Scope1", "NaturalGas", "m³" },
                    { new Guid("00000000-0000-7000-8002-000000000003"), true, 2.6853m, "IPCC 2006 Rehberi Cilt 2: Tablo 1.2 (alt ısıl değer) ve Tablo 2.3 (imalat sanayi sabit yanma); GWP: IPCC AR5 (CH4 28, N2O 265). Motorin (gas/diesel oil): ALD 43,0 MJ/kg, yoğunluk varsayımı 0,84 kg/L.", 2006, "Scope1", "Diesel", "litre" },
                    { new Guid("00000000-0000-7000-8002-000000000004"), true, 2.2952m, "IPCC 2006 Rehberi Cilt 2: Tablo 1.2 (alt ısıl değer) ve Tablo 2.3 (imalat sanayi sabit yanma); GWP: IPCC AR5 (CH4 28, N2O 265). Benzin (motor gasoline): ALD 44,3 MJ/kg, yoğunluk varsayımı 0,745 kg/L.", 2006, "Scope1", "Gasoline", "litre" },
                    { new Guid("00000000-0000-7000-8002-000000000005"), true, 2.9872m, "IPCC 2006 Rehberi Cilt 2: Tablo 1.2 (alt ısıl değer) ve Tablo 2.3 (imalat sanayi sabit yanma); GWP: IPCC AR5 (CH4 28, N2O 265). LPG: ALD 47,3 MJ/kg.", 2006, "Scope1", "Lpg", "kg" },
                    { new Guid("00000000-0000-7000-8002-000000000006"), true, 3036.5055m, "IPCC 2006 Rehberi Cilt 2: Tablo 1.2 (alt ısıl değer) ve Tablo 2.3 (imalat sanayi sabit yanma); GWP: IPCC AR5 (CH4 28, N2O 265). Kok (coke oven coke): ALD 28,2 MJ/kg.", 2006, "Scope1", "Coke", "ton" },
                    { new Guid("00000000-0000-7000-8002-000000000007"), true, 1209.9623m, "IPCC 2006 Rehberi Cilt 2: Tablo 1.2 (alt ısıl değer) ve Tablo 2.3 (imalat sanayi sabit yanma); GWP: IPCC AR5 (CH4 28, N2O 265). Linyit: IPCC varsayılan ALD 11,9 MJ/kg; Türk linyitlerinde ısıl değer ocağa göre değişir.", 2006, "Scope1", "Lignite", "ton" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarbonFootprintItems_CarbonFootprintId_Source",
                table: "CarbonFootprintItems",
                columns: new[] { "CarbonFootprintId", "Source" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarbonFootprints_CompanyId_Year",
                table: "CarbonFootprints",
                columns: new[] { "CompanyId", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmissionFactors_Source",
                table: "EmissionFactors",
                column: "Source",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarbonFootprintItems");

            migrationBuilder.DropTable(
                name: "EmissionFactors");

            migrationBuilder.DropTable(
                name: "CarbonFootprints");
        }
    }
}
