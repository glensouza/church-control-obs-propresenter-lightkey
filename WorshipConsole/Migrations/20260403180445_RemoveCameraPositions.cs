using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corona.Pageant.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCameraPositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Camera1Position",
                table: "Scripts");

            migrationBuilder.DropColumn(
                name: "Camera2Position",
                table: "Scripts");

            migrationBuilder.DropColumn(
                name: "Camera3Position",
                table: "Scripts");

            migrationBuilder.DropColumn(
                name: "Camera4Position",
                table: "Scripts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Camera1Position",
                table: "Scripts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Camera2Position",
                table: "Scripts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Camera3Position",
                table: "Scripts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Camera4Position",
                table: "Scripts",
                type: "TEXT",
                nullable: true);
        }
    }
}
