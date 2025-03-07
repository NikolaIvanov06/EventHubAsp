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
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
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

            var result = await _roleManagementService.GetPendingRequestsAsync();

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].RequestedRole, Is.EqualTo("Organizer"));
            Assert.That(result[0].User, Is.Not.Null);
            Assert.That(result[0].User.UserName, Is.EqualTo("testUser"));
        }

        [Test]
        public async Task GetPendingRequestsAsync_ShouldReturnEmptyList_WhenNoPendingRequests()
        {
            var result = await _roleManagementService.GetPendingRequestsAsync();

            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task ApproveRequestAsync_ShouldApproveRequestAndChangeUserRole()
        {
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

            await _roleManagementService.ApproveRequestAsync(request.RequestID);

            var updatedRequest = await _context.RoleChangeRequests.FindAsync(request.RequestID);
            var remainingRegistrations = await _context.Registrations.AnyAsync(r => r.UserID == user.Id);
            Assert.That(updatedRequest, Is.Null);
            Assert.That(remainingRegistrations, Is.False);
            _mockUserManager.Verify(x => x.RemoveFromRolesAsync(user, It.Is<IEnumerable<string>>(r => r.Contains("User"))), Times.Once);
            _mockUserManager.Verify(x => x.AddToRoleAsync(user, "Organizer"), Times.Once);
        }

        [Test]
        public async Task ApproveRequestAsync_ShouldDoNothing_WhenRequestNotFound()
        {
            var nonExistentId = Guid.NewGuid();

            await _roleManagementService.ApproveRequestAsync(nonExistentId);

            _mockUserManager.Verify(x => x.FindByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task ApproveRequestAsync_ShouldDoNothing_WhenUserNotFound()
        {
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

            await _roleManagementService.ApproveRequestAsync(request.RequestID);

            var updatedRequest = await _context.RoleChangeRequests.FindAsync(request.RequestID);
            Assert.That(updatedRequest, Is.Not.Null);
        }

        [Test]
        public async Task DeclineRequestAsync_ShouldRemoveRequest()
        {
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

            await _roleManagementService.DeclineRequestAsync(request.RequestID);

            var updatedRequest = await _context.RoleChangeRequests.FindAsync(request.RequestID);
            Assert.That(updatedRequest, Is.Null);
        }

        [Test]
        public async Task DeclineRequestAsync_ShouldDoNothing_WhenRequestNotFound()
        {
            var nonExistentId = Guid.NewGuid();

            await _roleManagementService.DeclineRequestAsync(nonExistentId);

            var requests = await _context.RoleChangeRequests.AnyAsync();
            Assert.That(requests, Is.False);
        }

        [Test]
        public async Task SubmitRoleChangeRequestAsync_ShouldAddNewRequest()
        {
            var user = new User { Id = Guid.NewGuid(), UserName = "testUser", Email = "test@example.com" };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            _mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.IsInRoleAsync(user, "Organizer")).ReturnsAsync(false);
            _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });

            await _roleManagementService.SubmitRoleChangeRequestAsync(user.Id, "Organizer");

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
            var user = new User { Id = Guid.NewGuid(), UserName = "testUser", Email = "test@example.com" };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            _mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.IsInRoleAsync(user, "Organizer")).ReturnsAsync(true);

            await _roleManagementService.SubmitRoleChangeRequestAsync(user.Id, "Organizer");

            var result = await _context.RoleChangeRequests.AnyAsync(r => r.UserID == user.Id);
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task SubmitRoleChangeRequestAsync_ShouldDoNothing_WhenUserNotFound()
        {
            var userId = Guid.NewGuid();
            _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync((User)null);

            await _roleManagementService.SubmitRoleChangeRequestAsync(userId, "Organizer");

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