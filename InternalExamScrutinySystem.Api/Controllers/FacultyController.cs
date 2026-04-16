using InternalExamScrutinySystem.Api.Contracts;
using InternalExamScrutinySystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InternalExamScrutinySystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FacultyController : ControllerBase
{
    private readonly IFacultyService _facultyService;

    public FacultyController(IFacultyService facultyService)
    {
        _facultyService = facultyService;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
    }

    [HttpGet("my-assignments")]
    public async Task<ActionResult<ApiResponse<List<FacultySubjectAssignmentResponse>>>> GetMyAssignments(CancellationToken cancellationToken)
    {
        int userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var response = await _facultyService.GetMyAssignmentsAsync(userId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("upload-paper")]
    public async Task<ActionResult<ApiResponse<object>>> UploadPaper([FromForm] UploadQnPaperRequest request, CancellationToken cancellationToken)
    {
        int userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var response = await _facultyService.UploadQuestionPaperAsync(userId, request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("scrutiny-assignments")]
    public async Task<ActionResult<ApiResponse<List<ScrutinyAssignmentResponse>>>> GetScrutinyAssignments(CancellationToken cancellationToken)
    {
        int userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var response = await _facultyService.GetScrutinyAssignmentsAsync(userId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("submit-scrutiny")]
    public async Task<ActionResult<ApiResponse<object>>> SubmitScrutiny(SubmitScrutinyReportRequest request, CancellationToken cancellationToken)
    {
        int userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var response = await _facultyService.SubmitScrutinyReportAsync(userId, request, cancellationToken);
        return Ok(response);
    }
}
