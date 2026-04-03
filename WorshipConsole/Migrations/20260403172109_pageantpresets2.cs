using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corona.Pageant.Migrations
{
    /// <inheritdoc />
    public partial class pageantpresets2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "PageantObsScenes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "PageantObsScenes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
