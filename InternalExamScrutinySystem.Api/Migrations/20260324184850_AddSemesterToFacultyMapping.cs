using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternalExamScrutinySystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSemesterToFacultyMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScrutinizerAssignments_Modules_ModuleId",
                table: "ScrutinizerAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_ScrutinizerAssignments_Users_FacultyId",
                table: "ScrutinizerAssignments");

            migrationBuilder.AddColumn<string>(
                name: "Semester",
                table: "FacultySubjectAssignments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScrutinyReports_QuestionPaperId",
                table: "ScrutinyReports",
                column: "QuestionPaperId");

            migrationBuilder.CreateIndex(
                name: "IX_ScrutinyReports_ScrutinizerUserId",
                table: "ScrutinyReports",
                column: "ScrutinizerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScrutinyAssignments_QuestionPaperId",
                table: "ScrutinyAssignments",
                column: "QuestionPaperId");

            migrationBuilder.CreateIndex(
                name: "IX_ScrutinyAssignments_ScrutinizerUserId",
                table: "ScrutinyAssignments",
                column: "ScrutinizerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionPapers_ScrutinizerUserId",
                table: "QuestionPapers",
                column: "ScrutinizerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_QuestionPapers_Users_ScrutinizerUserId",
                table: "QuestionPapers",
                column: "ScrutinizerUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScrutinizerAssignments_Modules_ModuleId",
                table: "ScrutinizerAssignments",
                column: "ModuleId",
                principalTable: "Modules",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScrutinizerAssignments_Users_FacultyId",
                table: "ScrutinizerAssignments",
                column: "FacultyId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScrutinyAssignments_QuestionPapers_QuestionPaperId",
                table: "ScrutinyAssignments",
                column: "QuestionPaperId",
                principalTable: "QuestionPapers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScrutinyAssignments_Users_ScrutinizerUserId",
                table: "ScrutinyAssignments",
                column: "ScrutinizerUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScrutinyReports_QuestionPapers_QuestionPaperId",
                table: "ScrutinyReports",
                column: "QuestionPaperId",
                principalTable: "QuestionPapers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScrutinyReports_Users_ScrutinizerUserId",
                table: "ScrutinyReports",
                column: "ScrutinizerUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuestionPapers_Users_ScrutinizerUserId",
                table: "QuestionPapers");

            migrationBuilder.DropForeignKey(
                name: "FK_ScrutinizerAssignments_Modules_ModuleId",
                table: "ScrutinizerAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_ScrutinizerAssignments_Users_FacultyId",
                table: "ScrutinizerAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_ScrutinyAssignments_QuestionPapers_QuestionPaperId",
                table: "ScrutinyAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_ScrutinyAssignments_Users_ScrutinizerUserId",
                table: "ScrutinyAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_ScrutinyReports_QuestionPapers_QuestionPaperId",
                table: "ScrutinyReports");

            migrationBuilder.DropForeignKey(
                name: "FK_ScrutinyReports_Users_ScrutinizerUserId",
                table: "ScrutinyReports");

            migrationBuilder.DropIndex(
                name: "IX_ScrutinyReports_QuestionPaperId",
                table: "ScrutinyReports");

            migrationBuilder.DropIndex(
                name: "IX_ScrutinyReports_ScrutinizerUserId",
                table: "ScrutinyReports");

            migrationBuilder.DropIndex(
                name: "IX_ScrutinyAssignments_QuestionPaperId",
                table: "ScrutinyAssignments");

            migrationBuilder.DropIndex(
                name: "IX_ScrutinyAssignments_ScrutinizerUserId",
                table: "ScrutinyAssignments");

            migrationBuilder.DropIndex(
                name: "IX_QuestionPapers_ScrutinizerUserId",
                table: "QuestionPapers");

            migrationBuilder.DropColumn(
                name: "Semester",
                table: "FacultySubjectAssignments");

            migrationBuilder.AddForeignKey(
                name: "FK_ScrutinizerAssignments_Modules_ModuleId",
                table: "ScrutinizerAssignments",
                column: "ModuleId",
                principalTable: "Modules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ScrutinizerAssignments_Users_FacultyId",
                table: "ScrutinizerAssignments",
                column: "FacultyId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
