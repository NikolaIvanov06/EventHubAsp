using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventHubASP.Core;
using EventHubASP.DataAccess;
using EventHubASP.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace EventHubASP.Tests.Tests
{
    [TestFixture]
    public class UserServiceTests
    {
        private ApplicationDbContext _context;
        private Mock<UserManager<User>> _mockUserManager;
        private UserService _userService;

        private static Mock<UserManager<User>> GetMockUserManager()
        {
            var store = new Mock<IUserStore<User>>();
            return new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
        }

        [SetUp]
        public void Setup()
        {
            // ✅ Fix: Use In-Memory Database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;

            _context = new ApplicationDbContext(options);

            // ✅ Fix: Clear previous test data
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();

            // ✅ Fix: Seed test data
            _context.Users.Add(new User { Id = Guid.NewGuid(), UserName = "TestUser", Email = "test@example.com" });
            _context.SaveChanges();

            // ✅ Fix: Create mock UserManager
            _mockUserManager = GetMockUserManager();

            // ✅ Fix: Inject InMemory DB instead of Mock
            _userService = new UserService(_context, _mockUserManager.Object);
        }

        [TearDown]
        public void Teardown()
        {
            _context.Dispose();
        }


        [Test]
        public async Task RegisterUserAsync_ShouldReturnFalse_WhenUserAlreadyExists()
        {
            // ✅ Fix: Query directly from InMemoryDatabase instead of Mocking DbContext
            await _context.Users.AddAsync(new User { UserName = "testUser", Email = "test@example.com" });
            await _context.SaveChangesAsync();

            var result = await _userService.RegisterUserAsync("testUser", "test@example.com", "Password123!");

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task RegisterUserAsync_ShouldReturnTrue_WhenUserIsNew()
        {
            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _userService.RegisterUserAsync("newUser", "new@example.com", "Password123!");

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task ValidateUserAsync_ShouldReturnUser_WhenCredentialsAreCorrect()
        {
            var user = new User { UserName = "testUser", Email = "test@example.com" };
            _mockUserManager.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(true);

            var result = await _userService.ValidateUserAsync("testUser", "Password123!");

            Assert.That(result, Is.Not.Null);
            Assert.That(result.UserName, Is.EqualTo("testUser"));
        }

        [Test]
        public async Task ValidateUserAsync_ShouldReturnNull_WhenUserDoesNotExist()
        {
            _mockUserManager.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((User)null);

            var result = await _userService.ValidateUserAsync("nonExistentUser", "Password123!");

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task ValidateUserAsync_ShouldReturnNull_WhenPasswordIsIncorrect()
        {
            var user = new User { UserName = "testUser" };
            _mockUserManager.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(false);

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
        public async Task GetAllUsersAsync_ShouldReturnListOfUsers()
        {
            await _context.Users.AddRangeAsync(
                new User { UserName = "User1"  , Email = "testUser@example.com" },
                new User { UserName = "User2" , Email = "testUser1@example.com" }
            );
            await _context.SaveChangesAsync();

            var result = await _userService.GetAllUsersAsync();

            Assert.That(result.Count(), Is.EqualTo(3)); // Including the seeded "TestUser"
        }

        [Test]
        public async Task DeleteUserAsync_ShouldReturnTrue_WhenUserIsDeleted()
        {
            var user = new User { Id = Guid.NewGuid(), UserName = "testUser", Email = "testUser@example.com" };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            _mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _userService.DeleteUserAsync(user.Id);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task DeleteUserAsync_ShouldReturnFalse_WhenUserDoesNotExist()
        {
            _mockUserManager.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((User)null);

            var result = await _userService.DeleteUserAsync(Guid.NewGuid());

            Assert.That(result, Is.False);
        }
    }
}
