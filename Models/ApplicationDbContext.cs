using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.EntityFrameworkCore;

namespace LocalLinker.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }

        public DbSet<users> Users { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<ServiceProvider> ServiceProviders { get; set; }
        public DbSet<ServiceRequest> ServiceRequests { get; set; }
        public DbSet<Reviews> Reviews { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Booking> Booking { get; set; }
    }
}
