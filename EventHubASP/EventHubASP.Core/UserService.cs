using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventHubASP.DataAccess;
using EventHubASP.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EventHubASP.Core
{
    public class UserService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IEmailService _emailService;

        public UserService(ApplicationDbContext context, UserManager<User> userManager, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        private string GenerateRecoveryCode()
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public async Task<bool> RegisterUserAsync(string username, string email, string password)
        {
            if (await _context.Users.AnyAsync(u => u.UserName == username || u.Email == email))
            {
                return false;
            }

            var user = new User
            {
                UserName = username,
                Email = email
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                var recoveryCode = GenerateRecoveryCode();
                var hashedRecoveryCode = _userManager.PasswordHasher.HashPassword(user, recoveryCode);
                user.HashedRecoveryCode = hashedRecoveryCode;
                await _userManager.UpdateAsync(user);
                await _emailService.SendRecoveryCodeAsync(user.Email, recoveryCode);
                return true;
            }
            return false;
        }

        public async Task<User> ValidateUserAsync(string username, string password)
        {
            var user = await _userManager.FindByNameAsync(username) ?? await _userManager.FindByEmailAsync(username);
            if (user != null)
            {
                var passwordValid = await _userManager.CheckPasswordAsync(user, password);
                if (passwordValid)
                {
                    return user;
                }
                else
                {
                    Console.WriteLine("Invalid password for user: " + username);
                }
            }
            else
            {
                Console.WriteLine("User not found: " + username);
            }
            return null;
        }

        public async Task<string> GetUserRoleAsync(User user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            return roles.FirstOrDefault();
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<bool> DeleteUserAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user != null)
            {
                var result = await _userManager.DeleteAsync(user);
                return result.Succeeded;
            }
            return false;
        }
    }
}