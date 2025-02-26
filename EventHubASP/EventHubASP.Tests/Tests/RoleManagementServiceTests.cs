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
    public class RoleManagementServiceTests
    {
        private ApplicationDbContext _context;
        private Mock<UserManager<User>> _mockUserManager;
        private Mock<RoleManager<IdentityRole<Guid>>> _mockRoleManager;
        private RoleManagementService _roleManagementService;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test
                .Options;

            _context = new ApplicationDbContext(options);

            _mockUserManager = GetMockUserManager();
            _mockRoleManager = GetMockRoleManager();

            _roleManagementService = new RoleManagementService(_context, _mockUserManager.Object, _mockRoleManager.Object);
        }

        private static Mock<UserManager<User>> GetMockUserManager()
        {
            var store = new Mock<IUserStore<User>>();
            return new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
        }

        private static Mock<RoleManager<IdentityRole<Guid>>> GetMockRoleManager()
        {
            var store = new Mock<IRoleStore<IdentityRole<Guid>>>();
            return new Mock<RoleManager<IdentityRole<Guid>>>(store.Object, null, null, null, null);
        }

        [Test]
        public async Task GetPendingRequestsAsync_ShouldReturnPendingRequests()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), UserName = "testUser", Email = "test@example.com" };
            var request = new RoleChangeRequest
            {
                RequestID = Guid.NewGuid(),
                UserID = user.Id,
                RequestedRole = "Organizer",
                CurrentRole = "User",
                IsApproved = false
            };

            await _context.Users.AddAsync(user);
            await _context.RoleChangeRequests.AddAsync(request);
            await _context.SaveChangesAsync();

            // Act
            var result = await _roleManagementService.GetPendingRequestsAsync();

            // Assert
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].RequestedRole, Is.EqualTo("Organizer")); // Fixed assertion
            Assert.That(result[0].User, Is.Not.Null);
            Assert.That(result[0].User.UserName, Is.EqualTo("testUser"));
        }

        [Test]
        public async Task GetPendingRequestsAsync_ShouldReturnEmptyList_WhenNoPendingRequests()
        {
            // Act
            var result = await _roleManagementService.GetPendingRequestsAsync();

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task ApproveRequestAsync_ShouldApproveRequestAndChangeUserRole()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), UserName = "testUser", Email = "test@example.com" };
            var request = new RoleChangeRequest
            {
                RequestID = Guid.NewGuid(),
                UserID = user.Id,
                RequestedRole = "Organizer",
                CurrentRole = "User",
                IsApproved = false
            };
            var registration = new Registration { RegistrationID = 1, UserID = user.Id };

            await _context.Users.AddAsync(user);
            await _context.RoleChangeRequests.AddAsync(request);
            await _context.Registrations.AddAsync(registration);
            await _context.SaveChangesAsync();

            _mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });
            _mockUserManager.Setup(x => x.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.AddToRoleAsync(user, "Organizer"))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            await _roleManagementService.ApproveRequestAsync(request.RequestID);

            // Assert
            var updatedRequest = await _context.RoleChangeRequests.FindAsync(request.RequestID);
            var remainingRegistrations = await _context.Registrations.AnyAsync(r => r.UserID == user.Id);
            Assert.That(updatedRequest, Is.Null); // Request removed
            Assert.That(remainingRegistrations, Is.False); // Registrations removed
            _mockUserManager.Verify(x => x.RemoveFromRolesAsync(user, It.Is<IEnumerable<string>>(r => r.Contains("User"))), Times.Once);
            _mockUserManager.Verify(x => x.AddToRoleAsync(user, "Organizer"), Times.Once);
        }

        [Test]
        public async Task ApproveRequestAsync_ShouldDoNothing_WhenRequestNotFound()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            await _roleManagementService.ApproveRequestAsync(nonExistentId);

            // Assert
            _mockUserManager.Verify(x => x.FindByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task ApproveRequestAsync_ShouldDoNothing_WhenUserNotFound()
        {
            // Arrange
            var request = new RoleChangeRequest
            {
                RequestID = Guid.NewGuid(),
                UserID = Guid.NewGuid(),
                RequestedRole = "Organizer",
                CurrentRole = "User",
                IsApproved = false
            };

            await _context.RoleChangeRequests.AddAsync(request);
            await _context.SaveChangesAsync();

            _mockUserManager.Setup(x => x.FindByIdAsync(request.UserID.ToString())).ReturnsAsync((User)null);

            // Act
            await _roleManagementService.ApproveRequestAsync(request.RequestID);

            // Assert
            var updatedRequest = await _context.RoleChangeRequests.FindAsync(request.RequestID);
            Assert.That(updatedRequest, Is.Not.Null); // Request remains
        }

        [Test]
        public async Task DeclineRequestAsync_ShouldRemoveRequest()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), UserName = "testUser", Email = "test@example.com" };
            var request = new RoleChangeRequest
            {
                RequestID = Guid.NewGuid(),
                UserID = user.Id,
                RequestedRole = "Organizer",
                CurrentRole = "User",
                IsApproved = false
            };

            await _context.Users.AddAsync(user);
            await _context.RoleChangeRequests.AddAsync(request);
            await _context.SaveChangesAsync();

            // Act
            await _roleManagementService.DeclineRequestAsync(request.RequestID);

            // Assert
            var updatedRequest = await _context.RoleChangeRequests.FindAsync(request.RequestID);
            Assert.That(updatedRequest, Is.Null);
        }

        [Test]
        public async Task DeclineRequestAsync_ShouldDoNothing_WhenRequestNotFound()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            await _roleManagementService.DeclineRequestAsync(nonExistentId);

            // Assert
            var requests = await _context.RoleChangeRequests.AnyAsync();
            Assert.That(requests, Is.False); // No changes
        }

        [Test]
        public async Task SubmitRoleChangeRequestAsync_ShouldAddNewRequest()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), UserName = "testUser", Email = "test@example.com" };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            _mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.IsInRoleAsync(user, "Organizer")).ReturnsAsync(false);
            _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });

            // Act
            await _roleManagementService.SubmitRoleChangeRequestAsync(user.Id, "Organizer");

            // Assert
            var result = await _context.RoleChangeRequests.FirstOrDefaultAsync(r => r.UserID == user.Id);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.RequestedRole, Is.EqualTo("Organizer"));
            Assert.That(result.CurrentRole, Is.EqualTo("User"));
            Assert.That(result.IsApproved, Is.False);
            Assert.That(result.RequestDate, Is.GreaterThan(DateTime.MinValue));
        }

        [Test]
        public async Task SubmitRoleChangeRequestAsync_ShouldNotAddRequest_WhenUserAlreadyInRole()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), UserName = "testUser", Email = "test@example.com" };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            _mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.IsInRoleAsync(user, "Organizer")).ReturnsAsync(true); // Already in role

            // Act
            await _roleManagementService.SubmitRoleChangeRequestAsync(user.Id, "Organizer");

            // Assert
            var result = await _context.RoleChangeRequests.AnyAsync(r => r.UserID == user.Id);
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task SubmitRoleChangeRequestAsync_ShouldDoNothing_WhenUserNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync((User)null);

            // Act
            await _roleManagementService.SubmitRoleChangeRequestAsync(userId, "Organizer");

            // Assert
            var result = await _context.RoleChangeRequests.AnyAsync(r => r.UserID == userId);
            Assert.That(result, Is.False);
        }

        [TearDown]
        public void Teardown()
        {
            _context.Dispose();
        }
    }
}