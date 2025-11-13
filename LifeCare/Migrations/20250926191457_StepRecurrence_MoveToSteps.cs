using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeCare.Migrations
{
    /// <inheritdoc />
    public partial class StepRecurrence_MoveToSteps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RRule",
                table: "Routines");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RRule",
                table: "Routines",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
