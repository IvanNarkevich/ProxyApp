using Microsoft.EntityFrameworkCore;

namespace ProxyApp.Models
{
    public class DatabaseContext : DbContext
    {
        public DbSet<Rule> Rules { get; set; }
        public DatabaseContext(DbContextOptions<DatabaseContext> options)
            : base(options)
        {

            Database.EnsureCreated();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Rule>().HasData(
                new Rule { Id = 1, allowed_C01_Belarus_WGS84 = 0, allowed_A06_ATE_TE_WGS84 = 0,
                allowed_A05_EGRNI_WGS84 = 0, allowed_A01_ZIS_WGS84 = 0}
            );
        }
    }
}
