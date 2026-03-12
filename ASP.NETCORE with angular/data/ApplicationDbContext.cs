using Microsoft.EntityFrameworkCore;
using ASP.NETCORE_with_angular.model;

namespace ASP.NETCORE_with_angular.data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ApplicationUser> Users { get; set; }

        public DbSet<product> products { get; set; }
    }
}