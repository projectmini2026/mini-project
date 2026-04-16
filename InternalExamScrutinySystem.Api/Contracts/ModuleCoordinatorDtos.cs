using System.ComponentModel.DataAnnotations;
using InternalExamScrutinySystem.Api.Data;

namespace InternalExamScrutinySystem.Api.Contracts;

public class ApiResponse<T>
{
    public bool success { get; set; }
    public string message { get; set; } = string.Empty;
    public T? data { get; set; }   // ? nullable
}

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class FacultyDto
{
    public required int userId { get; set; }
    public required string name { get; set; }
    public string? email { get; set; }
}

public class ModuleSubjectDto
{
    public required string subjectCode { get; set; }
    public required string subjectName { get; set; }
}

public class ModuleDto
{
    public required int id { get; set; }
    public required string moduleName { get; set; }
    public string? semester { get; set; }
    public string? scrutinizerName { get; set; }
    public required List<ModuleSubjectDto> subjects { get; set; }
    public required List<FacultyDto> faculties { get; set; }
}

public class AssignScrutinizerToModuleRequest
{
    public int moduleId { get; set; }
    public int facultyId { get; set; }
    public string? remarks { get; set; }
}

public class QuestionPaperDto
{
    public required int id { get; set; }
    public required int moduleId { get; set; }
    public required int subjectId { get; set; }
    public string? moduleName { get; set; }

    public required string subjectCode { get; set; }

    public string? subjectName { get; set; }

    public required int submittedByFacultyUserId { get; set; }
    public required string submittedByFacultyName { get; set; }

    public string? fileUrl { get; set; }
    public required DateTime submittedDateUtc { get; set; }

    public required string status { get; set; }

    public int? scrutinizerUserId { get; set; }
    public string? scrutinizerName { get; set; }   // ? only once
    public string? reportJson { get; set; }
    public string? semester { get; set; }
    public string? series { get; set; }
    public string? examName { get; set; }
    public int? examId { get; set; }
    public DateTime? lastDateToUpload { get; set; }
    public string? academicYear { get; set; }

    public bool uploaded { get; set; }
    public int version { get; set; }
}

public class AssignScrutinizerRequest
{
    [Required]
    public int scrutinizerUserId { get; set; }
}