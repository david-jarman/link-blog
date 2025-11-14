using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace LinkBlog.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFullTextSearchVector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Posts",
                type: "tsvector",
                nullable: true,
                computedColumnSql: "setweight(to_tsvector('english', COALESCE(\"Title\", '')), 'A') ||\n                  setweight(to_tsvector('english', COALESCE(\"LinkTitle\", '')), 'B') ||\n                  setweight(to_tsvector('english', COALESCE(\"Contents\", '')), 'C')",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_SearchVector",
                table: "Posts",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Posts_SearchVector",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "Posts");
        }
    }
}