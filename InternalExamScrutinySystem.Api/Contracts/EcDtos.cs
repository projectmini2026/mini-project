using System.ComponentModel.DataAnnotations;

namespace InternalExamScrutinySystem.Api.Contracts;

public class CreateEcAssignmentRequest
{
    [Required]
    public int FacultyId { get; set; }

    [Required]
    public int ModuleId { get; set; }

    [Required]
    public int SubjectId { get; set; }

    public string? Semester { get; set; }
}

public class CreateModuleAssignmentRequest
{
    [Required]
    public int ModuleId { get; set; }

    [Required]
    public List<int> FacultyIds { get; set; } = new();
}

public class UpdateEcAssignmentRequest : CreateEcAssignmentRequest
{
}

public class EcAssignmentResponse
{
    public int Id { get; set; }
    public int FacultyId { get; set; }
    public string FacultyName { get; set; } = null!;
    public string FacultyDesignation { get; set; } = null!;
    public int ModuleId { get; set; }
    public string ModuleName { get; set; } = null!;
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = null!;
    public string SubjectCode { get; set; } = null!;
    public string AssignedBy { get; set; } = null!;
    public string Status { get; set; } = "NotSubmitted";
    public DateTime? SubmittedDate { get; set; }
    public string? ScrutinizerName { get; set; }
    public string? Semester { get; set; }
    public int? PaperId { get; set; }
    public string? FileUrl { get; set; }
    public string? AcademicYear { get; set; }

    public DateTime AssignedAtUtc { get; set; }
    public List<SeriesStatusDto> SeriesStatus { get; set; } = new();
}

public class ModuleAssignmentResponse
{
    public int Id { get; set; }
    public int FacultyId { get; set; }
    public string FacultyName { get; set; } = null!;
    public string FacultyEmail { get; set; } = null!;
    public string FacultyDesignation { get; set; } = null!;
    public int ModuleId { get; set; }
    public string ModuleName { get; set; } = null!;
    public DateTime AssignedAtUtc { get; set; }
}

public class AwaitingApprovalPaperDto
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    public string ModuleName { get; set; } = null!;
    public string SubjectCode { get; set; } = null!;
    public string SubjectName { get; set; } = null!;
    public string FacultyName { get; set; } = null!;
    public string FileUrl { get; set; } = null!;
    public DateTime SubmittedDateUtc { get; set; }
    public string Status { get; set; } = null!;
    public string? Semester { get; set; }
    public int Version { get; set; }
    public string? AcademicYear { get; set; }
}
