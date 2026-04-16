using System.ComponentModel.DataAnnotations;
using InternalExamScrutinySystem.Api.Data;

namespace InternalExamScrutinySystem.Api.Contracts;

public class CreateModuleRequest
{
    [Required(ErrorMessage = "Module Name is required")]
    [MaxLength(200, ErrorMessage = "Module Name cannot exceed 200 characters")]
    public string ModuleName { get; set; } = string.Empty;

    [Required(ErrorMessage = "At least one subject is required")]
    [MinLength(1, ErrorMessage = "At least one subject is required")]
    public List<CreateModuleSubjectRequest> Subjects { get; set; } = new();

    public string? Semester { get; set; }
}

public class UpdateModuleRequest
{
    [Required]
    public string ModuleName { get; set; } = null!;

    public string? Semester { get; set; }

    public List<CreateModuleSubjectRequest> Subjects { get; set; } = new();
}

public class CreateModuleSubjectRequest
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Subject Code is required")]
    [MaxLength(50, ErrorMessage = "Subject Code cannot exceed 50 characters")]
    public string SubjectCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Subject Name is required")]
    [MaxLength(200, ErrorMessage = "Subject Name cannot exceed 200 characters")]
    public string SubjectName { get; set; } = string.Empty;
}

public class AssignCoordinatorRequest
{
    [Required]
    public int UserId { get; set; }
}

public class UpdateUserRoleRequest
{
    [Required]
    public Role Role { get; set; }
}

public class UserListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public Role Role { get; set; }
}

public class ModuleListDto
{
    public int Id { get; set; }
    public string ModuleName { get; set; } = null!;
    public List<CreateModuleSubjectRequest> Subjects { get; set; } = new();
    public string? Semester { get; set; }
    public int? CoordinatorId { get; set; }
    public string? CoordinatorName { get; set; }
    public List<string> Teachers { get; set; } = new();
}

public class UpdateModuleCoordinatorRequest
{
    [Required]
    public int FacultyId { get; set; }
}

public class UpdateHodProfileRequest
{
    [Required]
    public string Name { get; set; } = null!;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;
}

