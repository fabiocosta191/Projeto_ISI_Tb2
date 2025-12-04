using Microsoft.EntityFrameworkCore;
using SafeHome.Data.Models;

namespace SafeHome.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Sensor> Sensors { get; set; }
        public DbSet<SensorReading> SensorReadings { get; set; }

        // Novas Tabelas
        public DbSet<Building> Buildings { get; set; }
        public DbSet<Incident> Incidents { get; set; }
        public DbSet<Alert> Alerts { get; set; }
    }
}