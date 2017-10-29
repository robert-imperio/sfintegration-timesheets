using sfintegration.entities;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace sfintegration.infrastructure.Service.IntegrationDB
{
    public class SFIntegrationContext : DbContext
    {
        public SFIntegrationContext(string contextName = "SFIntegrationContext") : base(contextName)
        {
        }

        public DbSet<TimeSheet> TimeSheets { get; set; }
        public DbSet<TimeSheetActivity> TimeSheetActivities { get; set; }
        public DbSet<UserTimeClock> UserTimeClocks { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigTimeSheet(modelBuilder);
            ConfigTimeSheetActivity(modelBuilder);
            ConfigUserTimeClock(modelBuilder);
        }

        private void ConfigTimeSheet(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TimeSheet>()
                .ToTable("TimeSheets")
                .HasKey(m => new { m.Id });
        }

        private void ConfigTimeSheetActivity(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TimeSheetActivity>()
                .ToTable("TimeSheetActivities")
                .HasKey(m => new { m.Id });
        }

        private void ConfigUserTimeClock(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserTimeClock>()
                .ToTable("UserTimeClocks")                
                .HasKey(m => new { m.JobOrderId, m.UserId, m.ActivityId, m.StartTime });            
        }
    }
}
