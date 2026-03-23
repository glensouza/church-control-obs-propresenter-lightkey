using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corona.Pageant.Migrations
{
    /// <inheritdoc />
    public partial class NewLightFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HouseLights",
                table: "Scripts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NavClass",
                table: "Scripts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpotlightLeft",
                table: "Scripts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpotlightRight",
                table: "Scripts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StageLightScene",
                table: "Scripts",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HouseLights",
                table: "Scripts");

            migrationBuilder.DropColumn(
                name: "NavClass",
                table: "Scripts");

            migrationBuilder.DropColumn(
                name: "SpotlightLeft",
                table: "Scripts");

            migrationBuilder.DropColumn(
                name: "SpotlightRight",
                table: "Scripts");

            migrationBuilder.DropColumn(
                name: "StageLightScene",
                table: "Scripts");
        }
    }
}
