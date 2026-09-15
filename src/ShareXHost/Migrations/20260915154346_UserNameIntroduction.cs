using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShareXHost.Migrations
{
    /// <inheritdoc />
    public partial class UserNameIntroduction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "user_name",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "users_user_name_key",
                table: "users",
                column: "user_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "users_user_name_key",
                table: "users");

            migrationBuilder.DropColumn(
                name: "user_name",
                table: "users");
        }
    }
}
