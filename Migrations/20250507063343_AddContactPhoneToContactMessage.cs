using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeFolio.Migrations
{
    /// <inheritdoc />
    public partial class AddContactPhoneToContactMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "ContactMessages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "ContactMessages");
        }
    }
}
