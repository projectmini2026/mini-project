using InternalExamScrutinySystem.Api.Contracts;
using InternalExamScrutinySystem.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using InternalExamScrutinySystem.Api.Helpers;

namespace InternalExamScrutinySystem.Api.Services;

public interface IHodService
{
    Task<ApiResponse<List<UserListDto>>> GetUsersAsync(CancellationToken cancellationToken);
    Task<ApiResponse<List<FacultyResponseDto>>> GetFacultiesAsync(int? moduleId, CancellationToken cancellationToken);
    Task<ApiResponse<object>> CreateFacultyAsync(CreateFacultyRequest request, CancellationToken cancellationToken);
    Task<ApiResponse<object>> UpdateFacultyAsync(int id, UpdateFacultyRequest request, CancellationToken cancellationToken);
    Task<ApiResponse<object>> DeleteFacultyAsync(int id, CancellationToken cancellationToken);
    Task<ApiResponse<object>> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken);

    Task<ApiResponse<List<ModuleListDto>>> GetModulesAsync(CancellationToken cancellationToken);
    Task<ApiResponse<object>> CreateModuleAsync(CreateModuleRequest request, int creatorUserId, CancellationToken cancellationToken);
    Task<ApiResponse<object>> AssignModuleCoordinatorAsync(int moduleId, int userId, CancellationToken cancellationToken);
    Task<ApiResponse<object>> UpdateUserRoleAsync(int userId, Role role, CancellationToken cancellationToken);
    Task<ApiResponse<object>> UpdateModuleAsync(int id, UpdateModuleRequest request, CancellationToken cancellationToken);
    Task<ApiResponse<object>> DeleteModuleAsync(int id, CancellationToken cancellationToken);
    Task<ApiResponse<object>> DeleteSubjectAsync(int id, CancellationToken cancellationToken);
    Task<ApiResponse<object>> AssignFacultyToSubjectAsync(AssignFacultyToSubjectRequest request, CancellationToken cancellationToken);
    Task<ApiResponse<List<FacultyRosterDto>>> GetFacultyRosterAsync(CancellationToken cancellationToken);
    Task<ApiResponse<object>> UpdateModuleCoordinatorAsync(int moduleId, int facultyId, CancellationToken cancellationToken);

    // Module Assignments
    Task<ApiResponse<object>> AssignModuleAsync(CreateModuleAssignmentRequest request, int hodUserId, CancellationToken cancellationToken);
    Task<ApiResponse<object>> DeleteModuleAssignmentAsync(int id, CancellationToken cancellationToken);
    Task<ApiResponse<List<ModuleAssignmentResponse>>> GetModuleAssignmentsAsync(CancellationToken cancellationToken);
}

public class HodService : IHodService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<AppUser> _passwordHasher;

    public HodService(AppDbContext db, IPasswordHasher<AppUser> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<ApiResponse<List<UserListDto>>> GetUsersAsync(CancellationToken cancellationToken)
    {
        var users = await _db.Users
            .Select(u => new UserListDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Role = u.RoleId ?? Role.Faculty
            })
            .ToListAsync(cancellationToken);

        return new ApiResponse<List<UserListDto>> { success = true, message = "Users retrieved.", data = users };
    }

    public async Task<ApiResponse<List<FacultyResponseDto>>> GetFacultiesAsync(int? moduleId, CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine("[DEBUG] GetFacultiesAsync: Fetching faculties from DB...");

            var allUsersCount = await _db.Users.CountAsync(cancellationToken);
            Console.WriteLine($"[DEBUG] Total users in DB: {allUsersCount}");

            var query = _db.Users
                .Where(u => u.RoleId != Role.HOD); // Show everyone except other HODs for now, or just show all.

            if (moduleId.HasValue)
            {
                query = query.Where(u => u.ModuleId == moduleId.Value);
            }

            var faculties = await query
                .Include(u => u.AssignedModule)
                .Select(u => new FacultyResponseDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    Role = u.RoleId ?? Role.Faculty,
                    Position = u.Position,
                    ModuleId = u.ModuleId,
                    ModuleName = u.AssignedModule != null ? u.AssignedModule.ModuleName : null,
                    IsFirstLogin = u.IsFirstLogin ?? true
                })
                .ToListAsync(cancellationToken);

            Console.WriteLine($"[DEBUG] GetFacultiesAsync: Successfully fetched {faculties.Count} faculties after filtering.");
            return new ApiResponse<List<FacultyResponseDto>> { success = true, message = "Faculties retrieved.", data = faculties };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] GetFacultiesAsync Exception: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"[ERROR] GetFacultiesAsync Inner Exception: {ex.InnerException.Message}");
            }
            return new ApiResponse<List<FacultyResponseDto>> 
            { 
                success = false, 
                message = "Failed to retrieve faculties.",
                data = null!
            };
        }
    }

    public async Task<ApiResponse<object>> CreateFacultyAsync(CreateFacultyRequest request, CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine($"[DEBUG] CreateFacultyAsync started. Name: {request.Name}, Email: {request.Email}, ModuleId: {request.ModuleId}");

            if (await _db.Users.AnyAsync(u => u.Email == request.Email, cancellationToken))
            {
                Console.WriteLine($"[DEBUG] CreateFacultyAsync: Email {request.Email} already exists.");
                return new ApiResponse<object> { success = false, message = "Email already exists." };
            }

            var faculty = new AppUser
            {
                Name = request.Name,
                Email = request.Email,
                RoleId = Role.Faculty,
                Position = request.Position,
                ModuleId = request.ModuleId,
                IsFirstLogin = true
            };

            // Generate temporary password
            faculty.PasswordHash = _passwordHasher.HashPassword(faculty, "Temp@123");

            _db.Users.Add(faculty);
            Console.WriteLine("[DEBUG] CreateFacultyAsync: Saving changes to DB...");
            await _db.SaveChangesAsync(cancellationToken);
            Console.WriteLine("[DEBUG] CreateFacultyAsync: Faculty created successfully.");

            return new ApiResponse<object> { success = true, message = "Faculty created successfully. Temporary password: Temp@123" };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] CreateFacultyAsync Exception: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"[ERROR] CreateFacultyAsync Inner Exception: {ex.InnerException.Message}");
            }
            return new ApiResponse<object> { 
                success = false, 
                message = "Failed to create faculty.",
                data = ex.InnerException?.Message ?? ex.Message
            };
        }
    }

    public async Task<ApiResponse<object>> UpdateFacultyAsync(int id, UpdateFacultyRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _db.Users.FindAsync(new object[] { id }, cancellationToken);
            if (user == null) return new ApiResponse<object> { success = false, message = "Faculty not found." };

            user.Name = request.Name;
            user.Email = request.Email;
            user.Position = request.Position;
            user.ModuleId = request.ModuleId;

            await _db.SaveChangesAsync(cancellationToken);
            return new ApiResponse<object> { success = true, message = "Faculty updated successfully." };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] UpdateFacultyAsync: {ex.Message}");
            return new ApiResponse<object> { success = false, message = "Failed to update faculty." };
        }
    }

    public async Task<ApiResponse<object>> DeleteFacultyAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _db.Users.FindAsync(new object[] { id }, cancellationToken);
            if (user == null) return new ApiResponse<object> { success = false, message = "User not found." };

            // 1. Remove FacultySubjectAssignments (NoAction blocker)
            var subjectAssignments = await _db.FacultySubjectAssignments
                .Where(a => a.FacultyId == id)
                .ToListAsync(cancellationToken);
            if (subjectAssignments.Any())
            {
                _db.FacultySubjectAssignments.RemoveRange(subjectAssignments);
            }

            // 2. Nullify Module Coordinator references (SetNull safeguard)
            var coordinatingModules = await _db.Modules
                .Where(m => m.CoordinatorId == id)
                .ToListAsync(cancellationToken);
            foreach (var m in coordinatingModules)
            {
                m.CoordinatorId = null;
            }

            // 3. Remove FacultyAssignments (Cascade is usually on, but manual ensures sync)
            var moduleAssignments = await _db.FacultyAssignments
                .Where(a => a.FacultyId == id)
                .ToListAsync(cancellationToken);
            if (moduleAssignments.Any())
            {
                _db.FacultyAssignments.RemoveRange(moduleAssignments);
            }

            // 4. Remove Notifications
            var notifications = await _db.Notifications
                .Where(n => n.UserId == id)
                .ToListAsync(cancellationToken);
            if (notifications.Any())
            {
                _db.Notifications.RemoveRange(notifications);
            }

            // 5. Finally remove the user
            _db.Users.Remove(user);
            await _db.SaveChangesAsync(cancellationToken);

            return new ApiResponse<object> { success = true, message = "Faculty deleted successfully." };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] DeleteFacultyAsync: {ex.Message}");
            if (ex.InnerException != null) Console.WriteLine($"[ERROR] Inner: {ex.InnerException.Message}");
            
            return new ApiResponse<object> { 
                success = false, 
                message = "Failed to delete faculty. They may have active exam papers or other critical linkings.",
                data = ex.InnerException?.Message ?? ex.Message
            };
        }
    }

    public async Task<ApiResponse<object>> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null) return new ApiResponse<object> { success = false, message = "User not found." };

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
            if (result == PasswordVerificationResult.Failed)
            {
                return new ApiResponse<object> { success = false, message = "Invalid current password." };
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
            user.IsFirstLogin = false;

            await _db.SaveChangesAsync(cancellationToken);

            return new ApiResponse<object> { success = true, message = "Password changed successfully." };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] ChangePasswordAsync: {ex.Message}");
            return new ApiResponse<object> { success = false, message = "Failed to change password." };
        }
    }

    public async Task<ApiResponse<List<ModuleListDto>>> GetModulesAsync(CancellationToken cancellationToken)
    {
        var modules = await _db.Modules
            .Include(m => m.Subjects)
            .Include(m => m.ModuleCoordinator)
            .Select(m => new ModuleListDto
            {
                Id = m.Id,
                ModuleName = m.ModuleName,
                Semester = m.Semester,
                CoordinatorId = m.CoordinatorId,
                CoordinatorName = m.ModuleCoordinator != null 
                    ? (m.ModuleCoordinator.Position.ToShortForm() != null ? m.ModuleCoordinator.Position.ToShortForm() + " " : "") + m.ModuleCoordinator.Name 
                    : "Not assigned",
                Subjects = m.Subjects.Select(s => new CreateModuleSubjectRequest
                {
                    Id = s.Id,
                    SubjectCode = s.SubjectCode,
                    SubjectName = s.SubjectName
                }).ToList(),
                Teachers = _db.FacultyAssignments
                    .Where(fa => fa.ModuleId == m.Id)
                    .Include(fa => fa.Faculty)
                    .Select(fa => (fa.Faculty.Position.ToShortForm() != null ? fa.Faculty.Position.ToShortForm() + " " : "") + fa.Faculty.Name)
                    .Distinct()
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return new ApiResponse<List<ModuleListDto>> { success = true, message = "Modules retrieved.", data = modules };
    }

    public async Task<ApiResponse<object>> CreateModuleAsync(CreateModuleRequest request, int creatorUserId, CancellationToken cancellationToken)
    {
        try
        {
            var moduleCode = request.ModuleName.Length > 50 
                ? request.ModuleName.Substring(0, 50) 
                : request.ModuleName;

            var module = new Module
            {
                ModuleCode = moduleCode,
                ModuleName = request.ModuleName,
                Semester = request.Semester,
                // Assign the creator as the initial coordinator if the DB requires a NOT NULL value
                CoordinatorId = null, // Modules start as 'Not Assigned'
                Subjects = request.Subjects.Select(s => new ModuleSubject
                {
                    SubjectCode = s.SubjectCode,
                    SubjectName = s.SubjectName
                }).ToList()
            };

            _db.Modules.Add(module);
            var result = await _db.SaveChangesAsync(cancellationToken);
            
            Console.WriteLine($"Rows affected: {result}");

            return new ApiResponse<object> { success = true, message = "Module created successfully.", data = new { id = module.Id } };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database error during module creation: {ex.Message}");
            if (ex.InnerException != null) Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            throw; // Rethrow to be caught by the controller
        }
    }

    public async Task<ApiResponse<object>> AssignModuleCoordinatorAsync(int moduleId, int userId, CancellationToken cancellationToken)
    {
        var module = await _db.Modules.FindAsync(new object[] { moduleId }, cancellationToken);
        if (module == null) return new ApiResponse<object> { success = false, message = "Module not found." };

        var user = await _db.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null) return new ApiResponse<object> { success = false, message = "User not found." };

        var oldCoordinatorId = module.CoordinatorId;

        if (user.RoleId != Role.ModuleCoordinator && user.RoleId != Role.HOD)
        {
            user.RoleId = Role.ModuleCoordinator;
        }

        module.CoordinatorId = userId;
        await _db.SaveChangesAsync(cancellationToken);

        // Optional: Demote old coordinator if they coordinate no other modules
        if (oldCoordinatorId.HasValue && oldCoordinatorId.Value != userId)
        {
            var isStillCoordinator = await _db.Modules.AnyAsync(m => m.CoordinatorId == oldCoordinatorId.Value, cancellationToken);
            if (!isStillCoordinator)
            {
                var oldCoord = await _db.Users.FindAsync(new object[] { oldCoordinatorId.Value }, cancellationToken);
                if (oldCoord != null && oldCoord.RoleId == Role.ModuleCoordinator)
                {
                    oldCoord.RoleId = Role.Faculty;
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }
        }

        return new ApiResponse<object> { success = true, message = "Module Coordinator assigned successfully." };
    }

    public async Task<ApiResponse<object>> UpdateUserRoleAsync(int userId, Role role, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null) return new ApiResponse<object> { success = false, message = "User not found." };

        // If assigning ExamCoordinator, demote current ones to Faculty
        if (role == Role.ExamCoordinator)
        {
            var existingEcs = await _db.Users
                .Where(u => u.RoleId == Role.ExamCoordinator)
                .ToListAsync(cancellationToken);
            
            foreach (var ec in existingEcs)
            {
                ec.RoleId = Role.Faculty;
            }
        }

        user.RoleId = role;
        await _db.SaveChangesAsync(cancellationToken);

        return new ApiResponse<object> { success = true, message = $"User role updated to {role}." };
    }

    public async Task<ApiResponse<object>> UpdateModuleAsync(int id, UpdateModuleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var module = await _db.Modules
                .Include(m => m.Subjects)
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

            if (module == null) 
                return new ApiResponse<object> { success = false, message = "Module not found." };

            Console.WriteLine($"[DEBUG] Updating module {id}. Current code: {module.ModuleCode}");

            // Truncate module code if it exceeds 50 characters
            var moduleCode = request.ModuleName.Length > 50 
                ? request.ModuleName.Substring(0, 50) 
                : request.ModuleName;

            // Strip redundant " Updated" suffix if it accrued from previous test scripts
            var sanitizedName = request.ModuleName;
            while (sanitizedName.EndsWith(" Updated"))
            {
                sanitizedName = sanitizedName.Substring(0, sanitizedName.Length - 8);
            }

            module.ModuleCode = moduleCode;
            module.ModuleName = sanitizedName;
            module.Semester = request.Semester;

            // Log subject update
            Console.WriteLine($"[DEBUG] Reconciling subjects for module {id}. Incoming count: {request.Subjects.Count}");

            var existingSubjects = module.Subjects.ToList();
            var incomingSubjectIds = request.Subjects.Where(s => s.Id.HasValue && s.Id > 0).Select(s => s.Id!.Value).ToList();

            // 1. Identify subjects to remove
            var subjectsToRemove = existingSubjects.Where(es => !incomingSubjectIds.Contains(es.Id)).ToList();
            if (subjectsToRemove.Any())
            {
                // Check if any subject to remove is in use
                foreach (var s in subjectsToRemove)
                {
                    var inUseByAssignment = await _db.FacultySubjectAssignments.AnyAsync(a => a.SubjectId == s.Id, cancellationToken);
                    var inUseByExam = await _db.ExamSubjects.AnyAsync(es => es.SubjectId == s.Id, cancellationToken);
                    var inUseByPaper = await _db.QuestionPapers.AnyAsync(qp => qp.SubjectId == s.Id, cancellationToken);

                    if (inUseByAssignment || inUseByExam || inUseByPaper)
                    {
                        return new ApiResponse<object> 
                        { 
                            success = false, 
                            message = $"Cannot remove subject '{s.SubjectName}' ({s.SubjectCode}) because it is already assigned to faculty or linked to an exam/question paper." 
                        };
                    }
                }

                Console.WriteLine($"[DEBUG] Removing {subjectsToRemove.Count} unused subjects.");
                _db.ModuleSubjects.RemoveRange(subjectsToRemove);
            }

            // 2. Update existing and Add new
            foreach (var sReq in request.Subjects)
            {
                if (sReq.Id.HasValue && sReq.Id > 0)
                {
                    var existing = existingSubjects.FirstOrDefault(es => es.Id == sReq.Id.Value);
                    if (existing != null)
                    {
                        existing.SubjectCode = sReq.SubjectCode;
                        existing.SubjectName = sReq.SubjectName;
                    }
                }
                else
                {
                    module.Subjects.Add(new ModuleSubject
                    {
                        ModuleId = id,
                        SubjectCode = sReq.SubjectCode,
                        SubjectName = sReq.SubjectName
                    });
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            return new ApiResponse<object> { success = true, message = "Module updated successfully." };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Database error during module update: {ex.Message}");
            if (ex.InnerException != null) Console.WriteLine($"[ERROR] Inner Exception: {ex.InnerException.Message}");
            throw;
        }
    }

    public async Task<ApiResponse<object>> DeleteModuleAsync(int id, CancellationToken cancellationToken)
    {
        var module = await _db.Modules.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (module == null) return new ApiResponse<object> { success = false, message = "Module not found." };

        var coordinatorId = module.CoordinatorId;

        _db.Modules.Remove(module);
        await _db.SaveChangesAsync(cancellationToken);

        // Demote coordinator if they have no other modules
        if (coordinatorId.HasValue)
        {
            var isStillCoordinator = await _db.Modules.AnyAsync(m => m.CoordinatorId == coordinatorId.Value, cancellationToken);
            if (!isStillCoordinator)
            {
                var coord = await _db.Users.FindAsync(new object[] { coordinatorId.Value }, cancellationToken);
                if (coord != null && coord.RoleId == Role.ModuleCoordinator)
                {
                    coord.RoleId = Role.Faculty;
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }
        }

        return new ApiResponse<object> { success = true, message = "Module deleted successfully." };
    }

    public async Task<ApiResponse<object>> DeleteSubjectAsync(int id, CancellationToken cancellationToken)
    {
        var subject = await _db.ModuleSubjects.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (subject == null) return new ApiResponse<object> { success = false, message = "Subject not found." };

        // Check if subject is in use before deleting
        var inUseByAssignment = await _db.FacultySubjectAssignments.AnyAsync(a => a.SubjectId == id, cancellationToken);
        var inUseByExam = await _db.ExamSubjects.AnyAsync(es => es.SubjectId == id, cancellationToken);
        var inUseByPaper = await _db.QuestionPapers.AnyAsync(qp => qp.SubjectId == id, cancellationToken);

        if (inUseByAssignment || inUseByExam || inUseByPaper)
        {
            return new ApiResponse<object> 
            { 
                success = false, 
                message = $"Cannot delete subject because it is already assigned to faculty or linked to an exam/question paper." 
            };
        }

        _db.ModuleSubjects.Remove(subject);
        await _db.SaveChangesAsync(cancellationToken);
        return new ApiResponse<object> { success = true, message = "Subject deleted successfully." };
    }


    public async Task<ApiResponse<List<FacultyRosterDto>>> GetFacultyRosterAsync(CancellationToken cancellationToken)
    {
        try
        {
            var users = await _db.Users
                .Where(u => u.RoleId != Role.HOD)
                .OrderBy(u => u.Name)
                .Select(u => new FacultyResponseDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    Role = u.RoleId ?? Role.Faculty,
                    Position = u.Position,
                    ModuleId = null,
                    ModuleName = "Global List",
                    IsFirstLogin = u.IsFirstLogin ?? true
                })
                .ToListAsync(cancellationToken);

            var roster = new List<FacultyRosterDto>
            {
                new FacultyRosterDto
                {
                    ModuleId = 0,
                    ModuleName = "All Faculty",
                    Faculty = users
                }
            };

            return new ApiResponse<List<FacultyRosterDto>> { success = true, message = "Faculty list retrieved.", data = roster };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] GetFacultyRosterAsync Exception: {ex.Message}");
            return new ApiResponse<List<FacultyRosterDto>> { success = false, message = "Error: " + ex.Message };
        }
    }

    public async Task<ApiResponse<object>> AssignFacultyToSubjectAsync(AssignFacultyToSubjectRequest request, CancellationToken cancellationToken)
    {
        var module = await _db.Modules.Include(m => m.Subjects).FirstOrDefaultAsync(m => m.Id == request.ModuleId, cancellationToken);
        if (module == null) return new ApiResponse<object> { success = false, message = "Module not found." };

        var subject = module.Subjects.FirstOrDefault(s => s.SubjectName == request.SubjectName);
        if (subject == null) return new ApiResponse<object> { success = false, message = "Subject not found in this module." };

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.FacultyId, cancellationToken);
        if (user == null) return new ApiResponse<object> { success = false, message = "Faculty not found." };

        var assignment = new FacultySubjectAssignment
        {
            ModuleId = request.ModuleId,
            SubjectId = subject.Id,
            FacultyId = request.FacultyId,

            AssignedAtUtc = DateTime.UtcNow
        };

        _db.FacultySubjectAssignments.Add(assignment);
        await _db.SaveChangesAsync(cancellationToken);

        return new ApiResponse<object> { success = true, message = "Faculty assigned to subject successfully." };
    }

    public async Task<ApiResponse<object>> UpdateModuleCoordinatorAsync(int moduleId, int facultyId, CancellationToken cancellationToken)
    {
        var module = await _db.Modules.FindAsync(new object[] { moduleId }, cancellationToken);
        if (module == null) return new ApiResponse<object> { success = false, message = "Module not found." };

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == facultyId, cancellationToken);
        if (user == null) return new ApiResponse<object> { success = false, message = "Faculty not found." };

        var oldCoordinatorId = module.CoordinatorId;

        // Automatic promotion logic if needed (like in AssignModuleCoordinatorAsync)
        if (user.RoleId != Role.ModuleCoordinator && user.RoleId != Role.HOD)
        {
            user.RoleId = Role.ModuleCoordinator;
        }

        module.CoordinatorId = facultyId;
        await _db.SaveChangesAsync(cancellationToken);

        // Demote old coordinator if they coordinate no other modules
        if (oldCoordinatorId.HasValue && oldCoordinatorId.Value != facultyId)
        {
            var isStillCoordinator = await _db.Modules.AnyAsync(m => m.CoordinatorId == oldCoordinatorId.Value, cancellationToken);
            if (!isStillCoordinator)
            {
                var oldCoord = await _db.Users.FindAsync(new object[] { oldCoordinatorId.Value }, cancellationToken);
                if (oldCoord != null && oldCoord.RoleId == Role.ModuleCoordinator)
                {
                    oldCoord.RoleId = Role.Faculty;
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }
        }

        return new ApiResponse<object> { success = true, message = "Module Coordinator updated successfully." };
    }

    public async Task<ApiResponse<object>> AssignModuleAsync(CreateModuleAssignmentRequest request, int hodUserId, CancellationToken cancellationToken)
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
                    // HodUserId could be stored if we added AssignedByUserId to FacultyAssignment
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
}
