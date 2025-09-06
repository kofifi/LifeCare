using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeCare.Migrations
{
    /// <inheritdoc />
    public partial class Routines_RotationFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RotationEnabled",
                table: "RoutineSteps",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RotationMode",
                table: "RoutineSteps",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "RoutineStepProducts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RoutineStepProductEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoutineStepEntryId = table.Column<int>(type: "int", nullable: false),
                    RoutineStepProductId = table.Column<int>(type: "int", nullable: false),
                    Completed = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutineStepProductEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoutineStepProductEntry_RoutineStepEntries_RoutineStepEntryId",
                        column: x => x.RoutineStepEntryId,
                        principalTable: "RoutineStepEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoutineStepProductEntry_RoutineStepProducts_RoutineStepProductId",
                        column: x => x.RoutineStepProductId,
                        principalTable: "RoutineStepProducts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoutineStepProductEntry_RoutineStepEntryId",
                table: "RoutineStepProductEntry",
                column: "RoutineStepEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineStepProductEntry_RoutineStepProductId",
                table: "RoutineStepProductEntry",
                column: "RoutineStepProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoutineStepProductEntry");

            migrationBuilder.DropColumn(
                name: "RotationEnabled",
                table: "RoutineSteps");

            migrationBuilder.DropColumn(
                name: "RotationMode",
                table: "RoutineSteps");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "RoutineStepProducts");
        }
    }
}
