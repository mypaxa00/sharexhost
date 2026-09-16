using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShareXHost.Migrations
{
    /// <inheritdoc />
    public partial class OriginalFileNameIntroduction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "original_file_name",
                table: "files",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "original_file_name",
                table: "files");
        }
    }
}
