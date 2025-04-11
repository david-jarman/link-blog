using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinkBlog.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdatedDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedDate",
                table: "Posts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            // Custom code - set the UpdatedDate to the "created" Date
            migrationBuilder.Sql(
@"
    UPDATE ""Posts""
    SET ""UpdatedDate"" = ""Date"";
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "Posts");
        }
    }
}