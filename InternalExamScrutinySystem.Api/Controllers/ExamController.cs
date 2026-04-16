using InternalExamScrutinySystem.Api.Contracts;
using InternalExamScrutinySystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace InternalExamScrutinySystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExamController : ControllerBase
{
    private readonly IExamService _examService;

    public ExamController(IExamService examService)
    {
        _examService = examService;
    }

    [HttpPost]
    [Authorize(Roles = "ExamCoordinator")]
    public async Task<ActionResult<ApiResponse<ExamResponse>>> Create([FromBody] CreateExamRequest request, CancellationToken cancellationToken)
    {
        var response = await _examService.CreateExamAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet]
    [Authorize(Roles = "ExamCoordinator")]
    public async Task<ActionResult<ApiResponse<List<ExamResponse>>>> GetExams(CancellationToken cancellationToken)
    {
        var response = await _examService.GetExamsAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("active")]
    [Authorize] // Allow all logged in users (Faculty, EC, etc) to check if an exam is active
    public async Task<ActionResult<ApiResponse<ExamResponse>>> GetActiveExam(CancellationToken cancellationToken)
    {
        var response = await _examService.GetActiveExamAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost("{id}/stop")]
    [Authorize(Roles = "ExamCoordinator")]
    public async Task<ActionResult<ApiResponse<object>>> StopExam(int id, CancellationToken cancellationToken)
    {
        var response = await _examService.StopExamAsync(id, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "ExamCoordinator")]
    public async Task<ActionResult<ApiResponse<ExamResponse>>> GetExam(int id, CancellationToken cancellationToken)
    {
        var response = await _examService.GetExamByIdAsync(id, cancellationToken);
        return Ok(response);
    }
}
