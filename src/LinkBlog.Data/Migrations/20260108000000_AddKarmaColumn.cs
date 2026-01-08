using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinkBlog.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKarmaColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Karma",
                table: "Posts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Karma",
                table: "Posts");
        }
    }
}
