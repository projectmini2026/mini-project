using System.ComponentModel.DataAnnotations;

namespace ASP.NETCORE_with_angular.model
{
    public class ApplicationUser
    {
        public int Id { get; set; }

        [Required, MaxLength(256)]
        public string Email { get; set; } = null!;

        [Required]
        public string PasswordHash { get; set; } = null!;
    }
}