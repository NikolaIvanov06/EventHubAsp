using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using EventHubASP.Models;

namespace EventHubASP.DataAccess
{
    public static class IdentitySeeder
    {
        public static async Task SeedRolesAndUsers(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();

            string[] roles = { "Admin", "Organizer", "User" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole<Guid> { Name = role, NormalizedName = role.ToUpper() });
                }
            }

            await CreateUserIfNotExists(userManager, "admin@example.com", "AdminUser", "Admin@123", "Admin");
            await CreateUserIfNotExists(userManager, "organizer@example.com", "OrganizerUser", "Organizer@123", "Organizer");
            await CreateUserIfNotExists(userManager, "user@example.com", "RegularUser", "User@123", "User");
        }

        private static async Task CreateUserIfNotExists(UserManager<User> userManager, string email, string username, string password, string role)
        {
            var existingUser = await userManager.FindByEmailAsync(email);
            if (existingUser == null)
            {
                var user = new User
                {
                    UserName = username,
                    Email = email,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                }
                else
                {
                    Console.WriteLine($"Failed to create user {username}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    throw new Exception($"User creation failed for {username}");
                }
            }
        }
    }
}   