using System.ComponentModel.DataAnnotations;

namespace ASP.NETCORE_with_angular.model
{
    public class LoginRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;
    }
}