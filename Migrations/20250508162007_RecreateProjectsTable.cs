using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeFolio.Migrations
{
    /// <inheritdoc />
    public partial class RecreateProjectsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Technologies",
                table: "Projects",
                newName: "ProjectTechnologies");

            migrationBuilder.RenameColumn(
                name: "GitHubLink",
                table: "Projects",
                newName: "ProjectContribution");

            migrationBuilder.AddColumn<string>(
                name: "GitHubRepository",
                table: "Projects",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ProjectDate",
                table: "Projects",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GitHubRepository",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ProjectDate",
                table: "Projects");

            migrationBuilder.RenameColumn(
                name: "ProjectTechnologies",
                table: "Projects",
                newName: "Technologies");

            migrationBuilder.RenameColumn(
                name: "ProjectContribution",
                table: "Projects",
                newName: "GitHubLink");
        }
    }
}
