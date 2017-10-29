using bhive.entities;
using System.Data.Entity;

namespace sfintegration.infrastructure.Service.Bhive
{
    public class BhiveContext : DbContext
    {
        public BhiveContext(string contextName = "BhiveContext") : base(contextName)
        {
        }

        #region Entities
        public DbSet<Activity> Activities { get; set; }
        public DbSet<JobOrder> JobOrders { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserTimeClock> UserTimeClocks { get; set; }
        #endregion


        #region Configuration
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigActivity(modelBuilder);
            ConfigJobOrder(modelBuilder);
            ConfigUser(modelBuilder);
            ConfigUserTimeClock(modelBuilder);
        }

        private void ConfigActivity(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Activity>()
                .ToTable("bh.Activities")
                .HasKey(m => new { m.Id });
        }

        private void ConfigJobOrder(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<JobOrder>()
                .ToTable("bh.JobOrders")
                .HasKey(m => new { m.Id });
        }

        private void ConfigUser(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .ToTable("bh.Users")
                .HasKey(m => new { m.Id });
        }

        private void ConfigUserTimeClock(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserTimeClock>()
                .ToTable("bh.UserTimeClocks")
                .HasKey(m => new { m.UserId, m.ActivityId, m.StartTime });

            modelBuilder.Entity<UserTimeClock>()
                .HasRequired(m => m.JobOrder);
        }
        #endregion
    }
}
