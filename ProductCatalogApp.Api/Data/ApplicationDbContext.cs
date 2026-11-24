using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProductCatalogApp.Api.Models;

namespace ProductCatalogApp.Api.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Sensor> Sensors => Set<Sensor>();
        public DbSet<SensorReading> SensorReadings => Set<SensorReading>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<SensorReading>().HasIndex(r => new { r.SensorId, r.Timestamp });
        }

        public DbSet<ProductionSite> ProductionSites =>
            Set<ProductionSite>();

        public DbSet<Equipment> EquipmentItems =>
            Set<Equipment>();
    }
}
