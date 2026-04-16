using InternalExamScrutinySystem.Api.Contracts;
using InternalExamScrutinySystem.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace InternalExamScrutinySystem.Api.Services;

public interface IExamCoordinatorService
{
    Task<ApiResponse<object>> CreateAssignmentAsync(CreateEcAssignmentRequest request, int ecUserId, CancellationToken cancellationToken);
    Task<ApiResponse<object>> UpdateAssignmentAsync(int id, UpdateEcAssignmentRequest request, int ecUserId, CancellationToken cancellationToken);
    Task<ApiResponse<object>> DeleteAssignmentAsync(int id, CancellationToken cancellationToken);
    Task<ApiResponse<List<EcAssignmentResponse>>> GetAssignmentsAsync(CancellationToken cancellationToken);
    
    Task<ApiResponse<object>> AssignModuleAsync(CreateModuleAssignmentRequest request, int ecUserId, CancellationToken cancellationToken);
    Task<ApiResponse<object>> DeleteModuleAssignmentAsync(int id, CancellationToken cancellationToken);
    Task<ApiResponse<List<ModuleAssignmentResponse>>> GetModuleAssignmentsAsync(CancellationToken cancellationToken);

    Task<ApiResponse<object>> GetModulesWithSubjectsAsync(CancellationToken cancellationToken);
    Task<ApiResponse<object>> GetFacultyRosterAsync(CancellationToken cancellationToken);
    Task<ApiResponse<object>> GetSubjectsBySemesterAsync(string semester, CancellationToken cancellationToken);
    Task<ApiResponse<object>> GetAllSubjectsAsync(CancellationToken cancellationToken);
    Task<ApiResponse<object>> ForceSeedAsync(CancellationToken cancellationToken);
    
    // New scrutiny workflow methods
    Task<ApiResponse<List<AwaitingApprovalPaperDto>>> GetAwaitingApprovalPapersAsync(CancellationToken cancellationToken);
    Task<ApiResponse<object>> ApproveFinalPaperAsync(int paperId, CancellationToken cancellationToken);
    Task<ApiResponse<object>> RequestCorrectionAsync(int paperId, CancellationToken cancellationToken);
}

public class ExamCoordinatorService : IExamCoordinatorService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;

    public ExamCoordinatorService(AppDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    public async Task<ApiResponse<object>> CreateAssignmentAsync(CreateEcAssignmentRequest request, int ecUserId, CancellationToken cancellationToken)
    {
        try
        {
            // Check for duplicates (must match Subject + Semester)
            var exists = await _db.FacultySubjectAssignments.AnyAsync(a => 
                a.SubjectId == request.SubjectId && 
                a.Semester == request.Semester, cancellationToken);
            
            if (exists) return new ApiResponse<object> { success = false, message = "This subject is already assigned to a faculty for this semester." };
            
            // NEW: Check for active exam cycle to enforce the lock
            var activeExam = await _db.Exams.OrderByDescending(e => e.Id).FirstOrDefaultAsync(e => e.IsActive, cancellationToken);
            if (activeExam != null) 
                return new ApiResponse<object> { success = false, message = $"Assignments are locked because the examination cycle '{activeExam.ExamName}' is currently active. Please close the exam before making changes." };

            var assignment = new FacultySubjectAssignment
            {
                FacultyId = request.FacultyId,
                ModuleId = request.ModuleId,
                SubjectId = request.SubjectId,
                Semester = request.Semester,
                ExamId = activeExam?.Id,
                AssignedByUserId = ecUserId,
                AssignedAtUtc = DateTime.UtcNow
            };

            _db.FacultySubjectAssignments.Add(assignment);
            await _db.SaveChangesAsync(cancellationToken);

            // Notify Faculty
            try
            {
                var subj = await _db.ModuleSubjects.FirstOrDefaultAsync(s => s.Id == request.SubjectId, cancellationToken);
                string subjectInfo = subj != null ? $"{subj.SubjectName} ({subj.SubjectCode})" : "Unknown Subject";
                
                await _notificationService.SendNotificationToUsersAsync(
                    new List<int> { request.FacultyId },
                    $"You have been assigned to prepare the question paper for {subjectInfo} for the current examination cycle.",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NOTIFICATION ERROR] EC Assignment: {ex.Message}");
            }

            return new ApiResponse<object> { success = true, message = "Assignment created successfully." };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] CreateAssignmentAsync: {ex.Message}");
            return new ApiResponse<object> { success = false, message = $"Failed to create assignment: {ex.InnerException?.Message ?? ex.Message}" };
        }
    }


    public async Task<ApiResponse<object>> UpdateAssignmentAsync(int id, UpdateEcAssignmentRequest request, int ecUserId, CancellationToken cancellationToken)
    {
        try
        {
            var assignment = await _db.FacultySubjectAssignments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
            if (assignment == null) return new ApiResponse<object> { success = false, message = "Assignment not found." };

            // Check for duplicates (excluding current record)
            var duplicate = await _db.FacultySubjectAssignments.AnyAsync(a => 
                a.Id != id &&
                a.FacultyId == request.FacultyId && 
                a.ModuleId == request.ModuleId && 
                a.SubjectId == request.SubjectId &&
                a.Semester == request.Semester, cancellationToken);

            if (duplicate) return new ApiResponse<object> { success = false, message = "This faculty is already assigned to this subject/semester in this module." };

            // NEW: Block if an exam cycle is already active
            var activeExam = await _db.Exams.FirstOrDefaultAsync(e => e.IsActive, cancellationToken);
            if (activeExam != null) 
                return new ApiResponse<object> { success = false, message = $"Modifications are locked while the examination cycle '{activeExam.ExamName}' is active." };

            assignment.FacultyId = request.FacultyId;
            assignment.ModuleId = request.ModuleId;
            assignment.SubjectId = request.SubjectId;
            assignment.Semester = request.Semester; // Update semester too
            assignment.AssignedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            return new ApiResponse<object> { success = true, message = "Assignment updated successfully." };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] UpdateAssignmentAsync: {ex.Message}");
            return new ApiResponse<object> { success = false, message = $"Failed to update assignment: {ex.InnerException?.Message ?? ex.Message}" };
        }
    }

    public async Task<ApiResponse<object>> DeleteAssignmentAsync(int id, CancellationToken cancellationToken)
    {
        var assignment = await _db.FacultySubjectAssignments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (assignment == null) return new ApiResponse<object> { success = false, message = "Assignment not found." };

        // NEW: Block if an exam cycle is already active
        var activeExam = await _db.Exams.FirstOrDefaultAsync(e => e.IsActive, cancellationToken);
        if (activeExam != null) 
            return new ApiResponse<object> { success = false, message = "Assignments cannot be deleted while an examination cycle is active." };

        _db.FacultySubjectAssignments.Remove(assignment);
        await _db.SaveChangesAsync(cancellationToken);
        return new ApiResponse<object> { success = true, message = "Assignment deleted." };
    }

    public async Task<ApiResponse<List<EcAssignmentResponse>>> GetAssignmentsAsync(CancellationToken cancellationToken)
    {
        // Get Active Exam for filtering
        var activeExam = await _db.Exams.OrderByDescending(e => e.Id).FirstOrDefaultAsync(e => e.IsActive, cancellationToken);
        var activeExamId = activeExam?.Id;

        // 1. Get assignments for the active exam cycle
        var assignments = await _db.FacultySubjectAssignments
            .Include(a => a.Faculty)
            .Include(a => a.Module)
            .Include(a => a.Subject)
            .ToListAsync(cancellationToken);

        var result = new List<EcAssignmentResponse>();

        foreach (var a in assignments)
        {
            var subjectCode = a.Subject?.SubjectCode ?? "";
            
        // Fetch papers for this subject in the CURRENT ACTIVE EXAM cycle
        var papers = await _db.QuestionPapers
            .Include(p => p.Exam)
            .Include(p => p.Subject)
            .Include(p => p.Scrutinizer)
            .Where(p => p.ModuleId == a.ModuleId 
                     && p.SubjectId == a.SubjectId
                     && (p.Semester == a.Semester || (string.IsNullOrEmpty(p.Semester) && string.IsNullOrEmpty(a.Semester))))
            .OrderByDescending(p => p.SubmittedDateUtc)
            .ToListAsync(cancellationToken);

            var latestPaper = papers.FirstOrDefault();

            // 1. Series Status Logic (Support for Series A, B, etc.)
            var seriesStatus = papers
                .GroupBy(p => p.Series)
                .Select(g => {
                    var latest = g.OrderByDescending(p => p.SubmittedDateUtc).First();
                    var v1 = g.FirstOrDefault(p => p.Version == 1) ?? g.OrderBy(p => p.SubmittedDateUtc).FirstOrDefault();
                    
                    // Finalized Date logic: Use the date it reached Finalized status
                    DateTime? finalizedDate = latest.Status == WorkflowStatus.Finalized ? latest.SubmittedDateUtc : (DateTime?)null;

                    return new SeriesStatusDto
                    {
                        Id = latest.Id,
                        Series = latest.Series,
                        Status = latest.Status.ToString(),
                        SubmittedDate = v1?.SubmittedDateUtc ?? latest.SubmittedDateUtc, // Original submission date
                        FileUrl = latest.FileUrl,
                        AcademicYear = latest.Exam?.AcademicYear,
                        ExamId = latest.ExamId,
                        version = latest.Version,

                        V1Status = latest.Status.ToString(), // Show latest general status
                        V1SubmittedDate = v1?.SubmittedDateUtc,
                        V2Status = null, // Removing V2 Status as per request
                        V2SubmittedDate = finalizedDate, // Using this slot for Finalized Date
                        ScrutinizerName = latest.Scrutinizer != null ? latest.Scrutinizer.Name : "-"
                    };
                })
                .ToList();

            // Scrutinizer Name logic
            string scrutinizerName = "-";
            if (latestPaper != null && latestPaper.ScrutinizerUserId != null)
            {
                var scrutinizer = await _db.Users.FirstOrDefaultAsync(u => u.Id == latestPaper.ScrutinizerUserId, cancellationToken);
                scrutinizerName = scrutinizer?.Name ?? "-";
            }
            else
            {
                var sa = await _db.ScrutinizerAssignments
                    .Include(s => s.Faculty)
                    .FirstOrDefaultAsync(s => s.ModuleId == a.ModuleId, cancellationToken);
                scrutinizerName = sa?.Faculty?.Name ?? "-";
            }

            result.Add(new EcAssignmentResponse
            {
                Id = a.Id,
                FacultyId = a.FacultyId,
                FacultyName = a.Faculty?.Name ?? "Unknown",
                FacultyDesignation = a.Faculty?.Position.ToString() ?? "Faculty",
                ModuleId = a.ModuleId,
                ModuleName = a.Module?.ModuleName ?? "Unknown",
                SubjectId = a.SubjectId,
                SubjectName = a.Subject?.SubjectName ?? "Unknown",
                SubjectCode = subjectCode,
                Status = latestPaper?.Status.ToString() ?? "NotSubmitted",
                SubmittedDate = latestPaper?.SubmittedDateUtc,
                ScrutinizerName = scrutinizerName,
                Semester = a.Semester,
                AssignedAtUtc = a.AssignedAtUtc,
                PaperId = latestPaper?.Id,
                FileUrl = latestPaper?.FileUrl,
                AcademicYear = latestPaper?.Exam?.AcademicYear,

                SeriesStatus = seriesStatus
            });
        }

        return new ApiResponse<List<EcAssignmentResponse>> { success = true, data = result };
    }

    public async Task<ApiResponse<object>> AssignModuleAsync(CreateModuleAssignmentRequest request, int ecUserId, CancellationToken cancellationToken)
    {
        foreach (var facultyId in request.FacultyIds)
        {
            var alreadyAssigned = await _db.FacultyAssignments.AnyAsync(a => a.ModuleId == request.ModuleId && a.FacultyId == facultyId, cancellationToken);
            if (!alreadyAssigned)
            {
                _db.FacultyAssignments.Add(new FacultyAssignment
                {
                    ModuleId = request.ModuleId,
                    FacultyId = facultyId,
                    AssignedDate = DateTime.UtcNow
                });
            }
        }
        await _db.SaveChangesAsync(cancellationToken);
        return new ApiResponse<object> { success = true, message = "Module assignments updated successfully." };
    }

    public async Task<ApiResponse<object>> DeleteModuleAssignmentAsync(int id, CancellationToken cancellationToken)
    {
        var assignment = await _db.FacultyAssignments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (assignment == null) return new ApiResponse<object> { success = false, message = "Assignment not found." };

        _db.FacultyAssignments.Remove(assignment);
        await _db.SaveChangesAsync(cancellationToken);
        return new ApiResponse<object> { success = true, message = "Assignment removed." };
    }

    public async Task<ApiResponse<List<ModuleAssignmentResponse>>> GetModuleAssignmentsAsync(CancellationToken cancellationToken)
    {
        var data = await _db.FacultyAssignments
            .Include(a => a.Faculty)
            .Include(a => a.Module)
            .Select(a => new ModuleAssignmentResponse
            {
                Id = a.Id,
                FacultyId = a.FacultyId,
                FacultyName = a.Faculty != null ? a.Faculty.Name : "Unknown",
                FacultyEmail = a.Faculty != null ? a.Faculty.Email : "Unknown",
                FacultyDesignation = a.Faculty != null ? a.Faculty.Position.ToString()! : "Faculty",
                ModuleId = a.ModuleId,
                ModuleName = a.Module != null ? a.Module.ModuleName : "Unknown",
                AssignedAtUtc = a.AssignedDate
            })
            .ToListAsync(cancellationToken);
        return new ApiResponse<List<ModuleAssignmentResponse>> { success = true, data = data };
    }

    public async Task<ApiResponse<object>> GetModulesWithSubjectsAsync(CancellationToken cancellationToken)
    {
        var modules = await _db.Modules
            .Include(m => m.Subjects)
            .Select(m => new {
                m.Id,
                m.ModuleName,
                m.Semester,
                Subjects = m.Subjects.Select(s => new { s.Id, s.SubjectCode, s.SubjectName })
            })
            .ToListAsync(cancellationToken);

        return new ApiResponse<object> { success = true, data = modules };
    }

    public async Task<ApiResponse<object>> GetFacultyRosterAsync(CancellationToken cancellationToken)
    {
        var faculties = await _db.Users
            .Where(u => u.RoleId != Role.HOD) // HOD doesn't get assigned usually, though allowed.
            .Select(u => new { u.Id, u.Name, u.Email, u.Position })
            .ToListAsync(cancellationToken);

        return new ApiResponse<object> { success = true, data = faculties };
    }

    public async Task<ApiResponse<object>> GetSubjectsBySemesterAsync(string semester, CancellationToken cancellationToken)
    {
        return await GetAllSubjectsAsync(cancellationToken); // Simplify for now
    }

    public async Task<ApiResponse<object>> GetAllSubjectsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var subjects = await _db.ModuleSubjects
                .AsNoTracking()
                .Include(s => s.Module)
                .ToListAsync(cancellationToken);

            var assignments = await _db.FacultySubjectAssignments
                .AsNoTracking()
                .Include(a => a.Faculty)
                .ToListAsync(cancellationToken);

            var assignmentsGrouped = assignments
                .GroupBy(a => a.SubjectId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var resultList = new List<object>();

            foreach (var s in subjects)
            {
                if (!assignmentsGrouped.TryGetValue(s.Id, out var subjectAssignments))
                {
                    // If no assignments, use the module's default semester
                    resultList.Add(new
                    {
                        subjectId = s.Id,
                        subjectCode = s.SubjectCode,
                        subjectName = s.SubjectName,
                        moduleId = s.ModuleId,
                        moduleName = s.Module?.ModuleName ?? "Unknown",
                        semester = s.Module?.Semester ?? "S1",
                        faculty = (object?)null
                    });
                }
                else
                {
                    // Return entries for each assigned semester to allow correct filtering
                    foreach (var a in subjectAssignments)
                    {
                        resultList.Add(new
                        {
                            subjectId = s.Id,
                            subjectCode = s.SubjectCode,
                            subjectName = s.SubjectName,
                            moduleId = s.ModuleId,
                            moduleName = s.Module?.ModuleName ?? "Unknown",
                            semester = a.Semester ?? s.Module?.Semester ?? "S1",
                            faculty = new
                            {
                                id = a.Faculty?.Id ?? 0,
                                name = a.Faculty?.Name ?? "Unknown",
                                email = a.Faculty?.Email ?? ""
                            }
                        });
                    }
                }
            }

            return new ApiResponse<object> { success = true, data = resultList };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] GetAllSubjectsAsync failed: {ex.Message}");
            return new ApiResponse<object> { success = false, message = "Internal database error: " + ex.Message };
        }
    }
    public async Task<ApiResponse<object>> ForceSeedAsync(CancellationToken cancellationToken)
    {
        try
        {
            var existingModule = await _db.Modules.FirstOrDefaultAsync(cancellationToken);
            if (existingModule == null)
            {
                existingModule = new Module 
                { 
                    ModuleCode = "M" + Guid.NewGuid().ToString().Substring(0, 4).ToUpper(),
                    ModuleName = "Default Seed Module"
                };
                _db.Modules.Add(existingModule);
                await _db.SaveChangesAsync(cancellationToken);
            }

            var sampleSubject = new ModuleSubject
            {
                SubjectCode = "S" + Guid.NewGuid().ToString().Substring(0, 4).ToUpper(),
                SubjectName = "Sample Subject (" + DateTime.Now.ToString("HH:mm:ss") + ")",
                ModuleId = existingModule.Id
            };

            _db.ModuleSubjects.Add(sampleSubject);
            await _db.SaveChangesAsync(cancellationToken);

            var totalCount = await _db.ModuleSubjects.CountAsync(cancellationToken);
            Console.WriteLine($"[SEED] ForceSeedAsync success. New Subject ID: {sampleSubject.Id}. Total Subjects in DB: {totalCount}");

            return new ApiResponse<object> { success = true, message = $"Seed success! Total subjects in DB: {totalCount}" };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] ForceSeedAsync failed: {ex.ToString()}");
            return new ApiResponse<object> { success = false, message = "Database Error: " + (ex.InnerException?.Message ?? ex.Message) };
        }
    }

    public async Task<ApiResponse<List<AwaitingApprovalPaperDto>>> GetAwaitingApprovalPapersAsync(CancellationToken cancellationToken)
    {
        var papers = await _db.QuestionPapers
            .Include(p => p.Module)
            .Include(p => p.Subject)
            .Where(p => p.Status == WorkflowStatus.CorrectedSubmitted || p.Status == WorkflowStatus.AwaitingECApproval || p.Status == WorkflowStatus.Submitted)
            .OrderByDescending(p => p.SubmittedDateUtc)

            .ToListAsync(cancellationToken);

        var result = new List<AwaitingApprovalPaperDto>();
        foreach (var p in papers)
        {
            var subj = p.Subject;

            var faculty = await _db.Users.FirstOrDefaultAsync(u => u.Id == p.SubmittedByFacultyUserId, cancellationToken);

            result.Add(new AwaitingApprovalPaperDto
            {
                Id = p.Id,
                ModuleId = p.ModuleId,
                ModuleName = p.Module?.ModuleName ?? "Unknown",
                SubjectCode = subj?.SubjectCode ?? "Unknown",

                SubjectName = subj?.SubjectName ?? "Unknown",
                FacultyName = faculty?.Name ?? "Unknown",
                FileUrl = p.FileUrl ?? "",
                SubmittedDateUtc = p.SubmittedDateUtc,
                Status = p.Status.ToString(),
                Semester = p.Semester ?? (p.Module != null ? p.Module.Semester : null),
                Version = p.Version,
                AcademicYear = p.Exam?.AcademicYear

            });
        }

        return new ApiResponse<List<AwaitingApprovalPaperDto>> { success = true, data = result };
    }

    public async Task<ApiResponse<object>> ApproveFinalPaperAsync(int paperId, CancellationToken cancellationToken)
    {
        var paper = await _db.QuestionPapers.FirstOrDefaultAsync(p => p.Id == paperId, cancellationToken);
        if (paper == null) return new ApiResponse<object> { success = false, message = "Paper not found." };

        if (paper.Status != WorkflowStatus.CorrectedSubmitted && paper.Status != WorkflowStatus.AwaitingECApproval)
            return new ApiResponse<object> { success = false, message = "Only CorrectedSubmitted or AwaitingECApproval papers can be finalized." };

        paper.Status = WorkflowStatus.Finalized;
        await _db.SaveChangesAsync(cancellationToken);

        return new ApiResponse<object> { success = true, message = "Paper finalized and approved." };
    }

    public async Task<ApiResponse<object>> RequestCorrectionAsync(int paperId, CancellationToken cancellationToken)
    {
        var paper = await _db.QuestionPapers.FirstOrDefaultAsync(p => p.Id == paperId, cancellationToken);
        if (paper == null) return new ApiResponse<object> { success = false, message = "Paper not found." };

        // Allow requesting correction for papers that are in statuses valid for EC final review
        if (paper.Status != WorkflowStatus.CorrectedSubmitted && paper.Status != WorkflowStatus.AwaitingECApproval)
            return new ApiResponse<object> { success = false, message = "Only CorrectedSubmitted or AwaitingECApproval papers can be sent back for correction by the EC." };

        paper.Status = WorkflowStatus.CorrectionRequired; // Send back to faculty
        
        // Notify Faculty
        try
        {
            var subj = await _db.ModuleSubjects.FirstOrDefaultAsync(s => s.Id == paper.SubjectId, cancellationToken);
            string subjectName = subj?.SubjectName ?? "Unknown Subject";

            await _notificationService.SendNotificationToUsersAsync(
                new List<int> { paper.SubmittedByFacultyUserId },
                $"Exam Coordinator requested correction for your question paper: {subjectName}. Please check the scrutiny report and re-upload.", 
                cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NOTIFICATION ERROR] EC Correction Request: {ex.Message}");
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new ApiResponse<object> { success = true, message = "Correction requested by Exam Coordinator." };
    }
}
