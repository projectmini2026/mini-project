using InternalExamScrutinySystem.Api.Contracts;
using InternalExamScrutinySystem.Api.Data;
using InternalExamScrutinySystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternalExamScrutinySystem.Api.Controllers;

[ApiController]
[Route("api/hod")]
[Authorize(Roles = "HOD")]
public class HodController : ControllerBase
{
    private readonly IHodService _hodService;
    private readonly AppDbContext _db;

    public HodController(IHodService hodService, AppDbContext db)
    {
        _hodService = hodService;
        _db = db;
    }

    [HttpPut("profile")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateProfile([FromBody] UpdateHodProfileRequest request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return Unauthorized();

        int userId = int.Parse(userIdClaim.Value);
        var user = await _db.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null) return NotFound();

        user.Name = request.Name;
        user.Email = request.Email;

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new ApiResponse<object> { success = true, message = "Profile updated in database." });
    }


    [HttpGet("modules")]
    public async Task<ActionResult<ApiResponse<List<ModuleListDto>>>> GetModules(CancellationToken cancellationToken)
    {
        var response = await _hodService.GetModulesAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost("modules")]
    public async Task<ActionResult<ApiResponse<object>>> CreateModule([FromBody] CreateModuleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Extract User ID from token claims
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            int currentUserId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

            Console.WriteLine($"[DEBUG] CreateModule hit. User: {currentUserId}. Payload: {System.Text.Json.JsonSerializer.Serialize(request)}");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                Console.WriteLine($"[DEBUG] Validation failed: {string.Join(", ", errors)}");

                return BadRequest(new ApiResponse<object> { 
                    success = false, 
                    message = "Validation failed", 
                    data = errors
                });
            }

            var response = await _hodService.CreateModuleAsync(request, currentUserId, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] CreateModule Exception: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            if (ex.InnerException != null) Console.WriteLine($"[ERROR] Inner Exception: {ex.InnerException.Message}");
            
            return StatusCode(500, new ApiResponse<object> { 
                success = false, 
                message = "An internal server error occurred.", 
                data = ex.Message + (ex.InnerException != null ? $" | Inner: {ex.InnerException.Message}" : "")
            });
        }
    }

    [HttpPost("assign-coordinator/{moduleId}/faculty/{facultyId}")]
    public async Task<ActionResult<ApiResponse<object>>> AssignCoordinator(int moduleId, int facultyId, CancellationToken cancellationToken)
    {
        var response = await _hodService.AssignModuleCoordinatorAsync(moduleId, facultyId, cancellationToken);
        return Ok(response);
    }

    [HttpPut("users/{id}/role")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateUserRole(int id, UpdateUserRoleRequest request, CancellationToken cancellationToken)
    {
        var response = await _hodService.UpdateUserRoleAsync(id, request.Role, cancellationToken);
        return Ok(response);
    }

    [HttpPut("modules/{id}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateModule(int id, [FromBody] UpdateModuleRequest request, CancellationToken cancellationToken)
    {
        try
        {
             // Log incoming request
            Console.WriteLine($"Updating module {id}: {System.Text.Json.JsonSerializer.Serialize(request)}");

            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object> { 
                    success = false, 
                    message = "Validation failed", 
                    data = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) 
                });
            }

            var response = await _hodService.UpdateModuleAsync(id, request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in UpdateModule: {ex.Message}");
            Console.WriteLine(ex.StackTrace);

            return StatusCode(500, new ApiResponse<object> { 
                success = false, 
                message = $"An error occurred: {ex.Message}", 
                data = ex.InnerException?.Message 
            });
        }
    }

    [HttpDelete("modules/{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteModule(int id, CancellationToken cancellationToken)
    {
        var response = await _hodService.DeleteModuleAsync(id, cancellationToken);
        return Ok(response);
    }

    [HttpDelete("subjects/{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteSubject(int id, CancellationToken cancellationToken)
    {
        var response = await _hodService.DeleteSubjectAsync(id, cancellationToken);
        return Ok(response);
    }

    [HttpPost("assign-faculty")]
    public async Task<ActionResult<ApiResponse<object>>> AssignFaculty([FromBody] AssignFacultyToSubjectRequest request, CancellationToken cancellationToken)
    {
        var response = await _hodService.AssignFacultyToSubjectAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPut("modules/{id:int}/coordinator")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateCoordinator([FromRoute] int id, [FromBody] UpdateModuleCoordinatorRequest request, CancellationToken cancellationToken)
    {
        var response = await _hodService.UpdateModuleCoordinatorAsync(id, request.FacultyId, cancellationToken);
        return Ok(response);
    }

    // ================= FACULTY MANAGEMENT =================

    [HttpGet("faculties")]
    public async Task<ActionResult<ApiResponse<List<FacultyResponseDto>>>> GetFaculties([FromQuery] int? moduleId, CancellationToken cancellationToken)
    {
        var response = await _hodService.GetFacultiesAsync(moduleId, cancellationToken);
        return Ok(response);
    }

    [HttpGet("faculty/roster")]
    public async Task<ActionResult<ApiResponse<List<FacultyRosterDto>>>> GetFacultyRoster(CancellationToken cancellationToken)
    {
        var response = await _hodService.GetFacultyRosterAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost("faculties")]
    public async Task<ActionResult<ApiResponse<object>>> CreateFaculty([FromBody] CreateFacultyRequest request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[DEBUG] CreateFaculty hit. Payload: {System.Text.Json.JsonSerializer.Serialize(request)}");
        var response = await _hodService.CreateFacultyAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPut("faculties/{id}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateFaculty(int id, [FromBody] UpdateFacultyRequest request, CancellationToken cancellationToken)
    {
        var response = await _hodService.UpdateFacultyAsync(id, request, cancellationToken);
        return Ok(response);
    }

    [HttpDelete("faculties/{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteFaculty(int id, CancellationToken cancellationToken)
    {
        var response = await _hodService.DeleteFacultyAsync(id, cancellationToken);
        return Ok(response);
    }

    // ================= MODULE ASSIGNMENTS =================

    [HttpGet("module-assignments")]
    public async Task<ActionResult<ApiResponse<List<ModuleAssignmentResponse>>>> GetModuleAssignments(CancellationToken cancellationToken)
    {
        var response = await _hodService.GetModuleAssignmentsAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost("module-assignments")]
    public async Task<ActionResult<ApiResponse<object>>> AssignModule([FromBody] CreateModuleAssignmentRequest request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        int currentUserId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        
        var response = await _hodService.AssignModuleAsync(request, currentUserId, cancellationToken);
        return Ok(response);
    }

    [HttpDelete("module-assignments/{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteModuleAssignment(int id, CancellationToken cancellationToken)
    {
        var response = await _hodService.DeleteModuleAssignmentAsync(id, cancellationToken);
        return Ok(response);
    }
}
