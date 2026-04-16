using System.Security.Claims;
using InternalExamScrutinySystem.Api.Contracts;
using InternalExamScrutinySystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternalExamScrutinySystem.Api.Controllers;

[ApiController]
[Route("api/module-coordinator")]
[Authorize(Roles = "ModuleCoordinator")]
public class ModuleCoordinatorController : ControllerBase
{
    private readonly IModuleCoordinatorService _moduleCoordinatorService;

    public ModuleCoordinatorController(IModuleCoordinatorService moduleCoordinatorService)
    {
        _moduleCoordinatorService = moduleCoordinatorService;
    }

    private int GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.Parse(value!);
    }

    [HttpGet("me/modules")]
    public async Task<ActionResult<ApiResponse<List<ModuleDto>>>> GetMyModules(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        return Ok(await _moduleCoordinatorService.GetMyModulesAsync(userId, cancellationToken));
    }

    [HttpGet("me/papers")]
    public async Task<ActionResult<ApiResponse<List<QuestionPaperDto>>>> GetPapers([FromQuery] int moduleId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        return Ok(await _moduleCoordinatorService.GetPapersByModuleAsync(userId, moduleId, cancellationToken));
    }

    [HttpPost("papers/{paperId:int}/assign-scrutinizer")]
    public async Task<ActionResult<ApiResponse<object>>> AssignScrutinizer(
        [FromRoute] int paperId,
        [FromBody] AssignScrutinizerRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        return Ok(await _moduleCoordinatorService.AssignScrutinizerAsync(userId, paperId, request.scrutinizerUserId, cancellationToken));
    }

    [HttpPost("papers/{paperId:int}/approve")]
    public async Task<ActionResult<ApiResponse<object>>> ApproveReport([FromRoute] int paperId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        return Ok(await _moduleCoordinatorService.ApproveReportAsync(userId, paperId, cancellationToken));
    }

    [HttpPost("papers/{paperId:int}/finalize")]
    public async Task<ActionResult<ApiResponse<object>>> FinalizeReport([FromRoute] int paperId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        return Ok(await _moduleCoordinatorService.FinalizeReportAsync(userId, paperId, cancellationToken));
    }

    [HttpPost("papers/{paperId:int}/request-correction")]
    public async Task<ActionResult<ApiResponse<object>>> RequestCorrection([FromRoute] int paperId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        return Ok(await _moduleCoordinatorService.RequestCorrectionAsync(userId, paperId, cancellationToken));
    }

    [HttpPost("assign-faculty")]
    public async Task<ActionResult<ApiResponse<object>>> AssignFaculty([FromBody] AssignFacultyToSubjectRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        return Ok(await _moduleCoordinatorService.AssignFacultyToSubjectAsync(userId, request, cancellationToken));
    }

    // --- New endpoints for Module User Dashboard ---

    [HttpGet("/api/modules/user/{userId}")]
    public async Task<ActionResult<ApiResponse<List<ModuleDto>>>> GetModulesForUser([FromRoute] int userId, CancellationToken cancellationToken)
    {
        // For now, we allow any logged-in user to call this if they are a ModuleCoordinator, 
        // but ideally we should check if the requested userId matches the token userId.
        var currentUserId = GetUserId();
        if (currentUserId != userId) return Forbid();

        return Ok(await _moduleCoordinatorService.GetMyModulesAsync(userId, cancellationToken));
    }

    [HttpGet("/api/faculty")]
    [AllowAnonymous] // Or restrict as needed, but requirements imply a general fetch
    public async Task<ActionResult<ApiResponse<List<FacultyDto>>>> GetFacultyList(CancellationToken cancellationToken)
    {
        return Ok(await _moduleCoordinatorService.GetFacultyListAsync(cancellationToken));
    }

    [HttpPost("/api/scrutinizer/assign")]
    public async Task<ActionResult<ApiResponse<object>>> AssignScrutinizerToModule([FromBody] AssignScrutinizerToModuleRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        return Ok(await _moduleCoordinatorService.AssignScrutinizerToModuleAsync(userId, request, cancellationToken));
    }
}

