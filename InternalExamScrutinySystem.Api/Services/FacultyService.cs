using System;
using System.Linq;

using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using InternalExamScrutinySystem.Api.Contracts;
using InternalExamScrutinySystem.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;


namespace InternalExamScrutinySystem.Api.Services;

public interface IFacultyService
{
    Task<ApiResponse<List<FacultySubjectAssignmentResponse>>> GetMyAssignmentsAsync(int userId, CancellationToken cancellationToken);
    Task<ApiResponse<object>> UploadQuestionPaperAsync(int userId, UploadQnPaperRequest request, CancellationToken cancellationToken);
    Task<ApiResponse<List<ScrutinyAssignmentResponse>>> GetScrutinyAssignmentsAsync(int userId, CancellationToken cancellationToken);
    Task<ApiResponse<object>> SubmitScrutinyReportAsync(int userId, SubmitScrutinyReportRequest request, CancellationToken cancellationToken);
}

public class FacultyService : IFacultyService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly INotificationService _notificationService;

    public FacultyService(AppDbContext db, IWebHostEnvironment env, IConfiguration config, INotificationService notificationService)
    {
        _db = db;
        _env = env;
        _config = config;
        _notificationService = notificationService;
    }

    public async Task<ApiResponse<List<FacultySubjectAssignmentResponse>>> GetMyAssignmentsAsync(int userId, CancellationToken cancellationToken)
    {
        var activeExam = await _db.Exams
            .OrderByDescending(e => e.Id)
            .FirstOrDefaultAsync(e => e.IsActive, cancellationToken);
        
        var activeExamId = activeExam?.Id;
        string activeSeriesName = activeExam?.ActiveSeries ?? "Series 1";

        var assignments = await _db.FacultySubjectAssignments
            .Include(a => a.Subject)
            .Include(a => a.Module)
            .Include(a => a.Exam)
            .Where(a => a.FacultyId == userId && (a.ExamId == null || a.ExamId == activeExamId || a.Exam!.IsActive))
            .ToListAsync(cancellationToken);

        var result = new List<FacultySubjectAssignmentResponse>();

        foreach (var a in assignments)
        {
            // Lookup exam-specific semester if available
            int? currentExamId = a.ExamId ?? activeExamId;
            string? examSemester = null;
            if (currentExamId != null)
            {
                examSemester = (await _db.ExamSubjects
                    .Where(es => es.ExamId == currentExamId && es.SubjectId == a.SubjectId)
                    .Select(es => es.Semester)
                    .ToListAsync(cancellationToken))
                    .FirstOrDefault();
            }

            string? finalizedSemester = !string.IsNullOrWhiteSpace(a.Semester) ? a.Semester : 
                                      (!string.IsNullOrWhiteSpace(examSemester) ? examSemester : a.Module?.Semester);

            // Robust lookup: Get papers for this subject/module in the CURRENT ACTIVE EXAM to support multiple series
            var papers = await _db.QuestionPapers
                .Where(p => p.ModuleId == a.ModuleId && p.SubjectId == a.SubjectId && (currentExamId == null || p.ExamId == currentExamId))
                .ToListAsync(cancellationToken);

            // Fetch ALL relevant scrutiny reports in one go to avoid N+1 and build errors
            var paperIds = papers.Select(paper => paper.Id).ToList();
            var allReports = await _db.ScrutinyReports
                .Where(r => paperIds.Contains(r.QuestionPaperId))
                .OrderByDescending(r => r.SubmittedAtUtc)
                .ToListAsync(cancellationToken);

            // Group existing papers by series
            var seriesStatus = papers
                .GroupBy(paperGroup => paperGroup.Series)
                .Select(g => {
                    var latestPaper = g.OrderByDescending(x => x.SubmittedDateUtc).First();
                    var report = allReports.FirstOrDefault(r => r.QuestionPaperId == latestPaper.Id)?.ReportJson;

                    return new SeriesStatusDto
                    {
                        Id = latestPaper.Id,
                        Series = latestPaper.Series,
                        Status = latestPaper.Status.ToString(),
                        SubmittedDate = latestPaper.SubmittedDateUtc,
                        FileUrl = latestPaper.FileUrl,
                        ReportJson = report,
                        version = latestPaper.Version
                    };
                })
                .ToList();

            // Ensure the CURRENT ACTIVE SERIES is always present in the list
            if (!seriesStatus.Any(s => s.Series == activeSeriesName))
            {
                seriesStatus.Add(new SeriesStatusDto
                {
                    Series = activeSeriesName,
                    Status = "NotSubmitted"
                });
            }

            // Determine an overall 'status' for the assignment (best-effort summary)
            var overallStatus = seriesStatus.OrderByDescending(s => s.SubmittedDate).FirstOrDefault()?.Status ?? "NotSubmitted";

            result.Add(new FacultySubjectAssignmentResponse
            {
                ModuleId = a.ModuleId,
                ModuleName = a.Module?.ModuleName ?? "Unknown",
                Semester = finalizedSemester,
                SubjectId = a.SubjectId,
                SubjectCode = a.Subject?.SubjectCode ?? "Unknown",
                SubjectName = a.Subject?.SubjectName ?? "Unknown",
                Status = overallStatus,
                AcademicYear = a.Exam?.AcademicYear ?? activeExam?.AcademicYear,
                ExamId = a.ExamId ?? activeExamId,
                ExamName = a.Exam?.ExamName ?? activeExam?.ExamName,
                SeriesStatus = seriesStatus.OrderBy(s => s.Series).ToList()
            });
        }

        return new ApiResponse<List<FacultySubjectAssignmentResponse>> 
        { 
            success = true, 
            message = "Assignments retrieved successfully", 
            data = result 
        };
    }


    public async Task<ApiResponse<object>> UploadQuestionPaperAsync(int userId, UploadQnPaperRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Get User Info for role-based logic
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null) return new ApiResponse<object> { success = false, message = "User not found." };

            bool isAdminRole = user.RoleId == Role.ExamCoordinator || 
                              user.RoleId == Role.ModuleCoordinator || 
                              user.RoleId == Role.HOD;

            // 2. Verify Assignment (Only for Faculty role; Admins/Coordinators can override)
            FacultySubjectAssignment? assignment = null;
            if (user.RoleId == Role.Faculty)
            {
                var assignments = await _db.FacultySubjectAssignments
                    .Include(a => a.Subject)
                    .Include(a => a.Module)
                    .Where(a => a.FacultyId == userId && a.ModuleId == request.ModuleId)
                    .ToListAsync(cancellationToken);

                assignment = assignments.FirstOrDefault(a => 
                    a.Subject!.SubjectCode.Equals(request.SubjectCode, StringComparison.OrdinalIgnoreCase) && 
                    (string.IsNullOrEmpty(request.Semester) || 
                     (a.Semester != null && a.Semester.Equals(request.Semester, StringComparison.OrdinalIgnoreCase)) ||
                     (a.Module != null && a.Module.Semester != null && a.Module.Semester.Equals(request.Semester, StringComparison.OrdinalIgnoreCase))));

                if (assignment == null)
                    return new ApiResponse<object> { success = false, message = $"Upload failed: You (Faculty #{userId}) are not assigned to {request.SubjectCode} in {request.Semester ?? "any"} semester for Module {request.ModuleId}." };
            }

            // 3. Verify against active exam cycle, dates, and academic year
            var activeExam = await _db.Exams.OrderByDescending(e => e.Id).FirstOrDefaultAsync(e => e.IsActive, cancellationToken);
            
            if (activeExam == null)
                return new ApiResponse<object> { success = false, message = "Upload blocked: No active examination cycle found." };

            if (DateTime.UtcNow > activeExam.LastDateToUpload)
                return new ApiResponse<object> { success = false, message = $"Upload blocked: The submission deadline for this cycle has passed. Deadline: {activeExam.LastDateToUpload:yyyy-MM-dd}" };

            if (DateTime.UtcNow > activeExam.EndDate)
                return new ApiResponse<object> { success = false, message = "Upload blocked: This examination cycle has ended." };

            if (!string.IsNullOrEmpty(request.AcademicYear) && request.AcademicYear != activeExam.AcademicYear)
                return new ApiResponse<object> { success = false, message = $"Upload blocked: Academic year mismatch. Requested: {request.AcademicYear}, Active Cycle: {activeExam.AcademicYear}" };

            string activeSeriesName = activeExam.ActiveSeries ?? "Series 1";
            if (request.Series != activeSeriesName && !request.IsCorrection)
                return new ApiResponse<object> { success = false, message = $"Upload blocked: This examination cycle is currently accepting submissions for {activeSeriesName}, not {request.Series}." };

            // --- EXTRA SAFETY: Resolve SubjectId if zero from SubjectCode + ModuleId ---
            if (request.SubjectId == 0 && !string.IsNullOrWhiteSpace(request.SubjectCode))
            {
                var resolvedId = await _db.ModuleSubjects
                    .Where(s => s.ModuleId == request.ModuleId && s.SubjectCode.ToLower() == request.SubjectCode.ToLower())
                    .Select(s => (int?)s.Id)
                    .FirstOrDefaultAsync(cancellationToken);
                
                if (resolvedId > 0) request.SubjectId = resolvedId.Value;
            }

            if (request.SubjectId == 0)
                return new ApiResponse<object> { success = false, message = "Upload failed: SubjectId is missing or invalid." };

            // 4. Check for existing submission for this specific series or paper in this Exam Cycle
            var existingPaper = await _db.QuestionPapers
                .FirstOrDefaultAsync(p => 
                    p.ModuleId == request.ModuleId && 
                    p.SubjectId == request.SubjectId && 
                    p.Series == request.Series &&
                    (p.ExamId == activeExam.Id || p.ExamId == null), cancellationToken);

            // Admins can update initial submissions. Correction uploads are strictly limited to ONLY ONCE.
            bool isCorrectionProcess = request.IsCorrection || (existingPaper != null && existingPaper.Status == WorkflowStatus.CorrectionRequired);
            
            bool canUpdate = existingPaper != null && (
                                (existingPaper.Status == WorkflowStatus.CorrectionRequired) || // Correction path
                                (isAdminRole && existingPaper.Status != WorkflowStatus.CorrectedSubmitted && existingPaper.Status != WorkflowStatus.Approved && existingPaper.Status != WorkflowStatus.Finalized) || // Admin override for initial uploads
                                (userId == existingPaper.SubmittedByFacultyUserId && (existingPaper.Status == WorkflowStatus.Submitted || existingPaper.Status == WorkflowStatus.AwaitingMCApproval)) // Owner fix before finalization
                             );

            // UNIVERSAL LOCK: If a correction has already been submitted, block further uploads for everyone (unless manual DB intervention)
            if (existingPaper != null && (existingPaper.Status == WorkflowStatus.CorrectedSubmitted || existingPaper.Status == WorkflowStatus.Approved))
            {
                return new ApiResponse<object> { 
                    success = false, 
                    message = $"Correction already submitted for {request.Series}. This process allows only one correction upload per paper." 
                };
            }

            if (existingPaper != null && !canUpdate)
            {
                string uploaderInfo = existingPaper.SubmittedByFacultyUserId == userId ? "You have" : "Another faculty has";
                string currentStatusMsg = existingPaper.Status == WorkflowStatus.Finalized ? "The paper is finalized for printing." : $"Current Status: {existingPaper.Status}";
                return new ApiResponse<object> { 
                    success = false, 
                    message = $"{uploaderInfo} already uploaded a question paper for {request.Series}. {currentStatusMsg}" 
                };
            }

            // 5. Validate File
            if (request.File == null || request.File.Length == 0)
                return new ApiResponse<object> { success = false, message = "No file uploaded." };

            if (Path.GetExtension(request.File.FileName).ToLower() != ".pdf")
                return new ApiResponse<object> { success = false, message = "Only PDF files are accepted." };

            if (request.File.Length > 10 * 1024 * 1024) // 10MB
                return new ApiResponse<object> { success = false, message = "File size exceeds the 10MB limit." };

            // 6. Save File
            string webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string uploadsFolder = Path.Combine(webRoot, "Uploads", "QuestionPapers");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string fileName = $"{userId}_{request.ModuleId}_{request.SubjectId}_{request.Series}_{DateTime.UtcNow.Ticks}.pdf";

            string filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.File.CopyToAsync(stream, cancellationToken);
            }

            // 7. Create or Update Record
            if (existingPaper != null)
            {
                // Trusted Correction Detection: Trust the frontend flag OR the current workflow state
                bool isCorrectionUpload = request.IsCorrection ||
                                         existingPaper.Status == WorkflowStatus.CorrectionRequired || 
                                         existingPaper.Status == WorkflowStatus.AwaitingECApproval ||
                                         existingPaper.Status == WorkflowStatus.UnderScrutiny ||
                                         existingPaper.Status == WorkflowStatus.AwaitingMCApproval ||
                                         existingPaper.Status == WorkflowStatus.CorrectedSubmitted;
                
                existingPaper.FileUrl = $"/Uploads/QuestionPapers/{fileName}";
                existingPaper.SubmittedDateUtc = DateTime.UtcNow;
                
                // ENSURE EXAM ID IS SET (Recovery for updates)
                if (existingPaper.ExamId != activeExam.Id) existingPaper.ExamId = activeExam.Id;
                
                // Set status based on whether it's a correction requested by scrutinizer/MC/Admin
                if (isCorrectionUpload) 
                {
                    // Update: Sending these to CorrectedSubmitted so EC can finalize/download.
                    existingPaper.Status = WorkflowStatus.CorrectedSubmitted;
                }
                else 
                {
                    existingPaper.Status = WorkflowStatus.Submitted;
                }
                
                existingPaper.Version += 1;

                // Notify relevant roles on correction
                if (isCorrectionUpload)
                {
                    try
                    {
                        var ecIds = await _db.Users
                            .Where(u => u.RoleId == Role.ExamCoordinator)
                            .Select(u => u.Id)
                            .ToListAsync(cancellationToken);
                        
                        var subj = await _db.ModuleSubjects.FirstOrDefaultAsync(s => s.ModuleId == request.ModuleId && s.SubjectCode == request.SubjectCode, cancellationToken);
                        string subjectName = subj?.SubjectName ?? request.SubjectCode;
                        
                        string prefix = user.RoleId == Role.ExamCoordinator ? "Exam Coordinator updated" : "Faculty corrected";
                        string ecMessage = $"{prefix} Question Paper for {subjectName} ({request.SubjectCode})";
                        await _notificationService.SendNotificationToUsersAsync(ecIds, ecMessage, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[NOTIFICATION ERROR] Failed to notify on correction upload: {ex.Message}");
                    }
                }
            }
            else
            {
                // Auto-assign module-level scrutinizer if one exists
                var moduleScrutinizer = await _db.ScrutinizerAssignments
                    .FirstOrDefaultAsync(sa => sa.ModuleId == request.ModuleId, cancellationToken);
                
                var paper = new QuestionPaper
                {
                    ExamId = activeExam.Id,
                    ModuleId = request.ModuleId,
                    SubjectId = request.SubjectId,
                    SubjectCode = request.SubjectCode,
                    SubmittedByFacultyUserId = userId,
                    FileUrl = $"/Uploads/QuestionPapers/{fileName}",
                    SubmittedDateUtc = DateTime.UtcNow,
                    Status = moduleScrutinizer != null ? WorkflowStatus.UnderScrutiny : WorkflowStatus.Submitted,
                    Semester = request.Semester,
                    Series = request.Series,
                    ScrutinizerUserId = moduleScrutinizer?.FacultyId,
                    Version = 1
                };
                _db.QuestionPapers.Add(paper);

                if (moduleScrutinizer != null)
                {
                    _db.ScrutinyAssignments.Add(new ScrutinyAssignment
                    {
                        QuestionPaper = paper,
                        ScrutinizerUserId = moduleScrutinizer.FacultyId
                    });
                }
            }
            
            // 8. Notify Module Coordinator (if needed)
            int? coordinatorId = null;
            if (assignment != null) 
            {
                coordinatorId = assignment.Module?.CoordinatorId;
            }
            else
            {
                // If no assignment record (Admin upload), look up the module coordinator directly
                var module = await _db.Modules.FirstOrDefaultAsync(m => m.Id == request.ModuleId, cancellationToken);
                coordinatorId = module?.CoordinatorId;
            }

            if (coordinatorId > 0)
            {
                var subj = await _db.ModuleSubjects.FirstOrDefaultAsync(s => s.ModuleId == request.ModuleId && s.SubjectCode == request.SubjectCode, cancellationToken);
                string subjectName = subj?.SubjectName ?? request.SubjectCode;
                bool isCorrection = existingPaper != null;
                
                string prefix = isCorrection ? "Corrected" : "New";
                string roleName = user.RoleId.ToString();
                string message = $"{prefix} Question Paper uploaded for {subjectName} ({request.SubjectCode}) by {user.Name} ({roleName})";
                
                _db.Notifications.Add(new Notification
                {
                    UserId = coordinatorId.Value,
                    Message = message,
                    IsRead = false,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException dbEx)
            {
                var innerMsg = dbEx.InnerException?.Message ?? dbEx.Message;
                // LOG the detailed error to console if possible, or return it clearly
                Console.WriteLine($"[DB SAVE ERROR] {innerMsg}");
                return new ApiResponse<object> { 
                    success = false, 
                    message = $"Database Error: {innerMsg}" 
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object> { 
                    success = false, 
                    message = $"Unexpected Error: {ex.Message}" 
                };
            }

            string successMsg = existingPaper != null 
                ? "Corrected question paper submitted and uploaded successfully!" 
                : "Question paper uploaded successfully!";

            return new ApiResponse<object> { success = true, message = successMsg };
        }
        catch (Exception ex)
        {
            string detail = ex.InnerException != null ? $": {ex.InnerException.Message}" : "";
            return new ApiResponse<object> { success = false, message = $"Upload failed: {ex.Message}{detail}" };
        }
    }


    public async Task<ApiResponse<List<ScrutinyAssignmentResponse>>> GetScrutinyAssignmentsAsync(int userId, CancellationToken cancellationToken)
    {
        var activeExam = await _db.Exams.FirstOrDefaultAsync(e => e.IsActive, cancellationToken);

        // Get modules where this user is the assigned scrutinizer
        var scrutinyModuleIds = await _db.ScrutinizerAssignments
            .Where(sa => sa.FacultyId == userId)
            .Select(sa => sa.ModuleId)
            .ToListAsync(cancellationToken);

        // Fetch papers where user is explicitly assigned OR is the module-level scrutinizer
        var assignments = await _db.QuestionPapers
            .Include(p => p.Module)
            .Include(p => p.Exam)
            .Where(p => (p.ScrutinizerUserId == userId || scrutinyModuleIds.Contains(p.ModuleId)) &&
                        (p.Status == WorkflowStatus.Submitted || p.Status == WorkflowStatus.UnderScrutiny))
            .OrderByDescending(p => (activeExam != null && p.ExamId == activeExam.Id)) // Prioritize active exam
            .ThenByDescending(p => p.SubmittedDateUtc)
            .Select(p => new
            {
                p,
                ExamSemester = _db.ExamSubjects
                    .Where(es => es.ExamId == p.ExamId && es.SubjectId == p.SubjectId)
                    .Select(es => es.Semester)
                    .FirstOrDefault()

            })
            .Select(x => new ScrutinyAssignmentResponse
            {
                PaperId = x.p.Id,
                ModuleId = x.p.ModuleId,
                ModuleName = x.p.Module != null ? x.p.Module.ModuleName : "Unknown",
                SubjectId = x.p.SubjectId ?? 0,
                SubjectCode = x.p.Subject != null ? x.p.Subject.SubjectCode : "Unknown",
                SubmittedByFacultyName = null, // Anonymized for scrutiny
                FileUrl = x.p.FileUrl,
                Status = x.p.Status.ToString(),
                Semester = x.p.Semester ?? x.ExamSemester ?? (x.p.Module != null ? x.p.Module.Semester : null),
                ExamName = x.p.Exam != null ? x.p.Exam.ExamName : null,
                ExamId = x.p.ExamId,
                Series = x.p.Series,
                SubmittedDateUtc = x.p.SubmittedDateUtc
            })
            .ToListAsync(cancellationToken);


        // Fetch subject names from ModuleSubject table
        foreach (var a in assignments)
        {
            var subj = await _db.ModuleSubjects.FirstOrDefaultAsync(s => s.Id == a.SubjectId, cancellationToken);

            if (subj != null)
            {
                a.SubjectName = subj.SubjectName;
            }
        }

        return new ApiResponse<List<ScrutinyAssignmentResponse>> { success = true, data = assignments };
    }

    public async Task<ApiResponse<object>> SubmitScrutinyReportAsync(int userId, SubmitScrutinyReportRequest request, CancellationToken cancellationToken)
    {
        // Allow submission if the user is the PAPER's assigned scrutinizer OR the MODULE'S assigned scrutinizer
        var paper = await _db.QuestionPapers
            .Include(p => p.Module)
            .FirstOrDefaultAsync(p => p.Id == request.PaperId, cancellationToken);
            
        if (paper == null) return new ApiResponse<object> { success = false, message = "Paper not found." };

        bool isModuleScrutinizer = await _db.ScrutinizerAssignments
            .AnyAsync(sa => sa.ModuleId == paper.ModuleId && sa.FacultyId == userId, cancellationToken);

        if (paper.ScrutinizerUserId != userId && !isModuleScrutinizer)
            return new ApiResponse<object> { success = false, message = "You are not authorized to scrutinize this paper." };

        // Ensure the paper record is correctly tagged for this user if they are the module-level one
        if (paper.ScrutinizerUserId == null || paper.ScrutinizerUserId == 0)
        {
            paper.ScrutinizerUserId = userId;
        }

        if (paper.Status != WorkflowStatus.Submitted && paper.Status != WorkflowStatus.UnderScrutiny)
             return new ApiResponse<object> { success = false, message = "Invalid status for report submission." };

        var report = new ScrutinyReport
        {
            QuestionPaperId = request.PaperId,
            ScrutinizerUserId = userId,
            ReportJson = request.ReportJson,
            SubmittedAtUtc = DateTime.UtcNow
        };

        _db.ScrutinyReports.Add(report);
        paper.Status = WorkflowStatus.AwaitingMCApproval; // Transition to AwaitingMCApproval when report is submitted
        
        // Notify Module Coordinator
        if (paper.Module?.CoordinatorId != null)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = paper.Module.CoordinatorId.Value,
                Message = $"Scrutiny report submitted for {paper.Subject?.SubjectCode ?? "Unknown Subject"} in module {paper.Module?.ModuleName ?? "Unknown"}. Please review.",

                CreatedAtUtc = DateTime.UtcNow
            });
        }
        
        await _db.SaveChangesAsync(cancellationToken);

        return new ApiResponse<object> { success = true, message = "Scrutiny report submitted successfully." };
    }
}
