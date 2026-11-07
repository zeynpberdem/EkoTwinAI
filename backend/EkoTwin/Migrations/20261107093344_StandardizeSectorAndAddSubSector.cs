using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EkoTwin.Migrations
{
    /// <inheritdoc />
    public partial class StandardizeSectorAndAddSubSector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SubSector",
                table: "Companies",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            // Elle eklendi: eski serbest metin sektörler (örn. "Tekstil") yeni standart listede yoksa
            // uygulama okurken hata verir. Bu kayıtlar "Other" yapılır, eski metin kaybolmasın diye
            // SubSector alanına taşınır.
            migrationBuilder.Sql("""
                UPDATE "Companies"
                SET "SubSector" = left("Sector", 100),
                    "Sector" = 'Other'
                WHERE "Sector" NOT IN (
                    'MetalWorkingAndCasting', 'MachineryAndEquipment', 'AutomotiveSupply',
                    'FurnitureAndWood', 'FoodAndBeverage', 'TextileAndApparel', 'MiningAndMarble',
                    'ConstructionMaterials', 'ChemicalsAndPlastics', 'ElectricalAndElectronics',
                    'Energy', 'Other');
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Sector",
                table: "Companies",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Sector",
                table: "Companies",
                column: "Sector");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Companies_Sector",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "SubSector",
                table: "Companies");

            migrationBuilder.AlterColumn<string>(
                name: "Sector",
                table: "Companies",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
        }
    }
}
