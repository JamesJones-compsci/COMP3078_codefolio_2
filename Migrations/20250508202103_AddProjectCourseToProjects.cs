using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeFolio.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectCourseToProjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProjectCourse",
                table: "Projects",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProjectCourse",
                table: "Projects");
        }
    }
}
