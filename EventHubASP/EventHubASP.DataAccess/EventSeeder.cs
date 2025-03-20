using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using EventHubASP.Models;
using Microsoft.AspNetCore.Identity;

namespace EventHubASP.DataAccess
{
    public static class EventSeeder
    {
        public static async Task SeedEvents(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();

            if (!context.Events.Any())
            {
                var admin = await userManager.FindByEmailAsync("admin@example.com");
                var organizer = await userManager.FindByEmailAsync("organizer@example.com");
                var regularUser = await userManager.FindByEmailAsync("user@example.com");

                var events = new[]
                {
                    new Event
                    {
                        Title = "Tech Conference 2025",
                        Description = "Annual technology conference featuring latest innovations",
                        Date = DateTime.Now.AddMonths(1),
                        Location = "Convention Center, Downtown",
                        OrganizerID = organizer.Id,
                        Organizer = organizer,
                        ImageUrl = "https://images.unsplash.com/photo-1540575467063-1f587b372844?ixlib=rb-4.0.3&auto=format&fit=crop&w=1350&q=80"
                    },
                    new Event
                    {
                        Title = "Music Festival",
                        Description = "Three-day outdoor music festival with top artists",
                        Date = DateTime.Now.AddDays(15),
                        Location = "City Park",
                        OrganizerID = organizer.Id,
                        Organizer = organizer,
                        ImageUrl = "https://images.pexels.com/photos/167636/pexels-photo-167636.jpeg?auto=compress&cs=tinysrgb&w=1260&h=750&dpr=1"
                    },
                    new Event
                    {
                        Title = "Admin Training Workshop",
                        Description = "Training session for system administrators",
                        Date = DateTime.Now.AddDays(7),
                        Location = "Tech Hub Building",
                        OrganizerID = organizer.Id,
                        Organizer = organizer,
                        ImageUrl = "https://images.unsplash.com/photo-1517245386807-bb43f82c33c4?ixlib=rb-4.0.3&auto=format&fit=crop&w=1350&q=80"
                    },
                    new Event
                    {
                        Title = "Charity Run 5K",
                        Description = "Community run to raise funds for local charities",
                        Date = DateTime.Now.AddDays(30),
                        Location = "Riverside Park",
                        OrganizerID = organizer.Id,
                        Organizer = organizer,
                        ImageUrl = "https://images.pexels.com/photos/235922/pexels-photo-235922.jpeg?auto=compress&cs=tinysrgb&w=1260&h=750&dpr=1"
                    },
                    new Event
                    {
                        Title = "Art Exhibition Opening",
                        Description = "Showcase of local artists' works",
                        Date = DateTime.Now.AddDays(10),
                        Location = "City Art Gallery",
                        OrganizerID = organizer.Id,
                        Organizer = organizer,
                        ImageUrl = "https://images.unsplash.com/photo-1533722305675-9e838a7812db?ixlib=rb-4.0.3&auto=format&fit=crop&w=1350&q=80"
                    },
                    new Event
                    {
                        Title = "Food Truck Festival",
                        Description = "A gathering of the region's best food trucks",
                        Date = DateTime.Now.AddDays(20),
                        Location = "Main Street Square",
                        OrganizerID = organizer.Id,
                        Organizer = organizer,
                        ImageUrl = "https://images.pexels.com/photos/1352169/pexels-photo-1352169.jpeg?auto=compress&cs=tinysrgb&w=1260&h=750&dpr=1"
                    }
                };
                context.Events.AddRange(events);
                await context.SaveChangesAsync();
            }
        }
    }
}