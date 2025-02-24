using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinkBlog.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPostIndexesAndTagNameIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_Date",
                table: "Posts",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_ShortTitle",
                table: "Posts",
                column: "ShortTitle",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tags_Name",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Posts_Date",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_ShortTitle",
                table: "Posts");
        }
    }
}
