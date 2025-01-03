using EventHubASP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;

namespace EventHubASP.DataAccess;

public class ApplicationDbContext : DbContext
{

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Registration> Registrations { get; set; }
    public DbSet<RoleChangeRequest> RoleChangeRequests { get; set; }
    public DbSet<News> News { get; set; }
    public DbSet<Notification> Notifications { get; set; }




    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Registration -> Event (Cascade Delete)
        modelBuilder.Entity<Registration>()
            .HasOne(r => r.Event)
            .WithMany(e => e.Registrations)
            .HasForeignKey(r => r.EventID)
            .OnDelete(DeleteBehavior.Cascade);

        // User -> Role (No Cascade)
        modelBuilder.Entity<User>()
            .HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleID)
            .OnDelete(DeleteBehavior.Restrict);

        // Event -> User (Restrict Delete for Organizer)
        modelBuilder.Entity<Event>()
            .HasOne(e => e.Organizer)
            .WithMany(u => u.Events)
            .HasForeignKey(e => e.OrganizerID)
            .OnDelete(DeleteBehavior.Restrict);

        // Notification -> User (Restrict Delete)
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserID)
            .OnDelete(DeleteBehavior.Restrict);

        // Registration -> User (Restrict Delete)
        modelBuilder.Entity<Registration>()
            .HasOne(r => r.User)
            .WithMany(u => u.Registrations)
            .HasForeignKey(r => r.UserID)
            .OnDelete(DeleteBehavior.Restrict);

        // News -> Event (Cascade Delete)
        modelBuilder.Entity<News>()
            .HasOne(n => n.Event)
            .WithMany(e => e.News)
            .HasForeignKey(n => n.EventID)
            .OnDelete(DeleteBehavior.Cascade);

        // Notification -> News (Restrict Delete)
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.News)
            .WithMany()
            .HasForeignKey(n => n.NewsID)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed Roles
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleID = 1, RoleName = "User" },
            new Role { RoleID = 2, RoleName = "Organizer" },
            new Role { RoleID = 3, RoleName = "Admin" }
        );
    }


}
