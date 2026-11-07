using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EkoTwin.Migrations
{
    /// <inheritdoc />
    public partial class AddCbamFieldsToCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CbamProductGroup",
                table: "Companies",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<bool>(
                name: "ExportsToEu",
                table: "Companies",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CbamProductGroup",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ExportsToEu",
                table: "Companies");
        }
    }
}
