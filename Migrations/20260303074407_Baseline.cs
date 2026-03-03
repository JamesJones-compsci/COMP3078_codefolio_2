using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeFolio.Migrations
{
    /// <inheritdoc />
    public partial class Baseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This is intentionally left empty
            // EF will record the current state as applied without creating tables
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This is intentionally left empty
            // Nothing to rollback for baseline
        }
    }
}