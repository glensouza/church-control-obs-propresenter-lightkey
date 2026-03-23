using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corona.Pageant.Migrations
{
    /// <inheritdoc />
    public partial class AddCameras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NavClass",
                table: "Scripts");

            migrationBuilder.CreateTable(
                name: "Cameras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", nullable: false),
                    ViscaPort = table.Column<int>(type: "INTEGER", nullable: false),
                    UniFiPortNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    PanSpeed = table.Column<int>(type: "INTEGER", nullable: false),
                    TiltSpeed = table.Column<int>(type: "INTEGER", nullable: false),
                    ZoomSpeed = table.Column<int>(type: "INTEGER", nullable: false),
                    NumberOfPresets = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cameras", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cameras");

            migrationBuilder.AddColumn<string>(
                name: "NavClass",
                table: "Scripts",
                type: "TEXT",
                nullable: true);
        }
    }
}
