using EventHubASP.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;

namespace EventHubASP.DataAccess
{
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Event> Events { get; set; }
        public DbSet<Registration> Registrations { get; set; }
        public DbSet<RoleChangeRequest> RoleChangeRequests { get; set; }
        public DbSet<News> News { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            Guid adminRoleId = Guid.NewGuid();
            Guid organizerRoleId = Guid.NewGuid();
            Guid userRoleId = Guid.NewGuid();

            modelBuilder.Entity<IdentityRole<Guid>>().HasData(
                new IdentityRole<Guid> { Id = adminRoleId, Name = "Admin", NormalizedName = "ADMIN" },
                new IdentityRole<Guid> { Id = organizerRoleId, Name = "Organizer", NormalizedName = "ORGANIZER" },
                new IdentityRole<Guid> { Id = userRoleId, Name = "User", NormalizedName = "USER" }
            );

            modelBuilder.Entity<Registration>()
                .HasOne(r => r.Event)
                .WithMany(e => e.Registrations)
                .HasForeignKey(r => r.EventID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.News)
                .WithMany()
                .HasForeignKey(n => n.NewsID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<News>()
                .HasOne(n => n.Event)
                .WithMany(e => e.News)
                .HasForeignKey(n => n.EventID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<News>()
                .HasOne(n => n.Organizer)
                .WithMany()
                .HasForeignKey(n => n.OrganizerID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .Property(n => n.IsRead)
                .HasDefaultValue(false);
        }
    }
}