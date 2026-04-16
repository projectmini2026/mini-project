using InternalExamScrutinySystem.Api.Contracts;
using InternalExamScrutinySystem.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternalExamScrutinySystem.Api.Controllers;

[ApiController]
[Route("api/faculty-assignments")]
[Authorize(Roles = "HOD")]
public class FacultyAssignmentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public FacultyAssignmentsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> CreateAssignment([FromBody] CreateFacultyAssignmentRequest request, CancellationToken cancellationToken)
    {
        var faculty = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.FacultyId, cancellationToken);
        if (faculty == null || (faculty.RoleId != Role.Faculty && faculty.RoleId != Role.Scrutinizer && faculty.RoleId != Role.ModuleCoordinator))
            return BadRequest(new ApiResponse<object> { success = false, message = "Valid faculty member not found." });

        var module = await _db.Modules.FindAsync(new object[] { request.ModuleId }, cancellationToken);
        if (module == null)
            return BadRequest(new ApiResponse<object> { success = false, message = "Module not found." });

        // Check if assignment already exists
        var existing = await _db.FacultyAssignments
            .FirstOrDefaultAsync(fa => fa.FacultyId == request.FacultyId && fa.ModuleId == request.ModuleId, cancellationToken);

        if (existing != null)
            return Ok(new ApiResponse<object> { success = true, message = "Faculty is already assigned to this module." });

        var assignment = new FacultyAssignment
        {
            FacultyId = request.FacultyId,
            ModuleId = request.ModuleId
        };
        
        // Also ensure the user's base ModuleId is populated if it isn't, based on the previous system mapping needs
        if (faculty.ModuleId == null)
        {
            faculty.ModuleId = request.ModuleId;
        }

        _db.FacultyAssignments.Add(assignment);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new ApiResponse<object> { success = true, message = "Faculty assigned successfully." });
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<FacultyAssignmentDto>>>> GetAssignments(CancellationToken cancellationToken)
    {
        var assignments = await _db.FacultyAssignments
            .Include(fa => fa.Faculty)
            .Include(fa => fa.Module)
            .Select(fa => new FacultyAssignmentDto
            {
                Id = fa.Id,
                FacultyId = fa.FacultyId,
                FacultyName = fa.Faculty != null ? fa.Faculty.Name : "Unknown",
                ModuleId = fa.ModuleId,
                ModuleName = fa.Module != null ? fa.Module.ModuleName : "Unknown",
                AssignedDate = fa.AssignedDate
            })
            .ToListAsync(cancellationToken);

        return Ok(new ApiResponse<List<FacultyAssignmentDto>> { success = true, message = "Faculty assignments fetched.", data = assignments });
    }

    [HttpGet("subjects")]
    public async Task<ActionResult<ApiResponse<List<EcAssignmentResponse>>>> GetSubjectAssignments(CancellationToken cancellationToken)
    {
        var assignments = await _db.FacultySubjectAssignments
            .Include(a => a.Faculty)
            .Include(a => a.Module)
            .Include(a => a.Subject)
            .Select(a => new EcAssignmentResponse
            {
                Id = a.Id,
                FacultyId = a.FacultyId,
                FacultyName = a.Faculty != null ? a.Faculty.Name : "Unknown",
                FacultyDesignation = a.Faculty != null ? a.Faculty.Position.ToString()! : "Faculty",
                ModuleId = a.ModuleId,
                ModuleName = a.Module != null ? a.Module.ModuleName : "Unknown",
                SubjectId = a.SubjectId,
                SubjectName = a.Subject != null ? a.Subject.SubjectName : "Unknown",
                SubjectCode = a.Subject != null ? a.Subject.SubjectCode : "Unknown",
                AssignedAtUtc = a.AssignedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(new ApiResponse<List<EcAssignmentResponse>> { success = true, message = "Subject assignments fetched.", data = assignments });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAssignment(int id, CancellationToken cancellationToken)
    {
        var assignment = await _db.FacultyAssignments.FindAsync(new object[] { id }, cancellationToken);
        if (assignment == null)
            return NotFound(new ApiResponse<object> { success = false, message = "Assignment not found." });

        _db.FacultyAssignments.Remove(assignment);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new ApiResponse<object> { success = true, message = "Assignment deleted successfully." });
    }
}
