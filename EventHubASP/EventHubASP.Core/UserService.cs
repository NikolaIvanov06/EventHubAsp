using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using EventHubASP.DataAccess;
using EventHubASP.Models;

namespace EventHubASP.Core
{
    public class UserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        private string HashPassword(string password)
        {
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        public bool RegisterUser(string username, string email, string password, int roleId)
        {
            if (_context.Users.Any(u => u.Username == username || u.Email == email))
            {
                return false;
            }

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = HashPassword(password),
                RoleID = roleId
            };

            _context.Users.Add(user);
            _context.SaveChanges();
            return true;
        }

        // Validate user login
        public User ValidateUser(string username, string password)
        {
            var hashedPassword = HashPassword(password);
            return _context.Users.FirstOrDefault(u => u.Username == username && u.PasswordHash == hashedPassword);
        }
    }
}
