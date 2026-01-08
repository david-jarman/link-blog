using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinkBlog.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestbookTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuestbookEntries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Website = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestbookEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuestbookEntries_CreatedDate",
                table: "GuestbookEntries",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_GuestbookEntries_IsApproved",
                table: "GuestbookEntries",
                column: "IsApproved");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuestbookEntries");
        }
    }
}
