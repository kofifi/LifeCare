using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeCare.Migrations
{
    /// <inheritdoc />
    public partial class AddHabitOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "Habits",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Order",
                table: "Habits");
        }
    }
}
