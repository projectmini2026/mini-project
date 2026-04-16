using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternalExamScrutinySystem.Api.Data;

public enum Role
{
    Faculty = 0,
    HOD = 1,
    ExamCoordinator = 2,
    ModuleCoordinator = 3,
    Scrutinizer = 4
}

public enum Position
{
    Professor = 1,
    AssociateProfessor = 2,
    AssistantProfessor = 3,
    GuestLecturer = 4,
    Doctorate = 5
}

public enum WorkflowStatus
{
    NotSubmitted = 1,
    Submitted = 2,
    UnderScrutiny = 3,
    CorrectionRequired = 4,
    CorrectedSubmitted = 5,
    AwaitingMCApproval = 6,
    AwaitingECApproval = 7,
    Approved = 8,
    SentForPrinting = 9,
    Finalized = 10,
    SentForCorrection = 11
}

public class AppUser
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    [Required]
    [MaxLength(256)]
    public string Email { get; set; } = null!;

    [Required]
    public string PasswordHash { get; set; } = null!;

    public Role? RoleId { get; set; } = Role.Faculty;

    public bool? IsFirstLogin { get; set; } = true;

    public Position? Position { get; set; }

    public int? ModuleId { get; set; }
    
    [ForeignKey("ModuleId")]
    public Module? AssignedModule { get; set; }
}

public class Module
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string ModuleCode { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    [Column("Name")]
    public string ModuleName { get; set; } = null!;

    // HOD assigns this
    public int? CoordinatorId { get; set; }

    [ForeignKey("CoordinatorId")]
    public virtual AppUser? ModuleCoordinator { get; set; }

    [MaxLength(50)]
    public string? Semester { get; set; }

    public ICollection<ModuleSubject> Subjects { get; set; } = new List<ModuleSubject>();
}

public class ScrutinizerAssignment
{
    [Key]
    public int Id { get; set; }

    public int ModuleId { get; set; }

    public int FacultyId { get; set; }

    [MaxLength(1000)]
    public string? Remarks { get; set; }

    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

    [ForeignKey("ModuleId")]
    public virtual Module? Module { get; set; }

    [ForeignKey("FacultyId")]
    public virtual AppUser? Faculty { get; set; }
}

public class ModuleSubject
{
    [Key]
    public int Id { get; set; }

    public int ModuleId { get; set; }

    [Required]
    [MaxLength(50)]
    public string SubjectCode { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string SubjectName { get; set; } = null!;

    [ForeignKey("ModuleId")]
    public virtual Module? Module { get; set; }
}

// Which faculty teaches which subject (assigned by Exam Coordinator)
public class FacultySubjectAssignment
{
    [Key]
    public int Id { get; set; }

    public int FacultyId { get; set; }

    public int ModuleId { get; set; }

    public int SubjectId { get; set; }
    
    [MaxLength(50)]
    public string? Semester { get; set; }
    
    public int? ExamId { get; set; }

    [ForeignKey("ExamId")]
    public virtual Exam? Exam { get; set; }

    public int AssignedByUserId { get; set; } // The Exam Coordinator user ID

    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;

    [ForeignKey("FacultyId")]

    public virtual AppUser? Faculty { get; set; }

    [ForeignKey("ModuleId")]
    public virtual Module? Module { get; set; }

    [ForeignKey("SubjectId")]
    public virtual ModuleSubject? Subject { get; set; }
}

public class FacultyAssignment
{
    [Key]
    public int Id { get; set; }

    public int FacultyId { get; set; }

    public int ModuleId { get; set; }

    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

    [ForeignKey("FacultyId")]
    public virtual AppUser? Faculty { get; set; }

    [ForeignKey("ModuleId")]
    public virtual Module? Module { get; set; }
}

public class QuestionPaper
{
    [Key]
    public int Id { get; set; }

    public int ModuleId { get; set; }

    [ForeignKey("ModuleId")]
    public virtual Module? Module { get; set; }

    [Required]
    [MaxLength(100)]
    public string SubjectCode { get; set; } = string.Empty;



    public int SubmittedByFacultyUserId { get; set; }

    public string? FileUrl { get; set; }

    public DateTime SubmittedDateUtc { get; set; } = DateTime.UtcNow;

    public WorkflowStatus Status { get; set; } = WorkflowStatus.NotSubmitted;

    [MaxLength(50)]
    public string? Semester { get; set; }

    public int? ScrutinizerUserId { get; set; }

    [ForeignKey("ScrutinizerUserId")]
    public virtual AppUser? Scrutinizer { get; set; }

    [MaxLength(10)]
    public string Series { get; set; } = "A";
    
    [MaxLength(20)]
    // Use Exam.AcademicYear instead of this non-existent column
    // public string? AcademicYear { get; set; }


    public int? ExamId { get; set; }

    [ForeignKey("ExamId")]
    public virtual Exam? Exam { get; set; }

    public int? SubjectId { get; set; }

    [ForeignKey("SubjectId")]
    public virtual ModuleSubject? Subject { get; set; }

    public int Version { get; set; } = 1;
}

public class ScrutinyAssignment
{
    [Key]
    public int Id { get; set; }

    public int QuestionPaperId { get; set; }

    public int ScrutinizerUserId { get; set; }

    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;

    [ForeignKey("QuestionPaperId")]
    public virtual QuestionPaper? QuestionPaper { get; set; }

    [ForeignKey("ScrutinizerUserId")]
    public virtual AppUser? Scrutinizer { get; set; }
}

public class ScrutinyReport
{
    [Key]
    public int Id { get; set; }

    public int QuestionPaperId { get; set; }

    public int ScrutinizerUserId { get; set; }

    public string ReportJson { get; set; } = "{}";

    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;

    [ForeignKey("QuestionPaperId")]
    public virtual QuestionPaper? QuestionPaper { get; set; }

    [ForeignKey("ScrutinizerUserId")]
    public virtual AppUser? Scrutinizer { get; set; }
}

public class Notification
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = null!;

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class Exam
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("Name")]
    public string ExamName { get; set; } = null!;

    public string AcademicYear { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public DateTime LastDateToUpload { get; set; }

    [MaxLength(2000)]
    public string? GeneratedMessage { get; set; }

    public bool IsActive { get; set; } = true;

    public string ActiveSeries { get; set; } = "Series 1";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public virtual ICollection<ExamSubject> ExamSubjects { get; set; } = new List<ExamSubject>();
}

public class ExamSubject
{
    [Key]
    public int Id { get; set; }

    public int ExamId { get; set; }

    public int SubjectId { get; set; }

    [MaxLength(50)]
    public string? Semester { get; set; }

    [ForeignKey("ExamId")]
    public virtual Exam? Exam { get; set; }

    [ForeignKey("SubjectId")]
    public virtual ModuleSubject? Subject { get; set; }
}
