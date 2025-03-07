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
            // Set up in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Using unique name to avoid test interference
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            // Set up UserManager mock
            _mockUserManager = GetMockUserManager();

            // Set up EmailService mock
            _mockEmailService = new Mock<IEmailService>();
            _mockEmailService.Setup(e => e.SendRecoveryCodeAsync(
                It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Create UserService with all dependencies
            _userService = new UserService(_context, _mockUserManager.Object, _mockEmailService.Object);

            // Add a test user
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
            // Arrange - using existing user from setup

            // Act
            var result = await _userService.RegisterUserAsync("existingUser", "new@example.com", "Password123!");

            // Assert
            Assert.That(result, Is.False);
            _mockEmailService.Verify(e => e.SendRecoveryCodeAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task RegisterUserAsync_ShouldReturnFalse_WhenEmailAlreadyExists()
        {
            // Arrange - using existing email from setup

            // Act
            var result = await _userService.RegisterUserAsync("newUser", "existing@example.com", "Password123!");

            // Assert
            Assert.That(result, Is.False);
            _mockEmailService.Verify(e => e.SendRecoveryCodeAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
        [Test]
        public async Task RegisterUserAsync_ShouldReturnTrue_WhenUserIsNew()
        {
            // Arrange
            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<User>()))
                .ReturnsAsync(IdentityResult.Success);

            // Create a separate mock for the password hasher
            var mockPasswordHasher = new Mock<IPasswordHasher<User>>();
            mockPasswordHasher.Setup(x => x.HashPassword(It.IsAny<User>(), It.IsAny<string>()))
                .Returns("hashedRecoveryCode");

            // Use reflection to set the password hasher
            typeof(UserManager<User>)
                .GetProperty("PasswordHasher")
                .SetValue(_mockUserManager.Object, mockPasswordHasher.Object);

            // Act
            var result = await _userService.RegisterUserAsync("newUser", "new@example.com", "Password123!");

            // Assert
            Assert.That(result, Is.True);
            _mockEmailService.Verify(e => e.SendRecoveryCodeAsync("new@example.com", It.IsAny<string>()), Times.Once);
            _mockUserManager.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Once);
        }

        [Test]
        public async Task RegisterUserAsync_ShouldReturnFalse_WhenCreateFails()
        {
            // Arrange
            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Error" }));

            // Act
            var result = await _userService.RegisterUserAsync("newUser", "new@example.com", "Password123!");

            // Assert
            Assert.That(result, Is.False);
            _mockEmailService.Verify(e => e.SendRecoveryCodeAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task ValidateUserAsync_ShouldReturnUser_WhenUserNameIsCorrect()
        {
            // Arrange
            var user = new User { UserName = "testUser", Email = "test@example.com" };
            _mockUserManager.Setup(x => x.FindByNameAsync("testUser")).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.CheckPasswordAsync(user, "Password123!")).ReturnsAsync(true);

            // Act
            var result = await _userService.ValidateUserAsync("testUser", "Password123!");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.UserName, Is.EqualTo("testUser"));
        }

        [Test]
        public async Task ValidateUserAsync_ShouldReturnUser_WhenEmailIsCorrect()
        {
            // Arrange
            var user = new User { UserName = "testUser", Email = "test@example.com" };
            _mockUserManager.Setup(x => x.FindByNameAsync("test@example.com")).ReturnsAsync((User)null);
            _mockUserManager.Setup(x => x.FindByEmailAsync("test@example.com")).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.CheckPasswordAsync(user, "Password123!")).ReturnsAsync(true);

            // Act
            var result = await _userService.ValidateUserAsync("test@example.com", "Password123!");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.UserName, Is.EqualTo("testUser"));
        }

        [Test]
        public async Task ValidateUserAsync_ShouldReturnNull_WhenUserDoesNotExist()
        {
            // Arrange
            _mockUserManager.Setup(x => x.FindByNameAsync("nonExistentUser")).ReturnsAsync((User)null);
            _mockUserManager.Setup(x => x.FindByEmailAsync("nonExistentUser")).ReturnsAsync((User)null);

            // Act
            var result = await _userService.ValidateUserAsync("nonExistentUser", "Password123!");

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task ValidateUserAsync_ShouldReturnNull_WhenPasswordIsIncorrect()
        {
            // Arrange
            var user = new User { UserName = "testUser" };
            _mockUserManager.Setup(x => x.FindByNameAsync("testUser")).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.CheckPasswordAsync(user, "WrongPassword")).ReturnsAsync(false);

            // Act
            var result = await _userService.ValidateUserAsync("testUser", "WrongPassword");

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetUserRoleAsync_ShouldReturnFirstRole()
        {
            // Arrange
            var user = new User { UserName = "testUser" };
            _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });

            // Act
            var result = await _userService.GetUserRoleAsync(user);

            // Assert
            Assert.That(result, Is.EqualTo("Admin"));
        }

        [Test]
        public async Task GetUserRoleAsync_ShouldReturnNull_WhenNoRoles()
        {
            // Arrange
            var user = new User { UserName = "testUser" };
            _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string>());

            // Act
            var result = await _userService.GetUserRoleAsync(user);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetAllUsersAsync_ShouldReturnAllUsers()
        {
            // Arrange - Add two more users in addition to the one in setup
            await _context.Users.AddRangeAsync(
                new User { Id = Guid.NewGuid(), UserName = "User1", Email = "user1@example.com" },
                new User { Id = Guid.NewGuid(), UserName = "User2", Email = "user2@example.com" }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetAllUsersAsync();

            // Assert
            Assert.That(result.Count(), Is.EqualTo(3));
        }

        [Test]
        public async Task DeleteUserAsync_ShouldReturnTrue_WhenUserIsDeleted()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, UserName = "userToDelete" };

            _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userService.DeleteUserAsync(userId);

            // Assert
            Assert.That(result, Is.True);
            _mockUserManager.Verify(x => x.DeleteAsync(user), Times.Once);
        }

        [Test]
        public async Task DeleteUserAsync_ShouldReturnFalse_WhenUserDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            _mockUserManager.Setup(x => x.FindByIdAsync(nonExistentId.ToString())).ReturnsAsync((User)null);

            // Act
            var result = await _userService.DeleteUserAsync(nonExistentId);

            // Assert
            Assert.That(result, Is.False);
            _mockUserManager.Verify(x => x.DeleteAsync(It.IsAny<User>()), Times.Never);
        }

        [Test]
        public async Task DeleteUserAsync_ShouldReturnFalse_WhenDeleteFails()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, UserName = "userToDelete" };

            _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.DeleteAsync(user))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Delete failed" }));

            // Act
            var result = await _userService.DeleteUserAsync(userId);

            // Assert
            Assert.That(result, Is.False);
            _mockUserManager.Verify(x => x.DeleteAsync(user), Times.Once);
        }
    }
}