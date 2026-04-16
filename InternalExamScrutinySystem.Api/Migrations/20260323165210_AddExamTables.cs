using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternalExamScrutinySystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddExamTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Manual sync already performed. Emptying to allow 'Migrate()' to succeed.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Emptying as Up is empty.
        }
    }
}
