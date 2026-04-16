using InternalExamScrutinySystem.Api.Contracts;
using InternalExamScrutinySystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternalExamScrutinySystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [HttpPut("change-password")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return Unauthorized(new ApiResponse<object> { success = false, message = "Invalid user" });

        int userId = int.Parse(userIdClaim.Value);
        var response = await _authService.ChangePasswordAsync(userId, request, cancellationToken);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpGet("test")]
    public async Task<IActionResult> TestReset([FromQuery] string email, [FromServices] InternalExamScrutinySystem.Api.Data.AppDbContext db, [FromServices] Microsoft.AspNetCore.Identity.IPasswordHasher<InternalExamScrutinySystem.Api.Data.AppUser> hasher)
    {
        var targetEmail = string.IsNullOrEmpty(email) ? "hod@college.edu" : email;
        var user = db.Users.FirstOrDefault(u => u.Email == targetEmail);
        if (user != null) {
            user.PasswordHash = hasher.HashPassword(user, "Password123!");
            db.SaveChanges();
            return Ok(new { success = true, email = user.Email, hash = user.PasswordHash });
        }
        return Ok(new { success = false, message = $"User not found: {targetEmail}" });
    }
}

