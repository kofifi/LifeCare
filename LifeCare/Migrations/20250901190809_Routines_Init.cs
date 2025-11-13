using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeCare.Migrations
{
    /// <inheritdoc />
    public partial class Routines_Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ScheduledDate",
                table: "RoutineEntries",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "Done",
                table: "RoutineEntries",
                newName: "Skipped");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Routines",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Routines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Routines",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Routines",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "Routines",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "Routines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RRule",
                table: "Routines",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReminderEnabled",
                table: "Routines",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ReminderMinutesBefore",
                table: "Routines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDateUtc",
                table: "Routines",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "TimeOfDay",
                table: "Routines",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Completed",
                table: "RoutineEntries",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "RoutineEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RoutineSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoutineId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EstimatedMinutes = table.Column<int>(type: "int", nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false),
                    RRule = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutineSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoutineSteps_Routines_RoutineId",
                        column: x => x.RoutineId,
                        principalTable: "Routines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoutineStepEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoutineEntryId = table.Column<int>(type: "int", nullable: false),
                    RoutineStepId = table.Column<int>(type: "int", nullable: false),
                    Completed = table.Column<bool>(type: "bit", nullable: false),
                    Skipped = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutineStepEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoutineStepEntries_RoutineEntries_RoutineEntryId",
                        column: x => x.RoutineEntryId,
                        principalTable: "RoutineEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoutineStepEntries_RoutineSteps_RoutineStepId",
                        column: x => x.RoutineStepId,
                        principalTable: "RoutineSteps",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RoutineStepProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoutineStepId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutineStepProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoutineStepProducts_RoutineSteps_RoutineStepId",
                        column: x => x.RoutineStepId,
                        principalTable: "RoutineSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Routines_CategoryId",
                table: "Routines",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineStepEntries_RoutineEntryId",
                table: "RoutineStepEntries",
                column: "RoutineEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineStepEntries_RoutineStepId",
                table: "RoutineStepEntries",
                column: "RoutineStepId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineStepProducts_RoutineStepId",
                table: "RoutineStepProducts",
                column: "RoutineStepId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineSteps_RoutineId",
                table: "RoutineSteps",
                column: "RoutineId");

            migrationBuilder.AddForeignKey(
                name: "FK_Routines_Categories_CategoryId",
                table: "Routines",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Routines_Categories_CategoryId",
                table: "Routines");

            migrationBuilder.DropTable(
                name: "RoutineStepEntries");

            migrationBuilder.DropTable(
                name: "RoutineStepProducts");

            migrationBuilder.DropTable(
                name: "RoutineSteps");

            migrationBuilder.DropIndex(
                name: "IX_Routines_CategoryId",
                table: "Routines");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Routines");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "Routines");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Routines");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "Routines");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "Routines");

            migrationBuilder.DropColumn(
                name: "RRule",
                table: "Routines");

            migrationBuilder.DropColumn(
                name: "ReminderEnabled",
                table: "Routines");

            migrationBuilder.DropColumn(
                name: "ReminderMinutesBefore",
                table: "Routines");

            migrationBuilder.DropColumn(
                name: "StartDateUtc",
                table: "Routines");

            migrationBuilder.DropColumn(
                name: "TimeOfDay",
                table: "Routines");

            migrationBuilder.DropColumn(
                name: "Completed",
                table: "RoutineEntries");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "RoutineEntries");

            migrationBuilder.RenameColumn(
                name: "Skipped",
                table: "RoutineEntries",
                newName: "Done");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "RoutineEntries",
                newName: "ScheduledDate");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Routines",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);
        }
    }
}
