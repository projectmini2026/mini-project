using InternalExamScrutinySystem.Api.Contracts;
using InternalExamScrutinySystem.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace InternalExamScrutinySystem.Api.Services;

public interface IExamService
{
    Task<ApiResponse<ExamResponse>> CreateExamAsync(CreateExamRequest request, CancellationToken cancellationToken);
    Task<ApiResponse<List<ExamResponse>>> GetExamsAsync(CancellationToken cancellationToken);
    Task<ApiResponse<ExamResponse>> GetActiveExamAsync(CancellationToken cancellationToken);
    Task<ApiResponse<object>> StopExamAsync(int id, CancellationToken cancellationToken);
    Task<ApiResponse<ExamResponse>> GetExamByIdAsync(int id, CancellationToken cancellationToken);
}

public class ExamService : IExamService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;

    public ExamService(AppDbContext db, INotificationService notificationService, IEmailService emailService)
    {
        _db = db;
        _notificationService = notificationService;
        _emailService = emailService;
    }

    public async Task<ApiResponse<ExamResponse>> CreateExamAsync(CreateExamRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // 0. Strict Validation for Dates and Academic Year
            if (request.EndDate <= request.StartDate)
                return new ApiResponse<ExamResponse> { success = false, message = "Exam creation failed: End Date must be later than the Start Date." };

            if (request.LastDateToUpload.Date >= request.StartDate.Date)
                return new ApiResponse<ExamResponse> { success = false, message = "Exam creation failed: Last Date to Upload must be before the Exam Start Date." };

            if (request.LastDateToUpload > request.EndDate)
                return new ApiResponse<ExamResponse> { success = false, message = "Exam creation failed: Last Date to Upload must be on or before the Exam End Date." };

            // Specifically block old years mention by user
            if (request.AcademicYear == "2023 to 2024" || request.AcademicYear == "2023-2024" || 
                request.AcademicYear == "2024 to 2025" || request.AcademicYear == "2024-2025")
                return new ApiResponse<ExamResponse> { success = false, message = "Exam creation failed: The selected Academic Year is no longer active for new examination cycles." };

            // New Validation: Cannot create next academic year cycle while first half of current year is in progress
            var currentUtc = DateTime.UtcNow;
            int currentYear = currentUtc.Year;
            int currentMonth = currentUtc.Month;

            if (request.AcademicYear.Contains($"{currentYear}-{currentYear + 1}") || 
                request.AcademicYear.Contains($"{currentYear} to {currentYear + 1}"))
            {
                if (currentMonth < 7) // Before July
                {
                    int prevYear = currentYear - 1;
                    return new ApiResponse<ExamResponse>
                    {
                        success = false,
                        message = $"Exam creation failed: Cannot create {request.AcademicYear} while {prevYear}-{currentYear} academic year is not completed."
                    };
                }
            }

            // 0.5 Check for duplicate Exam (Same name and academic year)
            var yearExams = await _db.Exams
                .Where(e => e.AcademicYear == request.AcademicYear)
                .ToListAsync(cancellationToken);

            var existingExam = yearExams.Any(e => e.ExamName == request.ExamName);
            
            if (existingExam)
                return new ApiResponse<ExamResponse> { success = false, message = $"Exam creation failed: An exam named '{request.ExamName}' for the academic year '{request.AcademicYear}' already exists." };

            // 0.6 Chronological Validation (Series 1 -> Series 2 -> Retest)
            if (request.ExamName == "Series 2")
            {
                var series1 = yearExams.FirstOrDefault(e => e.ExamName == "Series 1");
                if (series1 == null)
                    return new ApiResponse<ExamResponse> { success = false, message = "Exam creation failed: 'Series 1' must be created before 'Series 2'." };
                if (request.StartDate.Date < series1.EndDate.Date)
                    return new ApiResponse<ExamResponse> { success = false, message = $"Exam creation failed: 'Series 2' must take place after 'Series 1' ends on '{series1.EndDate:dd-MM-yyyy}'." };
            }
            else if (request.ExamName == "Retest")
            {
                var series2 = yearExams.FirstOrDefault(e => e.ExamName == "Series 2");
                if (series2 == null)
                    return new ApiResponse<ExamResponse> { success = false, message = "Exam creation failed: 'Series 2' must be created before 'Retest'." };
                if (request.StartDate.Date < series2.EndDate.Date)
                    return new ApiResponse<ExamResponse> { success = false, message = $"Exam creation failed: 'Retest' must take place after 'Series 2' ends on '{series2.EndDate:dd-MM-yyyy}'." };
            }

            // 1. Deactivate any existing active exams to ensure only one is active at a time
            var activeExams = await _db.Exams.Where(e => e.IsActive).ToListAsync(cancellationToken);
            foreach (var oldExam in activeExams)
            {
                oldExam.IsActive = false;
            }

            // 2. Generate the message dynamically as requested
            string message = $"The '{request.ExamName}' ({request.ActiveSeries}) for the academic year '{request.AcademicYear}' is scheduled to be conducted from '{request.StartDate:dd-MM-yyyy}' to '{request.EndDate:dd-MM-yyyy}'. All assigned faculties are requested to submit their question papers on or before '{request.LastDateToUpload:dd-MM-yyyy}'. Thank you.";

            // 2. Save Exam to DB
            var exam = new Exam
            {
                ExamName = request.ExamName,
                AcademicYear = request.AcademicYear,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                LastDateToUpload = request.LastDateToUpload,
                GeneratedMessage = message,
                IsActive = true,
                ActiveSeries = request.ActiveSeries,
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.Exams.Add(exam);

            // 3. Save Selected Subjects
            if (request.SelectedSubjects != null && request.SelectedSubjects.Any())
            {
                var subjectIds = request.SelectedSubjects.Select(s => s.SubjectId).ToList();
                var validSubjectIds = await _db.ModuleSubjects
                    .AsNoTracking()
                    .Where(s => subjectIds.Contains(s.Id))
                    .Select(s => s.Id)
                    .ToListAsync(cancellationToken);

                foreach (var info in request.SelectedSubjects)
                {
                    if (validSubjectIds.Contains(info.SubjectId))
                    {
                        _db.ExamSubjects.Add(new ExamSubject 
                        { 
                            Exam = exam, // Link via navigation property
                            SubjectId = info.SubjectId,
                            Semester = info.Semester
                        });
                    }
                }
            }

            // 4. Carry over assignments for selected subjects
            if (request.SelectedSubjects != null && request.SelectedSubjects.Any())
            {
                var selectedSubjectIds = request.SelectedSubjects.Select(s => s.SubjectId).ToList();

                var existingAssignments = await _db.FacultySubjectAssignments
                    .Where(a => selectedSubjectIds.Contains(a.SubjectId))
                    .ToListAsync(cancellationToken);

                foreach (var group in existingAssignments.GroupBy(a => a.SubjectId))
                {
                    var latestAssignment = group.OrderByDescending(a => a.Id).First();
                    latestAssignment.Exam = exam; // Bind it via navigation property
                }
            }

            // Save ALL changes at once (Exams, ExamSubjects, Carry-over assignments)
            await _db.SaveChangesAsync(cancellationToken);

            // 5. Send Website Notifications
            List<AppUser> facultyUsers;
            if (request.SelectedSubjects != null && request.SelectedSubjects.Any())
            {
                var subjectIds = request.SelectedSubjects.Select(s => s.SubjectId).ToList();
                var assignments = await _db.FacultySubjectAssignments
                    .Include(a => a.Faculty)
                    .Where(a => subjectIds.Contains(a.SubjectId) && a.ExamId == exam.Id)
                    .ToListAsync(cancellationToken);

                facultyUsers = assignments
                    .Select(a => a.Faculty)
                    .Where(f => f != null)
                    .Cast<AppUser>()
                    .GroupBy(u => u.Id)
                    .Select(g => g.First())
                    .ToList();
            }
            else
            {
                facultyUsers = await _db.Users
                    .Where(u => u.RoleId != Role.HOD)
                    .ToListAsync(cancellationToken);
            }

            var facultyIds = facultyUsers.Select(u => u.Id).ToList();
            if (facultyIds.Any())
            {
                await _notificationService.SendNotificationToUsersAsync(facultyIds, message, cancellationToken);
            }

            // 6. Send Emails in Parallel
            var emailTasks = facultyUsers
                .Where(u => !string.IsNullOrEmpty(u.Email))
                .Select(u => _emailService.SendEmailAsync(u.Email, $"Notice: {request.ExamName}", message));
            
            await Task.WhenAll(emailTasks);

        var response = new ExamResponse
        {
            Id = exam.Id,
            ExamName = exam.ExamName,
            AcademicYear = exam.AcademicYear,
            StartDate = exam.StartDate,
            EndDate = exam.EndDate,
            LastDateToUpload = exam.LastDateToUpload,
            Message = exam.GeneratedMessage ?? string.Empty,
            IsActive = exam.IsActive,
            ActiveSeries = exam.ActiveSeries,
            CreatedAtUtc = exam.CreatedAtUtc,
            SelectedSubjects = exam.ExamSubjects.Select(es => new SelectedSubjectInfo 
            { 
                SubjectId = es.SubjectId, 
                Semester = es.Semester ?? "" 
            }).ToList()
        };

        return new ApiResponse<ExamResponse> { success = true, message = "Exam created and notification sent successfully", data = response };
        }
        catch (Exception ex)
        {
            var detailedError = ex.InnerException != null ? $"{ex.Message} (Inner: {ex.InnerException.Message})" : ex.Message;
            Console.WriteLine($"[ERROR] CreateExamAsync failed: {detailedError}");
            return new ApiResponse<ExamResponse> { success = false, message = $"Failed to open exam: {detailedError}" };
        }
    }

    public async Task<ApiResponse<List<ExamResponse>>> GetExamsAsync(CancellationToken cancellationToken)
    {
        var exams = await _db.Exams
            .Include(e => e.ExamSubjects)
            .OrderByDescending(e => e.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var response = exams.Select(e => new ExamResponse
        {
            Id = e.Id,
            ExamName = e.ExamName,
            AcademicYear = e.AcademicYear,
            StartDate = e.StartDate,
            EndDate = e.EndDate,
            LastDateToUpload = e.LastDateToUpload,
            Message = e.GeneratedMessage ?? string.Empty,
            IsActive = e.IsActive,
            ActiveSeries = e.ActiveSeries,
            CreatedAtUtc = e.CreatedAtUtc,
            SelectedSubjects = e.ExamSubjects.Select(es => new SelectedSubjectInfo 
            { 
                SubjectId = es.SubjectId, 
                Semester = es.Semester ?? "" 
            }).ToList()
        }).ToList();

        return new ApiResponse<List<ExamResponse>> { success = true, data = response };
    }

    public async Task<ApiResponse<ExamResponse>> GetActiveExamAsync(CancellationToken cancellationToken)
    {
        var activeExam = await _db.Exams
            .Include(e => e.ExamSubjects)
            .Where(e => e.IsActive)
            .OrderByDescending(e => e.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeExam == null) return new ApiResponse<ExamResponse> { success = true, data = null! };

        var response = new ExamResponse
        {
            Id = activeExam.Id,
            ExamName = activeExam.ExamName,
            AcademicYear = activeExam.AcademicYear,
            StartDate = activeExam.StartDate,
            EndDate = activeExam.EndDate,
            LastDateToUpload = activeExam.LastDateToUpload,
            Message = activeExam.GeneratedMessage ?? string.Empty,
            IsActive = activeExam.IsActive,
            ActiveSeries = activeExam.ActiveSeries,
            CreatedAtUtc = activeExam.CreatedAtUtc,
            SelectedSubjects = activeExam.ExamSubjects.Select(es => new SelectedSubjectInfo 
            { 
                SubjectId = es.SubjectId, 
                Semester = es.Semester ?? "" 
            }).ToList()
        };

        return new ApiResponse<ExamResponse> { success = true, data = response };
    }

    public async Task<ApiResponse<object>> StopExamAsync(int id, CancellationToken cancellationToken)
    {
        var exam = await _db.Exams.FindAsync(new object[] { id }, cancellationToken);
        if (exam == null) return new ApiResponse<object> { success = false, message = "Exam not found." };

        exam.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);

        return new ApiResponse<object> { success = true, message = $"Exam '{exam.ExamName}' has been closed." };
    }

    public async Task<ApiResponse<ExamResponse>> GetExamByIdAsync(int id, CancellationToken cancellationToken)
    {
        var exam = await _db.Exams
            .Include(e => e.ExamSubjects)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (exam == null) return new ApiResponse<ExamResponse> { success = false, message = "Exam not found." };

        var response = new ExamResponse
        {
            Id = exam.Id,
            ExamName = exam.ExamName,
            AcademicYear = exam.AcademicYear,
            StartDate = exam.StartDate,
            EndDate = exam.EndDate,
            LastDateToUpload = exam.LastDateToUpload,
            Message = exam.GeneratedMessage ?? string.Empty,
            IsActive = exam.IsActive,
            ActiveSeries = exam.ActiveSeries,
            CreatedAtUtc = exam.CreatedAtUtc,
            SelectedSubjects = exam.ExamSubjects.Select(es => new SelectedSubjectInfo 
            { 
                SubjectId = es.SubjectId, 
                Semester = es.Semester ?? "" 
            }).ToList()
        };

        return new ApiResponse<ExamResponse> { success = true, data = response };
    }
}
