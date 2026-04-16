using System.ComponentModel.DataAnnotations;

namespace InternalExamScrutinySystem.Api.Contracts;

public class AssignFacultyToSubjectRequest
{
    [Required]
    public int FacultyId { get; set; }

    [Required]
    public int ModuleId { get; set; }

    [Required]
    public string SubjectName { get; set; } = string.Empty;
}
