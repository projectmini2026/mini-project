using InternalExamScrutinySystem.Api.Contracts;
using InternalExamScrutinySystem.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using InternalExamScrutinySystem.Api.Helpers;

namespace InternalExamScrutinySystem.Api.Services;

public interface IModuleCoordinatorService
{
    Task<ApiResponse<List<ModuleDto>>> GetMyModulesAsync(int userId, CancellationToken cancellationToken);
    Task<ApiResponse<List<QuestionPaperDto>>> GetPapersByModuleAsync(int userId, int moduleId, CancellationToken cancellationToken);
    Task<ApiResponse<object>> AssignScrutinizerAsync(int userId, int paperId, int scrutinizerUserId, CancellationToken cancellationToken);
    Task<ApiResponse<object>> ApproveReportAsync(int userId, int paperId, CancellationToken cancellationToken);
    Task<ApiResponse<object>> FinalizeReportAsync(int userId, int paperId, CancellationToken cancellationToken);
    Task<ApiResponse<object>> RequestCorrectionAsync(int userId, int paperId, CancellationToken cancellationToken);
    Task<ApiResponse<object>> AssignFacultyToSubjectAsync(int userId, AssignFacultyToSubjectRequest request, CancellationToken cancellationToken);
    
    // New methods for Module User Dashboard
    Task<ApiResponse<List<FacultyDto>>> GetFacultyListAsync(CancellationToken cancellationToken);
    Task<ApiResponse<object>> AssignScrutinizerToModuleAsync(int userId, AssignScrutinizerToModuleRequest request, CancellationToken cancellationToken);
}

public class ModuleCoordinatorService : IModuleCoordinatorService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;

    public ModuleCoordinatorService(AppDbContext db, INotificationService notificationService, IEmailService emailService)
    {
        _db = db;
        _notificationService = notificationService;
        _emailService = emailService;
    }

    public async Task<ApiResponse<List<ModuleDto>>> GetMyModulesAsync(int userId, CancellationToken cancellationToken)
    {
        var modules = await _db.Modules
            .Where(m => m.CoordinatorId == userId)
            .Include(m => m.Subjects)
            .ToListAsync(cancellationToken);

        var result = new List<ModuleDto>();

        foreach (var module in modules)
        {
            var facultyIds = await _db.FacultyAssignments
                .Where(a => a.ModuleId == module.Id)
                .Select(a => a.FacultyId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var faculties = await _db.Users
                .Where(u => facultyIds.Contains(u.Id))
                .Select(u => new FacultyDto { 
                    userId = u.Id, 
                    name = (u.Position.ToShortForm() != null ? u.Position.ToShortForm() + " " : "") + u.Name, 
                    email = u.Email 
                })
                .ToListAsync(cancellationToken);

            var scrutinizerAssignment = await _db.ScrutinizerAssignments
                .Include(sa => sa.Faculty)
                .FirstOrDefaultAsync(sa => sa.ModuleId == module.Id, cancellationToken);

            result.Add(new ModuleDto
            {
                id = module.Id,
                moduleName = module.ModuleName,
                semester = module.Semester,
                scrutinizerName = scrutinizerAssignment?.Faculty != null 
                    ? (scrutinizerAssignment.Faculty.Position.ToShortForm() != null ? scrutinizerAssignment.Faculty.Position.ToShortForm() + " " : "") + scrutinizerAssignment.Faculty.Name 
                    : null,
                subjects = module.Subjects.Select(s => new ModuleSubjectDto
                {
                    subjectCode = s.SubjectCode,
                    subjectName = s.SubjectName
                }).ToList(),
                faculties = faculties
            });
        }

        return new ApiResponse<List<ModuleDto>> { success = true, message = "Modules fetched.", data = result };
    }

    public async Task<ApiResponse<List<FacultyDto>>> GetFacultyListAsync(CancellationToken cancellationToken)
    {
        var faculties = await _db.Users
            .Where(u => u.RoleId != Role.HOD) // Everyone else is a potential faculty/scrutinizer
            .Select(u => new FacultyDto { 
                userId = u.Id, 
                name = (u.Position.ToShortForm() != null ? u.Position.ToShortForm() + " " : "") + u.Name, 
                email = u.Email 
            })
            .ToListAsync(cancellationToken);

        return new ApiResponse<List<FacultyDto>> { success = true, message = "Faculty list fetched.", data = faculties };
    }

    public async Task<ApiResponse<object>> AssignScrutinizerToModuleAsync(int userId, AssignScrutinizerToModuleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Verify module ownership
            var module = await _db.Modules.FirstOrDefaultAsync(m => m.Id == request.moduleId && m.CoordinatorId == userId, cancellationToken);
            if (module == null) return new ApiResponse<object> { success = false, message = "Module not found or not authorized.", data = null };

            var existing = await _db.ScrutinizerAssignments.FirstOrDefaultAsync(sa => sa.ModuleId == request.moduleId, cancellationToken);
            if (existing != null)
            {
                existing.FacultyId = request.facultyId;
                existing.Remarks = request.remarks;
                existing.AssignedDate = DateTime.UtcNow;
            }
            else
            {
                _db.ScrutinizerAssignments.Add(new ScrutinizerAssignment
                {
                    ModuleId = request.moduleId,
                    FacultyId = request.facultyId,
                    Remarks = request.remarks,
                    AssignedDate = DateTime.UtcNow
                });
            }

            // Proactively assign this scrutinizer to any papers in this module that are still in 'Submitted' status
            var pendingPapers = await _db.QuestionPapers
                .Where(p => p.ModuleId == request.moduleId && p.Status == WorkflowStatus.Submitted && p.ScrutinizerUserId == null)
                .ToListAsync(cancellationToken);

            foreach (var paper in pendingPapers)
            {
                paper.ScrutinizerUserId = request.facultyId;
                paper.Status = WorkflowStatus.UnderScrutiny;
                
                // Add to ScrutinyAssignments for tracking
                _db.ScrutinyAssignments.Add(new ScrutinyAssignment
                {
                    QuestionPaperId = paper.Id,
                    ScrutinizerUserId = request.facultyId
                });
            }

            await _db.SaveChangesAsync(cancellationToken);

            // Notify Scrutinizer
            try
            {
                await _notificationService.SendNotificationToUsersAsync(
                    new List<int> { request.facultyId },
                    $"You have been appointed as the Scrutinizer for the module: {module.ModuleName}.",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NOTIFICATION ERROR] MC Module Scrutinizer: {ex.Message}");
            }

            return new ApiResponse<object> { success = true, message = "Scrutinizer assigned successfully.", data = null };
        }
        catch (Exception ex)
        {
            // Bubble up the actual error for debugging
            return new ApiResponse<object> { success = false, message = $"Assignment failed: {ex.Message} {ex.InnerException?.Message}", data = null };
        }
    }

    public async Task<ApiResponse<List<QuestionPaperDto>>> GetPapersByModuleAsync(int userId, int moduleId, CancellationToken cancellationToken)
    {
        // 1. Get the module and verify ownership
        var module = await _db.Modules.FirstOrDefaultAsync(m => m.Id == moduleId, cancellationToken);
            
        if (module == null || module.CoordinatorId != userId)
        {
            return new ApiResponse<List<QuestionPaperDto>> { success = false, message = "Module not found or not coordinated by you.", data = [] };
        }

        // 2. Fetch all uploaded papers for this module that are part of ANY active exam cycle
        var rawUploaded = await _db.QuestionPapers
            .Include(p => p.Exam)
            .Include(p => p.Subject)
            .Include(p => p.Scrutinizer)
            .Where(p => p.ModuleId == moduleId && p.Exam != null)
            .OrderByDescending(p => p.SubmittedDateUtc)
            .ToListAsync(cancellationToken);
        var uploadedPapers = new List<QuestionPaperDto>();
        foreach (var p in rawUploaded)
        {
            // Fetch faculty name separately to ensure clean translation
            var facultyUser = await _db.Users
                .FirstOrDefaultAsync(u => u.Id == p.SubmittedByFacultyUserId, cancellationToken);
            var facultyName = facultyUser != null 
                ? (facultyUser.Position.ToShortForm() != null ? facultyUser.Position.ToShortForm() + " " : "") + facultyUser.Name 
                : "Unknown";

            // Fetch latest report separately
            var reportJson = await _db.ScrutinyReports
                .Where(r => r.QuestionPaperId == p.Id)
                .OrderByDescending(r => r.SubmittedAtUtc)
                .Select(r => r.ReportJson)
                .FirstOrDefaultAsync(cancellationToken);

            uploadedPapers.Add(new QuestionPaperDto
            {
                id = p.Id,
                moduleId = p.ModuleId,
                moduleName = module.ModuleName,
                subjectId = p.SubjectId ?? 0,
                subjectCode = p.SubjectCode,
                subjectName = p.Subject?.SubjectName ?? "Unknown Subject",
                submittedByFacultyUserId = p.SubmittedByFacultyUserId,
                submittedByFacultyName = facultyName,
                fileUrl = p.FileUrl,
                submittedDateUtc = p.SubmittedDateUtc,
                status = p.Status.ToString(),
                scrutinizerUserId = p.ScrutinizerUserId,
                scrutinizerName = p.Scrutinizer != null 
                    ? (p.Scrutinizer.Position.ToShortForm() != null ? p.Scrutinizer.Position.ToShortForm() + " " : "") + p.Scrutinizer.Name 
                    : null,
                reportJson = reportJson,
                semester = !string.IsNullOrWhiteSpace(p.Semester) ? p.Semester : module.Semester,
                series = p.Series,
                examName = p.Exam?.ExamName ?? "N/A",
                lastDateToUpload = p.Exam?.LastDateToUpload,
                academicYear = p.Exam?.AcademicYear,
                examId = p.ExamId,
                uploaded = !string.IsNullOrWhiteSpace(p.FileUrl),
                version = p.Version
            });
        }

        // 3. Identify subjects that are part of ACTIVE exams but NOT YET UPLOADED
        var activeExams = await _db.Exams.Where(e => e.IsActive).ToListAsync(cancellationToken);
        
        if (activeExams.Any())
        {
            var activeExamIds = activeExams.Select(e => e.Id).ToList();
            
            // Use the papers we already loaded to track what's explicitly uploaded per exam
            var uploadedSet = uploadedPapers
                .Select(p => (p.examId ?? 0) + "-" + p.subjectId)
                .ToHashSet();

            var expectedPapersRaw = await _db.ExamSubjects
                .Include(es => es.Subject)
                .Include(es => es.Exam)
                .Where(es => activeExamIds.Contains(es.ExamId) && es.Subject.ModuleId == moduleId)
                .ToListAsync(cancellationToken);

            foreach (var es in expectedPapersRaw)
            {
                var key = es.ExamId + "-" + es.SubjectId;
                if (uploadedSet.Contains(key)) continue;

                // Lookup assigned faculty
                var assignment = await _db.FacultySubjectAssignments
                    .Where(a => a.SubjectId == es.SubjectId && (a.ExamId == es.ExamId || a.ExamId == null))
                    .OrderByDescending(a => a.ExamId) // Specific exam takes priority
                    .FirstOrDefaultAsync(cancellationToken);

                string facultyName = "Not Assigned";
                int facultyId = 0;

                if (assignment != null)
                {
                    facultyId = assignment.FacultyId;
                    var facUser = await _db.Users
                        .FirstOrDefaultAsync(u => u.Id == facultyId, cancellationToken);
                    facultyName = facUser != null 
                        ? (facUser.Position.ToShortForm() != null ? facUser.Position.ToShortForm() + " " : "") + facUser.Name 
                        : "Not Assigned";
                }

                uploadedPapers.Add(new QuestionPaperDto
                {
                    id = 0,
                    moduleId = moduleId,
                    subjectId = es.SubjectId,
                    moduleName = module.ModuleName,
                    subjectCode = es.Subject?.SubjectCode ?? "N/A",
                    subjectName = es.Subject?.SubjectName ?? "N/A",
                    submittedByFacultyUserId = facultyId,
                    submittedByFacultyName = facultyName,
                    fileUrl = null,
                    submittedDateUtc = DateTime.MinValue,
                    status = WorkflowStatus.NotSubmitted.ToString(),
                    scrutinizerUserId = null,
                    scrutinizerName = null,
                    series = es.Exam?.ActiveSeries ?? "A",
                    semester = es.Semester ?? es.Subject?.Module?.Semester,
                    uploaded = false,
                    examName = es.Exam?.ExamName ?? "N/A",
                    examId = es.ExamId,
                    academicYear = es.Exam?.AcademicYear
                });
            }
        }


        return new ApiResponse<List<QuestionPaperDto>> { success = true, message = "Papers fetched.", data = uploadedPapers };
    }

    private async Task<bool> IsExamActiveForPaperAsync(int paperId, CancellationToken cancellationToken)
    {
        var paper = await _db.QuestionPapers
            .Include(p => p.Exam)
            .FirstOrDefaultAsync(p => p.Id == paperId, cancellationToken);
            
        if (paper == null || paper.Exam == null) return false;

        return paper.Exam.IsActive && paper.Exam.EndDate >= DateTime.UtcNow;
    }

    public async Task<ApiResponse<object>> AssignScrutinizerAsync(int userId, int paperId, int scrutinizerUserId, CancellationToken cancellationToken)
    {
        try
        {
            if (!await IsExamActiveForPaperAsync(paperId, cancellationToken))
                return new ApiResponse<object> { success = false, message = "Actions are blocked. The examination cycle for this subject has been stopped or not started.", data = null };

            var paper = await _db.QuestionPapers.FirstOrDefaultAsync(p => p.Id == paperId, cancellationToken);
            if (paper == null) return new ApiResponse<object> { success = false, message = "Paper not found.", data = null };

            // ENSURE EXAM ID IS SET (Recovery fallback)
            if (paper.ExamId == null)
            {
                var activeExam = await _db.Exams.OrderByDescending(e => e.Id).FirstOrDefaultAsync(e => e.IsActive, cancellationToken);
                if (activeExam != null) paper.ExamId = activeExam.Id;
            }

            var isAuthorized = await _db.Modules.AnyAsync(m => m.CoordinatorId == userId && (m.Id == paper.ModuleId || _db.ModuleSubjects.Any(ms => ms.ModuleId == m.Id && ms.Id == paper.SubjectId)), cancellationToken);

            if (!isAuthorized) return new ApiResponse<object> { success = false, message = "Not authorized to coordinate this subject/module.", data = null };

            if (paper.Status != WorkflowStatus.Submitted && paper.Status != WorkflowStatus.UnderScrutiny)
                return new ApiResponse<object> { success = false, message = $"Invalid workflow transition. Current status: {paper.Status}", data = null };

            if (scrutinizerUserId == userId)
                return new ApiResponse<object> { success = false, message = "Module Coordinator cannot be the scrutinizer for their own module.", data = null };

            if (scrutinizerUserId == paper.SubmittedByFacultyUserId)
                return new ApiResponse<object> { success = false, message = "Scrutinizer must be a different faculty.", data = null };

            // --- NOTIFICATION CLEANUP (for old scrutinizer) ---
            if (paper.ScrutinizerUserId != null)
            {
                try
                {
                    var oldScrutinizerId = paper.ScrutinizerUserId.Value;
                    var subject = await _db.ModuleSubjects
                        .FirstOrDefaultAsync(s => s.Id == paper.SubjectId, cancellationToken);

                    string subjectInfoForCleanup = subject != null ? $"{subject.SubjectName} ({subject.SubjectCode})" : "Unknown Subject";

                    string oldNotificationMsg = $"You have been assigned as a Scrutinizer for {subjectInfoForCleanup}.";

                    var oldNotifications = await _db.Notifications
                        .Where(n => n.UserId == oldScrutinizerId && n.Message == oldNotificationMsg)
                        .ToListAsync(cancellationToken);
                    
                    if (oldNotifications.Any())
                    {
                        _db.Notifications.RemoveRange(oldNotifications);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CLEANUP ERROR] Failed to find old notifications for paper {paperId}: {ex.Message}");
                }
            }

            paper.ScrutinizerUserId = scrutinizerUserId;
            paper.Status = WorkflowStatus.UnderScrutiny;

            var existingAssignment = await _db.ScrutinyAssignments.FirstOrDefaultAsync(a => a.QuestionPaperId == paperId, cancellationToken);
            if (existingAssignment != null)
            {
                existingAssignment.ScrutinizerUserId = scrutinizerUserId;
                existingAssignment.AssignedAtUtc = DateTime.UtcNow;
            }
            else
            {
                _db.ScrutinyAssignments.Add(new ScrutinyAssignment
                {
                    QuestionPaperId = paper.Id,
                    ScrutinizerUserId = scrutinizerUserId
                });
            }

            await _db.SaveChangesAsync(cancellationToken);

            // --- NOTIFICATION & EMAIL ---
            try
            {
                var scrutinizer = await _db.Users.FirstOrDefaultAsync(u => u.Id == scrutinizerUserId, cancellationToken);
                if (scrutinizer != null)
                {
                    var subject = await _db.ModuleSubjects
                        .FirstOrDefaultAsync(s => s.Id == paper.SubjectId, cancellationToken);
                    
                    string subjectInfo = subject != null ? $"{subject.SubjectName} ({subject.SubjectCode})" : "Unknown Subject";

                    string notificationMsg = $"You have been assigned as a Scrutinizer for {subjectInfo}.";

                    // 1. Dashboard Notification
                    await _notificationService.SendNotificationToUsersAsync(new List<int> { scrutinizerUserId }, notificationMsg, cancellationToken);

                    // 2. Email Notification
                    string emailBody = $@"
Hello {scrutinizer.Name},

You have been assigned as a Scrutinizer for the following question paper:
Subject: {subjectInfo}
Module ID: {paper.ModuleId}

Please log in to the Internal Exam Scrutiny System to review the paper.

Regards,
Exam System Automaton";

                    await _emailService.SendEmailAsync(scrutinizer.Email, "Scrutinizer Assignment Notification", emailBody);
                }
            }
            catch (Exception ex)
            {
                // Don't fail the primary assignment if notification fails, but log it
                Console.WriteLine($"[NOTIFICATION ERROR] Failed to notify scrutinizer {scrutinizerUserId}: {ex.Message}");
            }

            return new ApiResponse<object> { success = true, message = "Scrutinizer assigned and notified.", data = null };
        }
        catch (Exception ex)
        {
            return new ApiResponse<object> { success = false, message = $"Assignment failed: {ex.Message} {ex.InnerException?.Message}", data = null };
        }
    }

    public async Task<ApiResponse<object>> ApproveReportAsync(int userId, int paperId, CancellationToken cancellationToken)
    {
        if (!await IsExamActiveForPaperAsync(paperId, cancellationToken))
            return new ApiResponse<object> { success = false, message = "Actions are blocked. The examination cycle for this subject has been stopped.", data = null };

        var paper = await _db.QuestionPapers.FirstOrDefaultAsync(p => p.Id == paperId, cancellationToken);
        if (paper == null) return new ApiResponse<object> { success = false, message = "Paper not found.", data = null };

        var module = await _db.Modules.FirstOrDefaultAsync(m => m.Id == paper.ModuleId && m.CoordinatorId == userId, cancellationToken);
        if (module == null) return new ApiResponse<object> { success = false, message = "Not authorized for this module.", data = null };

        if (paper.Status != WorkflowStatus.AwaitingMCApproval && paper.Status != WorkflowStatus.CorrectedSubmitted)
            return new ApiResponse<object> { success = false, message = "Only AwaitingMCApproval or CorrectedSubmitted papers can have their reports approved." };

        // --- SMART STATUS TRANSITION ---
        var latestReport = await _db.ScrutinyReports
            .Where(r => r.QuestionPaperId == paperId)
            .OrderByDescending(r => r.SubmittedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        bool hasRemarks = latestReport != null && DetectRemarks(latestReport.ReportJson);

        if (hasRemarks)
        {
            paper.Status = WorkflowStatus.CorrectionRequired;
        }
        else
        {
            paper.Status = WorkflowStatus.AwaitingECApproval;
        }

        // --- NOTIFICATION & AUDIT ---
        try
        {
            var subj = await _db.ModuleSubjects.FirstOrDefaultAsync(s => s.Id == paper.SubjectId, cancellationToken);
            string subjectName = subj?.SubjectName ?? "Unknown Subject";

            if (hasRemarks)
            {
                // Notify Faculty that correction is required
                await _notificationService.SendNotificationToUsersAsync(
                    new List<int> { paper.SubmittedByFacultyUserId },
                    $"Correction required for your question paper: {subjectName} (Report approved by MC). Please check remarks.",
                    cancellationToken);
            }
            else
            {
                paper.Status = WorkflowStatus.Finalized; // Auto-finalize clean papers

                // Notify Faculty that it's good
                await _notificationService.SendNotificationToUsersAsync(
                    new List<int> { paper.SubmittedByFacultyUserId },
                    $"Your question paper for {subjectName} has been Finalized & Approved by the Module Coordinator.",
                    cancellationToken);

                // Notify Exam Coordinators that it's ready for printing
                var ecIds = await _db.Users
                    .Where(u => u.RoleId == Role.ExamCoordinator)
                    .Select(u => u.Id)
                    .ToListAsync(cancellationToken);
                
                if (ecIds.Any())
                {
                    await _notificationService.SendNotificationToUsersAsync(
                        ecIds, 
                        $"Question paper for {subjectName} has been Finalized and is now ready for download.", 
                        cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NOTIFICATION ERROR] Failed to notify on report approval: {ex.Message}");
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new ApiResponse<object> { success = true, message = "Report approved.", data = null };
    }

    public async Task<ApiResponse<object>> FinalizeReportAsync(int userId, int paperId, CancellationToken cancellationToken)
    {
        if (!await IsExamActiveForPaperAsync(paperId, cancellationToken))
            return new ApiResponse<object> { success = false, message = "Actions are blocked. The examination cycle for this subject has been stopped.", data = null };

        var paper = await _db.QuestionPapers.FirstOrDefaultAsync(p => p.Id == paperId, cancellationToken);
        if (paper == null) return new ApiResponse<object> { success = false, message = "Paper not found.", data = null };

        var module = await _db.Modules.FirstOrDefaultAsync(m => m.Id == paper.ModuleId && m.CoordinatorId == userId, cancellationToken);
        if (module == null) return new ApiResponse<object> { success = false, message = "Not authorized for this module.", data = null };

        // Allow finalization from several stages where the coordinator has review power
        if (paper.Status != WorkflowStatus.AwaitingMCApproval && 
            paper.Status != WorkflowStatus.CorrectedSubmitted && 
            paper.Status != WorkflowStatus.AwaitingECApproval &&
            paper.Status != WorkflowStatus.UnderScrutiny)
        {
            return new ApiResponse<object> { success = false, message = $"Current status '{paper.Status}' cannot be finalized directly by Module Coordinator." };
        }

        paper.Status = WorkflowStatus.Finalized;

        // Notify Faculty
        try
        {
            var subj = await _db.ModuleSubjects.FirstOrDefaultAsync(s => s.Id == paper.SubjectId, cancellationToken);
            string subjectName = subj?.SubjectName ?? "Unknown Subject";
            
            await _notificationService.SendNotificationToUsersAsync(
                new List<int> { paper.SubmittedByFacultyUserId },
                $"Your question paper for {subjectName} has been Finalized & Approved by the Module Coordinator.",
                cancellationToken);

            // Notify Exam Coordinators that a paper is ready for download
            var ecIds = await _db.Users
                .Where(u => u.RoleId == Role.ExamCoordinator)
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);
            
            if (ecIds.Any())
            {
                await _notificationService.SendNotificationToUsersAsync(
                    ecIds, 
                    $"Question paper for {subjectName} has been Finalized and is now ready for download.", 
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NOTIFICATION ERROR] Failed to notify on report finalization: {ex.Message}");
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new ApiResponse<object> { success = true, message = "Paper finalized and approved successfully.", data = null };
    }

    public async Task<ApiResponse<object>> RequestCorrectionAsync(int userId, int paperId, CancellationToken cancellationToken)
    {
        if (!await IsExamActiveForPaperAsync(paperId, cancellationToken))
            return new ApiResponse<object> { success = false, message = "Actions are blocked. The examination cycle for this subject has been stopped.", data = null };

        var paper = await _db.QuestionPapers.FirstOrDefaultAsync(p => p.Id == paperId, cancellationToken);
        if (paper == null) return new ApiResponse<object> { success = false, message = "Paper not found.", data = null };

        var module = await _db.Modules.FirstOrDefaultAsync(m => m.Id == paper.ModuleId && m.CoordinatorId == userId, cancellationToken);
        if (module == null) return new ApiResponse<object> { success = false, message = "Not authorized for this module.", data = null };

        if (paper.Status != WorkflowStatus.AwaitingMCApproval)
            return new ApiResponse<object> { success = false, message = "Only AwaitingMCApproval papers can have their reports rejected." };

        paper.Status = WorkflowStatus.CorrectionRequired; // Send to faculty for correction
        
        // Notify Faculty
        try
        {
            var subj = await _db.ModuleSubjects.FirstOrDefaultAsync(s => s.Id == paper.SubjectId, cancellationToken);

            string subjectName = subj?.SubjectName ?? "Unknown Subject";

            await _notificationService.SendNotificationToUsersAsync(
                new List<int> { paper.SubmittedByFacultyUserId },
                $"Correction required for your question paper: {subjectName}. Please check the scrutiny report.", 

                cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NOTIFICATION ERROR] Failed to notify faculty on correction request: {ex.Message}");
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new ApiResponse<object> { success = true, message = "Correction requested.", data = null };
    }

    public async Task<ApiResponse<object>> AssignFacultyToSubjectAsync(int userId, AssignFacultyToSubjectRequest request, CancellationToken cancellationToken)
    {
        // 1. Verify that the userId is indeed the coordinator for the requested moduleId
        var module = await _db.Modules.Include(m => m.Subjects)
            .FirstOrDefaultAsync(m => m.Id == request.ModuleId && m.CoordinatorId == userId, cancellationToken);
        
        if (module == null)
            return new ApiResponse<object> { success = false, message = "Module not found or not authorized.", data = null };

        // 2. Verify subject exists in module
        var subject = module.Subjects.FirstOrDefault(s => s.SubjectName == request.SubjectName);
        if (subject == null)
            return new ApiResponse<object> { success = false, message = "Subject not found in this module.", data = null };

        // 3. Verify faculty exists
        var user = await _db.Users.FindAsync(new object[] { request.FacultyId }, cancellationToken);
        if (user == null)
            return new ApiResponse<object> { success = false, message = "Faculty not found.", data = null };

        // 4. Check if assignment already exists
        var existing = await _db.FacultySubjectAssignments.AnyAsync(
            a => a.ModuleId == request.ModuleId && 
                 a.SubjectId == subject.Id && 
                 a.FacultyId == request.FacultyId, 
            cancellationToken);

        if (existing)
            return new ApiResponse<object> { success = false, message = "Faculty already assigned to this subject.", data = null };

        // Get Active Exam for assignment binding
        var activeExam = await _db.Exams.OrderByDescending(e => e.Id).FirstOrDefaultAsync(e => e.IsActive, cancellationToken);

        // 5. Create assignment
        var assignment = new FacultySubjectAssignment
        {
            ModuleId = request.ModuleId,
            SubjectId = subject.Id,
            FacultyId = request.FacultyId,
            ExamId = activeExam?.Id,

            AssignedByUserId = userId,
            AssignedAtUtc = DateTime.UtcNow
        };

        _db.FacultySubjectAssignments.Add(assignment);
        await _db.SaveChangesAsync(cancellationToken);

        // Notify Faculty
        try
        {
            await _notificationService.SendNotificationToUsersAsync(
                new List<int> { request.FacultyId },
                $"You have been assigned to prepare the question paper for {request.SubjectName} for the current examination cycle.",
                cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NOTIFICATION ERROR] MC Subject Assignment: {ex.Message}");
        }

        return new ApiResponse<object> { success = true, message = "Faculty assigned to subject successfully.", data = null };
    }

    private bool DetectRemarks(string? reportJson)
    {
        if (string.IsNullOrWhiteSpace(reportJson)) return false;

        try
        {
            using var doc = JsonDocument.Parse(reportJson);
            var root = doc.RootElement;

            // 1. Check Question-wise remarks
            if (root.TryGetProperty("remarks", out var remarksArr) && remarksArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var r in remarksArr.EnumerateArray())
                {
                    if (r.TryGetProperty("remark", out var rem) && !string.IsNullOrWhiteSpace(rem.GetString()))
                    {
                        return true;
                    }
                }
            }

            // 2. Check Additional Remark
            if (root.TryGetProperty("additionalRemark", out var addRem) && !string.IsNullOrWhiteSpace(addRem.GetString()))
            {
                return true;
            }

            // 3. Check Header Remarks
            if (root.TryGetProperty("headerRemarks", out var headerRem) && headerRem.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in headerRem.EnumerateObject())
                {
                    if (!string.IsNullOrWhiteSpace(prop.Value.GetString()))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}

