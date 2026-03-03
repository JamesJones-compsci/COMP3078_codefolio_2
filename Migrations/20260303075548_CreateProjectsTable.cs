using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeFolio.Migrations
{
    /// <inheritdoc />
    public partial class CreateProjectsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    ProjectId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectTitle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProjectCourse = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProjectDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProjectTechnologies = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProjectDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ProjectContribution = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    YouTubeLink = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    GitHubRepository = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.ProjectId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
