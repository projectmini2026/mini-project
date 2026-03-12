using System.ComponentModel.DataAnnotations;

namespace ASP.NETCORE_with_angular.model
{
    public class Module
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ModuleCode { get; set; } = null!;

        [Required]
        public string Name { get; set; } = null!;

        [Required]
        public int SubjectCount { get; set; }
    }
}