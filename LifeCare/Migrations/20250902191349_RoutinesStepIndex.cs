using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeCare.Migrations
{
    /// <inheritdoc />
    public partial class RoutinesStepIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RoutineStepProductEntry_RoutineStepEntryId",
                table: "RoutineStepProductEntry");

            migrationBuilder.DropIndex(
                name: "IX_RoutineStepEntries_RoutineEntryId",
                table: "RoutineStepEntries");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineStepProductEntry_RoutineStepEntryId_RoutineStepProductId",
                table: "RoutineStepProductEntry",
                columns: new[] { "RoutineStepEntryId", "RoutineStepProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoutineStepEntries_RoutineEntryId_RoutineStepId",
                table: "RoutineStepEntries",
                columns: new[] { "RoutineEntryId", "RoutineStepId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RoutineStepProductEntry_RoutineStepEntryId_RoutineStepProductId",
                table: "RoutineStepProductEntry");

            migrationBuilder.DropIndex(
                name: "IX_RoutineStepEntries_RoutineEntryId_RoutineStepId",
                table: "RoutineStepEntries");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineStepProductEntry_RoutineStepEntryId",
                table: "RoutineStepProductEntry",
                column: "RoutineStepEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineStepEntries_RoutineEntryId",
                table: "RoutineStepEntries",
                column: "RoutineEntryId");
        }
    }
}
