using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRReserveSystem.Migrations
{
    /// <inheritdoc />
    public partial class HardeningConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Applications_CandidateId",
                table: "Applications");

            migrationBuilder.RenameColumn(
                name: "Password",
                table: "Recruiters",
                newName: "PasswordHash");

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Vacancies",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Candidates",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Vacancies_SalaryRange",
                table: "Vacancies",
                sql: "CAST(\"SalaryMax\" AS REAL) >= CAST(\"SalaryMin\" AS REAL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Vacancies_Status",
                table: "Vacancies",
                sql: "\"Status\" IN ('Open','Paused','Closed')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SoftSkillAssessments_Communication",
                table: "SoftSkillAssessments",
                sql: "\"Communication\" BETWEEN 1 AND 10");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SoftSkillAssessments_Leadership",
                table: "SoftSkillAssessments",
                sql: "\"Leadership\" BETWEEN 1 AND 10");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SoftSkillAssessments_Responsibility",
                table: "SoftSkillAssessments",
                sql: "\"Responsibility\" BETWEEN 1 AND 10");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SoftSkillAssessments_StressResistance",
                table: "SoftSkillAssessments",
                sql: "\"StressResistance\" BETWEEN 1 AND 10");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SoftSkillAssessments_Teamwork",
                table: "SoftSkillAssessments",
                sql: "\"Teamwork\" BETWEEN 1 AND 10");

            migrationBuilder.AddCheckConstraint(
                name: "CK_InterviewFeedbacks_Score",
                table: "InterviewFeedbacks",
                sql: "\"Score\" BETWEEN 1 AND 10");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_CandidateId_VacancyId",
                table: "Applications",
                columns: new[] { "CandidateId", "VacancyId" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Applications_Status",
                table: "Applications",
                sql: "\"Status\" IN ('New','Screening','Interview','TestTask','Offer','Hired','Rejected')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Vacancies_SalaryRange",
                table: "Vacancies");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Vacancies_Status",
                table: "Vacancies");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SoftSkillAssessments_Communication",
                table: "SoftSkillAssessments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SoftSkillAssessments_Leadership",
                table: "SoftSkillAssessments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SoftSkillAssessments_Responsibility",
                table: "SoftSkillAssessments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SoftSkillAssessments_StressResistance",
                table: "SoftSkillAssessments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SoftSkillAssessments_Teamwork",
                table: "SoftSkillAssessments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InterviewFeedbacks_Score",
                table: "InterviewFeedbacks");

            migrationBuilder.DropIndex(
                name: "IX_Applications_CandidateId_VacancyId",
                table: "Applications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Applications_Status",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Candidates");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "Recruiters",
                newName: "Password");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_CandidateId",
                table: "Applications",
                column: "CandidateId");
        }
    }
}
