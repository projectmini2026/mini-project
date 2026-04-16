using System.ComponentModel.DataAnnotations;
using InternalExamScrutinySystem.Api.Data;

namespace InternalExamScrutinySystem.Api.Contracts;

public class CreateFacultyRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    public Position? Position { get; set; }

    public int? ModuleId { get; set; }
}

public class UpdateFacultyRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    public Position? Position { get; set; }

    public int? ModuleId { get; set; }
}

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(6, ErrorMessage = "New password must be at least 6 characters")]
    public string NewPassword { get; set; } = string.Empty;
}

public class FacultyResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Role Role { get; set; }
    public Position? Position { get; set; }
    public int? ModuleId { get; set; }
    public string? ModuleName { get; set; }
    public bool IsFirstLogin { get; set; }
}
public class FacultyRosterDto
{
    public int ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public List<FacultyResponseDto> Faculty { get; set; } = new();
}

public class FacultyAssignmentDto
{
    public int Id { get; set; }
    public int FacultyId { get; set; }
    public string FacultyName { get; set; } = string.Empty;
    public int ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public DateTime AssignedDate { get; set; }
}

public class CreateFacultyAssignmentRequest
{
    [Required]
    public int FacultyId { get; set; }

    [Required]
    public int ModuleId { get; set; }
}

public class FacultySubjectAssignmentResponse
{
    public int ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string? Semester { get; set; }
    public int SubjectId { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string Status { get; set; } = "NotSubmitted";
    public string? AcademicYear { get; set; }
    public int? ExamId { get; set; }
    public string? ExamName { get; set; }
    public List<SeriesStatusDto> SeriesStatus { get; set; } = new();
}

public class SeriesStatusDto
{
    public int? Id { get; set; }
    public string Series { get; set; } = string.Empty;
    public string Status { get; set; } = "NotSubmitted";
    public DateTime? SubmittedDate { get; set; }
    public string? FileUrl { get; set; }
    public string? ReportJson { get; set; }
    public string? AcademicYear { get; set; }
    public int? ExamId { get; set; }
    public string? V1Status { get; set; }
    public DateTime? V1SubmittedDate { get; set; }
    public string? V2Status { get; set; }
    public DateTime? V2SubmittedDate { get; set; }
    public int version { get; set; }
    public string? ScrutinizerName { get; set; }
    public int? ScrutinizerUserId { get; set; }
}

public class UploadQnPaperRequest
{
    [Required]
    public int ModuleId { get; set; }
    
    [Required]
    public int SubjectId { get; set; }

    [Required]
    public string SubjectCode { get; set; } = string.Empty;

    
    [Required]
    public IFormFile File { get; set; } = null!;

    public string? Semester { get; set; }

    [Required]
    public string Series { get; set; } = "Series 1";

    public string? AcademicYear { get; set; }

    public bool IsCorrection { get; set; } = false;
}


public class ScrutinyAssignmentResponse
{
    public int PaperId { get; set; }
    public int ModuleId { get; set; }

    public string ModuleName { get; set; } = string.Empty;
    public int SubjectId { get; set; }

    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string SubmittedByFacultyName { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Semester { get; set; }
    public string? ExamName { get; set; }
    public int? ExamId { get; set; }
    public string? Series { get; set; }
    public DateTime SubmittedDateUtc { get; set; }
}

public class SubmitScrutinyReportRequest
{
    [Required]
    public int PaperId { get; set; }

    [Required]
    public string ReportJson { get; set; } = "{}";
}
