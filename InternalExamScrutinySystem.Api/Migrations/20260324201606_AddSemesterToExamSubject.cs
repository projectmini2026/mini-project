using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternalExamScrutinySystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSemesterToExamSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Semester",
                table: "ExamSubjects",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Semester",
                table: "ExamSubjects");
        }
    }
}
