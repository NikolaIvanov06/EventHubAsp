using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventHubASP.Core;
using EventHubASP.DataAccess;
using EventHubASP.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Moq;
using NUnit.Framework;

namespace EventHubASP.Tests.Tests
{
    [TestFixture]
    public class UserServiceTests
    {
        private ApplicationDbContext _context;
        private Mock<UserManager<User>> _mockUserManager;
        private Mock<IEmailService> _mockEmailService;
        private UserService _userService;

        private static Mock<UserManager<User>> GetMockUserManager()
        {
            var store = new Mock<IUserStore<User>>();
            return new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
        }

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Using unique name to avoid test interference
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            _mockUserManager = GetMockUserManager();

            _mockEmailService = new Mock<IEmailService>();
            _mockEmailService.Setup(e => e.SendRecoveryCodeAsync(
                It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _userService = new UserService(_context, _mockUserManager.Object, _mockEmailService.Object);

            _context.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                UserName = "existingUser",
                Email = "existing@example.com"
            });
            _context.SaveChanges();
        }

        [TearDown]
        public void Teardown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public async Task RegisterUserAsync_ShouldReturnFalse_WhenUserAlreadyExists()
        {

            var result = await _userService.RegisterUserAsync("existingUser", "new@example.com", "Password123!");

            Assert.That(result, Is.False);
            _mockEmailService.Verify(e => e.SendRecoveryCodeAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task RegisterUserAsync_ShouldReturnFalse_WhenEmailAlreadyExists()
        {

            var result = await _userService.RegisterUserAsync("newUser", "existing@example.com", "Password123!");

            Assert.That(result, Is.False);
            _mockEmailService.Verify(e => e.SendRecoveryCodeAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
        [Test]
        public async Task RegisterUserAsync_ShouldReturnTrue_WhenUserIsNew()
        {
            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<User>()))
                .ReturnsAsync(IdentityResult.Success);

            var mockPasswordHasher = new Mock<IPasswordHasher<User>>();
            mockPasswordHasher.Setup(x => x.HashPassword(It.IsAny<User>(), It.IsAny<string>()))
                .Returns("hashedRecoveryCode");

            typeof(UserManager<User>)
                .GetProperty("PasswordHasher")
                .SetValue(_mockUserManager.Object, mockPasswordHasher.Object);

            var result = await _userService.RegisterUserAsync("newUser", "new@example.com", "Password123!");

            Assert.That(result, Is.True);
            _mockEmailService.Verify(e => e.SendRecoveryCodeAsync("new@example.com", It.IsAny<string>()), Times.Once);
            _mockUserManager.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Once);
        }

        [Test]
        public async Task RegisterUserAsync_ShouldReturnFalse_WhenCreateFails()
        {
            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Error" }));

            var result = await _userService.RegisterUserAsync("newUser", "new@example.com", "Password123!");

            Assert.That(result, Is.False);
            _mockEmailService.Verify(e => e.SendRecoveryCodeAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task ValidateUserAsync_ShouldReturnUser_WhenUserNameIsCorrect()
        {
            var user = new User { UserName = "testUser", Email = "test@example.com" };
            _mockUserManager.Setup(x => x.FindByNameAsync("testUser")).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.CheckPasswordAsync(user, "Password123!")).ReturnsAsync(true);

            var result = await _userService.ValidateUserAsync("testUser", "Password123!");

            Assert.That(result, Is.Not.Null);
            Assert.That(result.UserName, Is.EqualTo("testUser"));
        }

        [Test]
        public async Task ValidateUserAsync_ShouldReturnUser_WhenEmailIsCorrect()
        {
            var user = new User { UserName = "testUser", Email = "test@example.com" };
            _mockUserManager.Setup(x => x.FindByNameAsync("test@example.com")).ReturnsAsync((User)null);
            _mockUserManager.Setup(x => x.FindByEmailAsync("test@example.com")).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.CheckPasswordAsync(user, "Password123!")).ReturnsAsync(true);

            var result = await _userService.ValidateUserAsync("test@example.com", "Password123!");

            Assert.That(result, Is.Not.Null);
            Assert.That(result.UserName, Is.EqualTo("testUser"));
        }

        [Test]
        public async Task ValidateUserAsync_ShouldReturnNull_WhenUserDoesNotExist()
        {
            _mockUserManager.Setup(x => x.FindByNameAsync("nonExistentUser")).ReturnsAsync((User)null);
            _mockUserManager.Setup(x => x.FindByEmailAsync("nonExistentUser")).ReturnsAsync((User)null);

            var result = await _userService.ValidateUserAsync("nonExistentUser", "Password123!");

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task ValidateUserAsync_ShouldReturnNull_WhenPasswordIsIncorrect()
        {
            var user = new User { UserName = "testUser" };
            _mockUserManager.Setup(x => x.FindByNameAsync("testUser")).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.CheckPasswordAsync(user, "WrongPassword")).ReturnsAsync(false);

            var result = await _userService.ValidateUserAsync("testUser", "WrongPassword");

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetUserRoleAsync_ShouldReturnFirstRole()
        {
            var user = new User { UserName = "testUser" };
            _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });

            var result = await _userService.GetUserRoleAsync(user);

            Assert.That(result, Is.EqualTo("Admin"));
        }

        [Test]
        public async Task GetUserRoleAsync_ShouldReturnNull_WhenNoRoles()
        {
            var user = new User { UserName = "testUser" };
            _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string>());

            var result = await _userService.GetUserRoleAsync(user);

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetAllUsersAsync_ShouldReturnAllUsers()
        {
            await _context.Users.AddRangeAsync(
                new User { Id = Guid.NewGuid(), UserName = "User1", Email = "user1@example.com" },
                new User { Id = Guid.NewGuid(), UserName = "User2", Email = "user2@example.com" }
            );
            await _context.SaveChangesAsync();

            var result = await _userService.GetAllUsersAsync();

            Assert.That(result.Count(), Is.EqualTo(3));
        }

        [Test]
        public async Task DeleteUserAsync_ShouldReturnTrue_WhenUserIsDeleted()
        {
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, UserName = "userToDelete" };

            _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _userService.DeleteUserAsync(userId);

            Assert.That(result, Is.True);
            _mockUserManager.Verify(x => x.DeleteAsync(user), Times.Once);
        }

        [Test]
        public async Task DeleteUserAsync_ShouldReturnFalse_WhenUserDoesNotExist()
        {
            var nonExistentId = Guid.NewGuid();
            _mockUserManager.Setup(x => x.FindByIdAsync(nonExistentId.ToString())).ReturnsAsync((User)null);

            var result = await _userService.DeleteUserAsync(nonExistentId);

            Assert.That(result, Is.False);
            _mockUserManager.Verify(x => x.DeleteAsync(It.IsAny<User>()), Times.Never);
        }

        [Test]
        public async Task DeleteUserAsync_ShouldReturnFalse_WhenDeleteFails()
        {
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, UserName = "userToDelete" };

            _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.DeleteAsync(user))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Delete failed" }));

            var result = await _userService.DeleteUserAsync(userId);

            Assert.That(result, Is.False);
            _mockUserManager.Verify(x => x.DeleteAsync(user), Times.Once);
        }
    }
}