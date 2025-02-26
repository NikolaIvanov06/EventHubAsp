using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventHubASP.Core;
using EventHubASP.Core.Hubs;
using EventHubASP.DataAccess;
using EventHubASP.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace EventHubASP.Tests.Tests
{
    [TestFixture]
    public class NotificationServiceTests
    {
        private ApplicationDbContext _context;
        private Mock<IHubContext<NotificationHub>> _mockHubContext;
        private Mock<IHubClients> _mockClients;
        private Mock<IClientProxy> _mockClientProxy;
        private NotificationService _notificationService;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;

            _context = new ApplicationDbContext(options);

            _mockHubContext = new Mock<IHubContext<NotificationHub>>();
            _mockClients = new Mock<IHubClients>();
            _mockClientProxy = new Mock<IClientProxy>();

            _mockHubContext.Setup(x => x.Clients).Returns(_mockClients.Object);
            _mockClients.Setup(x => x.User(It.IsAny<string>())).Returns(_mockClientProxy.Object);

            _notificationService = new NotificationService(_context, _mockHubContext.Object);
        }

        [TearDown]
        public void Teardown()
        {
            _context.Dispose();
        }

        [Test]
        public async Task CreateNotificationAsync_ShouldAddNotificationAndSendToUser()
        {
            var notification = new Notification
            {
                NotificationID = 1,
                UserID = Guid.NewGuid(),
                Message = "Test Notification",
                Date = DateTime.Now,
                IsRead = false
            };

            await _notificationService.CreateNotificationAsync(notification);

            var savedNotification = await _context.Notifications.FindAsync(notification.NotificationID);
            Assert.That(savedNotification, Is.Not.Null);
            Assert.That(savedNotification.Message, Is.EqualTo("Test Notification"));

            _mockClients.Verify(x => x.User(notification.UserID.ToString()), Times.Once);
            _mockClientProxy.Verify(x => x.SendCoreAsync("ReceiveNotification", It.Is<object[]>(y => y.Contains("Test Notification")), default), Times.Once);
        }

        [Test]
        public async Task GetNotificationsForUserAsync_ShouldReturnUnreadNotifications()
        {
            var userId = Guid.NewGuid();
            var notifications = new List<Notification>
            {
                new Notification { NotificationID = 2, UserID = userId, Message = "Unread Notification 1", Date = DateTime.Now, IsRead = false },
                new Notification { NotificationID = 3, UserID = userId, Message = "Unread Notification 2", Date = DateTime.Now, IsRead = false },
                new Notification { NotificationID = 4, UserID = userId, Message = "Read Notification", Date = DateTime.Now, IsRead = true }
            };

            await _context.Notifications.AddRangeAsync(notifications);
            await _context.SaveChangesAsync();

            var result = await _notificationService.GetNotificationsForUserAsync(userId);

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.Any(n => n.Message == "Read Notification"), Is.False);
        }

        [Test]
        public async Task MarkAsReadAsync_ShouldMarkNotificationsAsRead()
        {
            var userId = Guid.NewGuid();
            var notifications = new List<Notification>
            {
                new Notification { NotificationID = 5, UserID = userId, Message = "Unread Notification 1", Date = DateTime.Now, IsRead = false },
                new Notification { NotificationID = 6, UserID = userId, Message = "Unread Notification 2", Date = DateTime.Now, IsRead = false }
            };

            await _context.Notifications.AddRangeAsync(notifications);
            await _context.SaveChangesAsync();

            await _notificationService.MarkAsReadAsync(userId);

            var updatedNotifications = await _context.Notifications.Where(n => n.UserID == userId).ToListAsync();
            Assert.That(updatedNotifications.All(n => n.IsRead), Is.True);
        }

        [Test]
        public async Task GetParticipantsByEventIdAsync_ShouldReturnParticipants()
        {
            var eventId = 1;
            var users = new List<User>
            {
                new User { Id = Guid.NewGuid(), UserName = "User1", Email = "user1@example.com" },
                new User { Id = Guid.NewGuid(), UserName = "User2", Email = "user2@example.com" }
            };
            var registrations = new List<Registration>
            {
                new Registration { RegistrationID = 1, EventID = eventId, User = users[0] },
                new Registration { RegistrationID = 2, EventID = eventId, User = users[1] }
            };

            await _context.Users.AddRangeAsync(users);
            await _context.Registrations.AddRangeAsync(registrations);
            await _context.SaveChangesAsync();

            var result = await _notificationService.GetParticipantsByEventIdAsync(eventId);

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.Any(u => u.UserName == "User1"), Is.True);
            Assert.That(result.Any(u => u.UserName == "User2"), Is.True);
        }
    }
}