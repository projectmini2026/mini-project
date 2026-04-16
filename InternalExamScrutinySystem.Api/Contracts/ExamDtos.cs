using System.ComponentModel.DataAnnotations;

namespace InternalExamScrutinySystem.Api.Contracts;

public class SelectedSubjectInfo
{
    public int SubjectId { get; set; }
    public string Semester { get; set; } = string.Empty;
}

public class CreateExamRequest
{
    [Required]
    public string ExamName { get; set; } = string.Empty;

    public string AcademicYear { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    public DateTime LastDateToUpload { get; set; }

    public string ActiveSeries { get; set; } = "Series 1";

    public List<SelectedSubjectInfo> SelectedSubjects { get; set; } = new();
}

public class ExamResponse
{
    public int Id { get; set; }
    public string ExamName { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime LastDateToUpload { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string ActiveSeries { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public List<SelectedSubjectInfo> SelectedSubjects { get; set; } = new();
}
