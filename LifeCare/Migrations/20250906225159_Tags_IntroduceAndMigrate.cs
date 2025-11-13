using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeCare.Migrations
{
    /// <inheritdoc />
    public partial class Tags_IntroduceAndMigrate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tags_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HabitTags",
                columns: table => new
                {
                    HabitsId = table.Column<int>(type: "int", nullable: false),
                    TagsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HabitTags", x => new { x.HabitsId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_HabitTags_Habits_HabitsId",
                        column: x => x.HabitsId,
                        principalTable: "Habits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HabitTags_Tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "Tags",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RoutineTags",
                columns: table => new
                {
                    RoutinesId = table.Column<int>(type: "int", nullable: false),
                    TagsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutineTags", x => new { x.RoutinesId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_RoutineTags_Routines_RoutinesId",
                        column: x => x.RoutinesId,
                        principalTable: "Routines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoutineTags_Tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "Tags",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_HabitTags_TagsId",
                table: "HabitTags",
                column: "TagsId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineTags_TagsId",
                table: "RoutineTags",
                column: "TagsId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_UserId_Name",
                table: "Tags",
                columns: new[] { "UserId", "Name" },
                unique: true);
            
            migrationBuilder.Sql(@"
                INSERT INTO Tags (Name, UserId)
                SELECT DISTINCT LTRIM(RTRIM(c.Name)), c.UserId
                FROM Categories c
                WHERE c.Name IS NOT NULL AND c.UserId IS NOT NULL
            ");

            migrationBuilder.Sql(@"
                INSERT INTO HabitTags (HabitsId, TagsId)
                SELECT h.Id, t.Id
                FROM Habits h
                JOIN Categories c ON c.Id = h.CategoryId
                JOIN Tags t ON t.UserId = c.UserId AND t.Name = LTRIM(RTRIM(c.Name))
            ");

            migrationBuilder.Sql(@"
                INSERT INTO RoutineTags (RoutinesId, TagsId)
                SELECT r.Id, t.Id
                FROM Routines r
                JOIN Categories c ON c.Id = r.CategoryId
                JOIN Tags t ON t.UserId = c.UserId AND t.Name = LTRIM(RTRIM(c.Name))
            ");

            migrationBuilder.Sql("UPDATE Habits   SET CategoryId = NULL WHERE CategoryId IS NOT NULL");
            migrationBuilder.Sql("UPDATE Routines SET CategoryId = NULL WHERE CategoryId IS NOT NULL");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HabitTags");

            migrationBuilder.DropTable(
                name: "RoutineTags");

            migrationBuilder.DropTable(
                name: "Tags");
        }
    }
}
