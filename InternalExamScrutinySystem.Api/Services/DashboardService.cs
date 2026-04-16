using InternalExamScrutinySystem.Api.Contracts;
using InternalExamScrutinySystem.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace InternalExamScrutinySystem.Api.Services;

public interface IDashboardService
{
    Task<ApiResponse<object>> GetMyDashboardAsync(int userId, CancellationToken cancellationToken);
}

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResponse<object>> GetMyDashboardAsync(int userId, CancellationToken cancellationToken)
    {
        var moduleIds = await _db.Modules
            .Where(m => m.CoordinatorId == userId)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        if (moduleIds.Count == 0)
        {
            return new ApiResponse<object>
            {
                success = true,
                message = "No modules assigned to this coordinator.",
                data = new
                {
                    statusCounts = new Dictionary<WorkflowStatus, int>(),
                    pendingAssignCount = 0
                }
            };
        }

        var papers = await _db.QuestionPapers
            .Include(p => p.Exam)
            .Where(p => moduleIds.Contains(p.ModuleId) && p.Exam != null && p.Exam.IsActive)
            .ToListAsync(cancellationToken);

        var statusCounts = papers
            .GroupBy(p => p.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        var pendingAssignCount = papers.Count(p => p.Status == WorkflowStatus.Submitted && p.ScrutinizerUserId == null);
        var pendingApprovalCount = papers.Count(p => p.Status == WorkflowStatus.AwaitingMCApproval || p.Status == WorkflowStatus.CorrectedSubmitted);

        return new ApiResponse<object>
        {
            success = true,
            message = "Dashboard fetched.",
            data = new
            {
                statusCounts = statusCounts,
                pendingAssignCount = pendingAssignCount,
                pendingApprovalCount = pendingApprovalCount
            }
        };
    }
}

