using InternalExamScrutinySystem.Api.Contracts;
using InternalExamScrutinySystem.Api.Data;
using InternalExamScrutinySystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternalExamScrutinySystem.Api.Controllers;

[ApiController]
[Route("api/ec")]
[Authorize(Roles = "ExamCoordinator")]
public class EcController : ControllerBase
{
    private readonly IExamCoordinatorService _ecService;

    public EcController(IExamCoordinatorService ecService)
    {
        _ecService = ecService;
    }

    [HttpGet("assignments")]
    public async Task<ActionResult<ApiResponse<List<EcAssignmentResponse>>>> GetAssignments(CancellationToken cancellationToken)
    {
        var response = await _ecService.GetAssignmentsAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost("assignments")]
    public async Task<ActionResult<ApiResponse<object>>> CreateAssignment([FromBody] CreateEcAssignmentRequest request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return Unauthorized();

        int ecId = int.Parse(userIdClaim.Value);
        var response = await _ecService.CreateAssignmentAsync(request, ecId, cancellationToken);
        return Ok(response);
    }

    [HttpPut("assignments/{id}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateAssignment(int id, [FromBody] UpdateEcAssignmentRequest request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return Unauthorized();

        int ecId = int.Parse(userIdClaim.Value);
        var response = await _ecService.UpdateAssignmentAsync(id, request, ecId, cancellationToken);
        return Ok(response);
    }

    [HttpDelete("assignments/{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAssignment(int id, CancellationToken cancellationToken)
    {
        var response = await _ecService.DeleteAssignmentAsync(id, cancellationToken);
        return Ok(response);
    }

    // ================= MODULE ASSIGNMENTS =================

    [HttpGet("module-assignments")]
    public async Task<ActionResult<ApiResponse<List<ModuleAssignmentResponse>>>> GetModuleAssignments(CancellationToken cancellationToken)
    {
        var response = await _ecService.GetModuleAssignmentsAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost("module-assignments")]
    public async Task<ActionResult<ApiResponse<object>>> AssignModule([FromBody] CreateModuleAssignmentRequest request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return Unauthorized();

        int ecId = int.Parse(userIdClaim.Value);
        var response = await _ecService.AssignModuleAsync(request, ecId, cancellationToken);
        return Ok(response);
    }

    [HttpDelete("module-assignments/{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteModuleAssignment(int id, CancellationToken cancellationToken)
    {
        var response = await _ecService.DeleteModuleAssignmentAsync(id, cancellationToken);
        return Ok(response);
    }

    [HttpGet("modules-data")]
    public async Task<ActionResult<ApiResponse<object>>> GetModulesData(CancellationToken cancellationToken)
    {
        var response = await _ecService.GetModulesWithSubjectsAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("faculty-roster")]
    public async Task<ActionResult<ApiResponse<object>>> GetFacultyRoster(CancellationToken cancellationToken)
    {
        var response = await _ecService.GetFacultyRosterAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("subjects-by-semester/{semester}")]
    public async Task<ActionResult<ApiResponse<object>>> GetSubjectsBySemester(string semester, CancellationToken cancellationToken)
    {
        var response = await _ecService.GetSubjectsBySemesterAsync(semester, cancellationToken);
        return Ok(response);
    }

    [HttpGet("subjects-all")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> GetAllSubjects(CancellationToken cancellationToken)
    {
        var response = await _ecService.GetAllSubjectsAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost("seed-data")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> ForceSeed(CancellationToken cancellationToken)
    {
        var response = await _ecService.ForceSeedAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("awaiting-approval")]
    public async Task<ActionResult<ApiResponse<List<AwaitingApprovalPaperDto>>>> GetAwaitingApprovalPapers(CancellationToken cancellationToken)
    {
        var response = await _ecService.GetAwaitingApprovalPapersAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost("approve-paper/{id}")]
    public async Task<ActionResult<ApiResponse<object>>> ApproveFinalPaper(int id, CancellationToken cancellationToken)
    {
        var response = await _ecService.ApproveFinalPaperAsync(id, cancellationToken);
        return Ok(response);
    }

    [HttpPost("request-correction/{id}")]
    public async Task<ActionResult<ApiResponse<object>>> RequestCorrection(int id, CancellationToken cancellationToken)
    {
        var response = await _ecService.RequestCorrectionAsync(id, cancellationToken);
        return Ok(response);
    }
}
