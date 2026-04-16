using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternalExamScrutinySystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddExamIdToAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Note: Several columns (ExamId, SubjectId, Series, Version, ActiveSeries) already existed in the DB but were not tracked locally.
            // Keeping only the genuinely new ExamId for FacultySubjectAssignments.

            migrationBuilder.AddColumn<int>(
                name: "ExamId",
                table: "FacultySubjectAssignments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FacultySubjectAssignments_ExamId",
                table: "FacultySubjectAssignments",
                column: "ExamId");

            migrationBuilder.AddForeignKey(
                name: "FK_FacultySubjectAssignments_Exams_ExamId",
                table: "FacultySubjectAssignments",
                column: "ExamId",
                principalTable: "Exams",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FacultySubjectAssignments_Exams_ExamId",
                table: "FacultySubjectAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_QuestionPapers_Exams_ExamId",
                table: "QuestionPapers");

            migrationBuilder.DropForeignKey(
                name: "FK_QuestionPapers_ModuleSubjects_SubjectId",
                table: "QuestionPapers");

            migrationBuilder.DropIndex(
                name: "IX_QuestionPapers_ExamId",
                table: "QuestionPapers");

            migrationBuilder.DropIndex(
                name: "IX_QuestionPapers_SubjectId",
                table: "QuestionPapers");

            migrationBuilder.DropIndex(
                name: "IX_FacultySubjectAssignments_ExamId",
                table: "FacultySubjectAssignments");

            migrationBuilder.DropColumn(
                name: "ExamId",
                table: "QuestionPapers");

            migrationBuilder.DropColumn(
                name: "Series",
                table: "QuestionPapers");

            migrationBuilder.DropColumn(
                name: "SubjectId",
                table: "QuestionPapers");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "QuestionPapers");

            migrationBuilder.DropColumn(
                name: "ExamId",
                table: "FacultySubjectAssignments");

            migrationBuilder.DropColumn(
                name: "ActiveSeries",
                table: "Exams");

            migrationBuilder.AlterColumn<string>(
                name: "SubjectCode",
                table: "QuestionPapers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
