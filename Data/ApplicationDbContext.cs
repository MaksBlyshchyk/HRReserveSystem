using HRReserveSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HRReserveSystem.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<IdentityUser, IdentityRole, string>(options)
{
    public DbSet<Candidate> Candidates => Set<Candidate>();

    public DbSet<Vacancy> Vacancies => Set<Vacancy>();

    public DbSet<Application> Applications => Set<Application>();

    public DbSet<Interview> Interviews => Set<Interview>();

    public DbSet<InterviewFeedback> InterviewFeedbacks => Set<InterviewFeedback>();

    public DbSet<SoftSkillAssessment> SoftSkillAssessments => Set<SoftSkillAssessment>();

    public DbSet<Recruiter> Recruiters => Set<Recruiter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Candidate>()
            .HasIndex(candidate => candidate.Email)
            .IsUnique();

        modelBuilder.Entity<Recruiter>()
            .HasIndex(recruiter => recruiter.Email)
            .IsUnique();

        modelBuilder.Entity<Recruiter>()
            .HasIndex(recruiter => recruiter.Login)
            .IsUnique();

        modelBuilder.Entity<Application>()
            .HasIndex(application => new { application.CandidateId, application.VacancyId })
            .IsUnique();

        modelBuilder.Entity<Vacancy>()
            .ToTable(table =>
            {
                table.HasCheckConstraint("CK_Vacancies_Status", "\"Status\" IN ('Open','Paused','Closed')");
                table.HasCheckConstraint("CK_Vacancies_SalaryRange", "CAST(\"SalaryMax\" AS REAL) >= CAST(\"SalaryMin\" AS REAL)");
            });

        modelBuilder.Entity<Application>()
            .ToTable(table =>
            {
                table.HasCheckConstraint("CK_Applications_Status", "\"Status\" IN ('New','Screening','Interview','TestTask','Offer','Hired','Rejected')");
            });

        modelBuilder.Entity<InterviewFeedback>()
            .ToTable(table =>
            {
                table.HasCheckConstraint("CK_InterviewFeedbacks_Score", "\"Score\" BETWEEN 1 AND 10");
            });

        modelBuilder.Entity<SoftSkillAssessment>()
            .ToTable(table =>
            {
                table.HasCheckConstraint("CK_SoftSkillAssessments_Communication", "\"Communication\" BETWEEN 1 AND 10");
                table.HasCheckConstraint("CK_SoftSkillAssessments_Teamwork", "\"Teamwork\" BETWEEN 1 AND 10");
                table.HasCheckConstraint("CK_SoftSkillAssessments_Responsibility", "\"Responsibility\" BETWEEN 1 AND 10");
                table.HasCheckConstraint("CK_SoftSkillAssessments_StressResistance", "\"StressResistance\" BETWEEN 1 AND 10");
                table.HasCheckConstraint("CK_SoftSkillAssessments_Leadership", "\"Leadership\" BETWEEN 1 AND 10");
            });

        modelBuilder.Entity<Vacancy>()
            .Property(vacancy => vacancy.SalaryMin)
            .HasColumnType("TEXT");

        modelBuilder.Entity<Vacancy>()
            .Property(vacancy => vacancy.SalaryMax)
            .HasColumnType("TEXT");

        modelBuilder.Entity<Interview>()
            .HasOne(interview => interview.Recruiter)
            .WithMany(recruiter => recruiter.Interviews)
            .HasForeignKey(interview => interview.RecruiterId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<InterviewFeedback>()
            .HasOne(feedback => feedback.Recruiter)
            .WithMany(recruiter => recruiter.Feedbacks)
            .HasForeignKey(feedback => feedback.RecruiterId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
