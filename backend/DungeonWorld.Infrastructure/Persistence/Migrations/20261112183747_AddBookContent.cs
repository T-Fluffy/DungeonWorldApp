using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DungeonWorld.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Books",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Author = table.Column<string>(type: "text", nullable: true),
                    Introduction = table.Column<string>(type: "text", nullable: true),
                    MapPath = table.Column<string>(type: "text", nullable: true),
                    AdventureSheetPath = table.Column<string>(type: "text", nullable: true),
                    SectionCount = table.Column<int>(type: "integer", nullable: false),
                    PresentSectionCount = table.Column<int>(type: "integer", nullable: false),
                    MissingSectionCount = table.Column<int>(type: "integer", nullable: false),
                    CombatSectionCount = table.Column<int>(type: "integer", nullable: false),
                    EnemyCount = table.Column<int>(type: "integer", nullable: false),
                    RulesJson = table.Column<string>(type: "text", nullable: true),
                    ImportedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    ImagePath = table.Column<string>(type: "text", nullable: true),
                    Raw = table.Column<string>(type: "text", nullable: false),
                    Clean = table.Column<string>(type: "text", nullable: false),
                    HasCombat = table.Column<bool>(type: "boolean", nullable: false),
                    MissingText = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnd = table.Column<bool>(type: "boolean", nullable: false),
                    DeathEnd = table.Column<bool>(type: "boolean", nullable: false),
                    VictoryEnd = table.Column<bool>(type: "boolean", nullable: false),
                    FeaturesJson = table.Column<string>(type: "text", nullable: true),
                    ReferencesCsv = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sections_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Choices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Target = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: true),
                    Text = table.Column<string>(type: "text", nullable: true),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    IsDiceRoll = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Choices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Choices_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Books_Title",
                table: "Books",
                column: "Title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Choices_SectionId_Target",
                table: "Choices",
                columns: new[] { "SectionId", "Target" });

            migrationBuilder.CreateIndex(
                name: "IX_Sections_BookId_Number",
                table: "Sections",
                columns: new[] { "BookId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sections_Number",
                table: "Sections",
                column: "Number");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Choices");

            migrationBuilder.DropTable(
                name: "Sections");

            migrationBuilder.DropTable(
                name: "Books");
        }
    }
}
