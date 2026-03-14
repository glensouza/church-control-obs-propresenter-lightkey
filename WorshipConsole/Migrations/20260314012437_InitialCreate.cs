using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorshipConsole.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Scripts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Act = table.Column<string>(type: "TEXT", nullable: false),
                    Scene = table.Column<string>(type: "TEXT", nullable: false),
                    SceneLength = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    SwitchToScene = table.Column<string>(type: "TEXT", nullable: false),
                    Camera1Action = table.Column<string>(type: "TEXT", nullable: false),
                    Camera1Position = table.Column<string>(type: "TEXT", nullable: true),
                    Camera2Action = table.Column<string>(type: "TEXT", nullable: false),
                    Camera2Position = table.Column<string>(type: "TEXT", nullable: true),
                    Camera3Action = table.Column<string>(type: "TEXT", nullable: false),
                    Camera3Position = table.Column<string>(type: "TEXT", nullable: true),
                    StageLightScene = table.Column<string>(type: "TEXT", nullable: true),
                    SpotlightLeft = table.Column<string>(type: "TEXT", nullable: true),
                    SpotlightRight = table.Column<string>(type: "TEXT", nullable: true),
                    HouseLights = table.Column<string>(type: "TEXT", nullable: true),
                    LightingNotes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scripts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Scripts");

            migrationBuilder.DropTable(
                name: "Settings");
        }
    }
}
