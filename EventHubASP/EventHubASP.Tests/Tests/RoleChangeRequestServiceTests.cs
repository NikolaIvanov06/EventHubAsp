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
    public class RoleChangeRequestServiceTests
    {
        private ApplicationDbContext _context;
        private Mock<UserManager<User>> _mockUserManager;
        private Mock<RoleManager<IdentityRole<Guid>>> _mockRoleManager;
        private RoleChangeRequestService _roleChangeRequestService;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;

            _context = new ApplicationDbContext(options);

            _mockUserManager = GetMockUserManager();
            _mockRoleManager = GetMockRoleManager();

            _roleChangeRequestService = new RoleChangeRequestService(_context, _mockUserManager.Object, _mockRoleManager.Object);
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

        [TearDown]
        public void Teardown()
        {
            _context.Dispose();
        }

        [Test]
        public async Task CreateRequestAsync_ShouldReturnFalse_WhenRequestAlreadyExists()
        {
            var user = new User { Id = Guid.NewGuid(), UserName = "testUser", Email = "testUser@example.com" };
            var request = new RoleChangeRequest
            {
                UserID = user.Id,
                CurrentRole = "User",
                RequestedRole = "Admin",
                IsApproved = false
            };

            await _context.Users.AddAsync(user);
            await _context.RoleChangeRequests.AddAsync(request);
            await _context.SaveChangesAsync();

            var result = await _roleChangeRequestService.CreateRequestAsync(user.Id, "User", "Admin");

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CreateRequestAsync_ShouldReturnTrue_WhenRequestIsNew()
        {
            var user = new User { Id = Guid.NewGuid(), UserName = "testUser", Email = "testUser@example.com" };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var result = await _roleChangeRequestService.CreateRequestAsync(user.Id, "User", "Admin");

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task GetPendingRequestsAsync_ShouldReturnPendingRequests()
        {
            var user = new User { Id = Guid.NewGuid(), UserName = "testUser", Email = "testUser@example.com" };
            var request = new RoleChangeRequest
            {
                UserID = user.Id,
                CurrentRole = "User",
                RequestedRole = "Admin",
                IsApproved = false
            };

            await _context.Users.AddAsync(user);
            await _context.RoleChangeRequests.AddAsync(request);
            await _context.SaveChangesAsync();

            var result = await _roleChangeRequestService.GetPendingRequestsAsync();
            Assert.That(result.First().RequestedRole, Is.EqualTo("Admin"));
        }

        [Test]
        public async Task ApproveRequestAsync_ShouldApproveRequestAndChangeUserRole()
        {
            var user = new User { Id = Guid.NewGuid(), UserName = "testUser", Email = "testUser@example.com" };
            var request = new RoleChangeRequest
            {
                UserID = user.Id,
                CurrentRole = "User",
                RequestedRole = "Admin",
                IsApproved = false
            };

            await _context.Users.AddAsync(user);
            await _context.RoleChangeRequests.AddAsync(request);
            await _context.SaveChangesAsync();

            _mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });
            _mockUserManager.Setup(x => x.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.AddToRoleAsync(user, "Admin")).ReturnsAsync(IdentityResult.Success);

            await _roleChangeRequestService.ApproveRequestAsync(request.RequestID);

            var updatedRequest = await _context.RoleChangeRequests.FindAsync(request.RequestID);
            Assert.That(updatedRequest, Is.Null);
        }

        [Test]
        public async Task DenyRequestAsync_ShouldRemoveRequest()
        {
            var user = new User { Id = Guid.NewGuid(), UserName = "testUser", Email = "testUser@example.com" };
            var request = new RoleChangeRequest
            {
                UserID = user.Id,
                CurrentRole = "User",
                RequestedRole = "Admin",
                IsApproved = false
            };

            await _context.Users.AddAsync(user);
            await _context.RoleChangeRequests.AddAsync(request);
            await _context.SaveChangesAsync();

            await _roleChangeRequestService.DenyRequestAsync(request.RequestID);

            var updatedRequest = await _context.RoleChangeRequests.FindAsync(request.RequestID);
            Assert.That(updatedRequest, Is.Null);
        }
    }
}