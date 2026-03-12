using System.ComponentModel.DataAnnotations;
using ASP.NETCORE_with_angular.Enums;

namespace ASP.NETCORE_with_angular.model
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;

        [Required]
        public Role RoleId { get; set; }
    }
}