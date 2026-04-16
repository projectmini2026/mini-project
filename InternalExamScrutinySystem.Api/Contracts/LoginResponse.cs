namespace InternalExamScrutinySystem.Api.Contracts;

public class LoginResponse
{
    public string token { get; set; } = string.Empty;
    public int userId { get; set; }
    public string name { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string role { get; set; } = string.Empty;
    public bool isFirstLogin { get; set; }
    public int? moduleId { get; set; }
    public string? position { get; set; }
}