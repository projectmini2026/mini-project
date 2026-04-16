using Microsoft.EntityFrameworkCore;

namespace InternalExamScrutinySystem.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<ModuleSubject> ModuleSubjects => Set<ModuleSubject>();
    public DbSet<FacultySubjectAssignment> FacultySubjectAssignments => Set<FacultySubjectAssignment>();
    public DbSet<FacultyAssignment> FacultyAssignments => Set<FacultyAssignment>();
    public DbSet<QuestionPaper> QuestionPapers => Set<QuestionPaper>();
    public DbSet<ScrutinyAssignment> ScrutinyAssignments => Set<ScrutinyAssignment>();
    public DbSet<ScrutinizerAssignment> ScrutinizerAssignments => Set<ScrutinizerAssignment>();
    public DbSet<ScrutinyReport> ScrutinyReports => Set<ScrutinyReport>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamSubject> ExamSubjects => Set<ExamSubject>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>().ToTable("Users");

        modelBuilder.Entity<AppUser>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<ModuleSubject>()
            .HasOne(s => s.Module)
            .WithMany(m => m.Subjects)
            .HasForeignKey(s => s.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Explicitly map as 1-to-many to prevent EF from inferring a 1-to-1 unique index
        modelBuilder.Entity<Module>()
            .HasOne(m => m.ModuleCoordinator)
            .WithMany()
            .HasForeignKey(m => m.CoordinatorId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AppUser>()
            .HasOne(u => u.AssignedModule)
            .WithMany()
            .HasForeignKey(u => u.ModuleId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // FacultyAssignments Cascade Deletion
        modelBuilder.Entity<FacultyAssignment>()
            .HasOne(fa => fa.Faculty)
            .WithMany()
            .HasForeignKey(fa => fa.FacultyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FacultyAssignment>()
            .HasOne(fa => fa.Module)
            .WithMany()
            .HasForeignKey(fa => fa.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);


        // QuestionPaper Cascade Deletion
        modelBuilder.Entity<QuestionPaper>()
            .HasOne(qp => qp.Module)
            .WithMany()
            .HasForeignKey(qp => qp.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ScrutinizerAssignment>()
            .HasIndex(sa => sa.ModuleId)
            .IsUnique();

        modelBuilder.Entity<ScrutinizerAssignment>()
            .HasOne(sa => sa.Module)
            .WithMany()
            .HasForeignKey(sa => sa.ModuleId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ScrutinizerAssignment>()
            .HasOne(sa => sa.Faculty)
            .WithMany()
            .HasForeignKey(sa => sa.FacultyId)
            .OnDelete(DeleteBehavior.NoAction);

        // FacultySubjectAssignment - Use NoAction to avoid multiple cascade paths (SQL Error 1785)
        modelBuilder.Entity<FacultySubjectAssignment>()
            .HasOne(a => a.Faculty)
            .WithMany()
            .HasForeignKey(a => a.FacultyId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<FacultySubjectAssignment>()
            .HasOne(a => a.Module)
            .WithMany()
            .HasForeignKey(a => a.ModuleId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<FacultySubjectAssignment>()
            .HasOne(a => a.Subject)
            .WithMany()
            .HasForeignKey(a => a.SubjectId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ExamSubject>()
            .HasOne(es => es.Exam)
            .WithMany(e => e.ExamSubjects)
            .HasForeignKey(es => es.ExamId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExamSubject>()
            .HasOne(es => es.Subject)
            .WithMany()
            .HasForeignKey(es => es.SubjectId)
            .OnDelete(DeleteBehavior.NoAction);

        // New Scrutiny Relationships - NoAction to avoid cascade conflicts
        modelBuilder.Entity<QuestionPaper>()
            .HasOne(qp => qp.Scrutinizer)
            .WithMany()
            .HasForeignKey(qp => qp.ScrutinizerUserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ScrutinyAssignment>()
            .HasOne(sa => sa.QuestionPaper)
            .WithMany()
            .HasForeignKey(sa => sa.QuestionPaperId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ScrutinyAssignment>()
            .HasOne(sa => sa.Scrutinizer)
            .WithMany()
            .HasForeignKey(sa => sa.ScrutinizerUserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ScrutinyReport>()
            .HasOne(sr => sr.QuestionPaper)
            .WithMany()
            .HasForeignKey(sr => sr.QuestionPaperId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ScrutinyReport>()
            .HasOne(sr => sr.Scrutinizer)
            .WithMany()
            .HasForeignKey(sr => sr.ScrutinizerUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

