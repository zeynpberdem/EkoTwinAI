using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EkoTwin.Migrations
{
    /// <inheritdoc />
    public partial class AddProgressTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CarbonTargets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BaseYear = table.Column<int>(type: "integer", nullable: false),
                    TargetYear = table.Column<int>(type: "integer", nullable: false),
                    ReductionPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarbonTargets", x => x.Id);
                    table.CheckConstraint("CK_CarbonTargets_Percent", "\"ReductionPercent\" > 0 AND \"ReductionPercent\" <= 100");
                    table.CheckConstraint("CK_CarbonTargets_Years", "\"TargetYear\" > \"BaseYear\"");
                    table.ForeignKey(
                        name: "FK_CarbonTargets_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarbonTargets_CompanyId",
                table: "CarbonTargets",
                column: "CompanyId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarbonTargets");
        }
    }
}
