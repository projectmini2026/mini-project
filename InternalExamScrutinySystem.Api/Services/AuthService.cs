using InternalExamScrutinySystem.Api.Contracts;
using InternalExamScrutinySystem.Api.Data;
using InternalExamScrutinySystem.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using InternalExamScrutinySystem.Api.Helpers;

namespace InternalExamScrutinySystem.Api.Services;

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<ApiResponse<object>> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(AppDbContext db, IPasswordHasher<AppUser> passwordHasher, IJwtTokenService jwtTokenService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[AUTH-DEBUG] Login attempt for email: '{request.Email}' with password len: {request.Password?.Length ?? 0}");
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (user == null)
        {
            Console.WriteLine($"[AUTH-DEBUG] User not found: '{request.Email}'");
            return new ApiResponse<LoginResponse> { success = false, message = "Invalid email or password.", data = null! };
        }

        Console.WriteLine($"[AUTH-DEBUG] User found: '{user.Email}', stored hash: '{user.PasswordHash}'");
        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        
        if (result == PasswordVerificationResult.Failed)
        {
            Console.WriteLine($"[AUTH-DEBUG] Password verification failed. VerificationResult: {result}");
            return new ApiResponse<LoginResponse> { success = false, message = "Invalid email or password.", data = null! };
        }

        Console.WriteLine($"[AUTH-DEBUG] Login successful for: {request.Email}. Result: {result}");

        var effectiveRole = user.RoleId ?? Role.Faculty;
        
        // If user is a ModuleCoordinator, check if they are actually assigned to any module
        if (effectiveRole == Role.ModuleCoordinator)
        {
            var isAssigned = await _db.Modules.AnyAsync(m => m.CoordinatorId == user.Id, cancellationToken);
            if (!isAssigned)
            {
                effectiveRole = Role.Faculty;
            }
        }

        var token = _jwtTokenService.GenerateToken(user);
        return new ApiResponse<LoginResponse>
        {
            success = true,
            message = "Login successful.",
            data = new LoginResponse
            {
                token = token,
                userId = user.Id,
                role = effectiveRole.ToString(),
                name = user.Name,
                email = user.Email,
                isFirstLogin = user.IsFirstLogin ?? true,
                moduleId = user.ModuleId,
                position = user.Position.ToShortForm()
            }
        };
    }

    public async Task<ApiResponse<object>> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _db.Users.FindAsync(new object[] { userId }, cancellationToken);
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
}

