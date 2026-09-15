using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShareXHost.Migrations
{
    /// <inheritdoc />
    public partial class MakeShortIdLinkKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_links",
                table: "links");

            migrationBuilder.DropIndex(
                name: "IX_links_short_id",
                table: "links");

            migrationBuilder.DropColumn(
                name: "id",
                table: "links");

            migrationBuilder.AddPrimaryKey(
                name: "PK_links",
                table: "links",
                column: "short_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_links",
                table: "links");

            migrationBuilder.AddColumn<Guid>(
                name: "id",
                table: "links",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_links",
                table: "links",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_links_short_id",
                table: "links",
                column: "short_id",
                unique: true);
        }
    }
}
