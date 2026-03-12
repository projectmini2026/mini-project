using System.ComponentModel.DataAnnotations;

namespace ASP.NETCORE_with_angular.model
{
    public class product
    {
        [Key]

        public int Id { get; set; }
        public required string productname { get; set; }
        public required string price { get; set; }

        public required string Description { get; set; }
        public int Rating { get; set; }
        public bool status  { get; set; }
    }
}
