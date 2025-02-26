using System.Threading.Tasks;
using EventHubASP.Core.Hubs;
using EventHubASP.Models;
using Microsoft.AspNetCore.SignalR;
using Moq;
using NUnit.Framework;

namespace EventHubASP.Tests.Tests
{
    [TestFixture]
    public class NotificationHubTests
    {
        private Mock<IHubCallerClients> _mockClients;
        private Mock<IClientProxy> _mockClientProxy;
        private NotificationHub _notificationHub;

        [SetUp]
        public void Setup()
        {
            _mockClients = new Mock<IHubCallerClients>();
            _mockClientProxy = new Mock<IClientProxy>();

            _mockClients.Setup(clients => clients.User(It.IsAny<string>())).Returns(_mockClientProxy.Object);
            _mockClients.Setup(clients => clients.All).Returns(_mockClientProxy.Object);

            _notificationHub = new NotificationHub
            {
                Clients = _mockClients.Object
            };
        }

        [TearDown]
        public void Teardown()
        {
            _notificationHub.Dispose();
        }

        [Test]
        public async Task SendNotification_ShouldSendNotificationToUser()
        {
            var userId = "testUser";
            var message = "Test Notification";

            await _notificationHub.SendNotification(userId, message);

            _mockClients.Verify(clients => clients.User(userId).SendCoreAsync("ReceiveNotification", It.Is<object[]>(o => o != null && o.Length == 1 && o[0] == message), default), Times.Once);
        }

        [Test]
        public async Task BroadcastNews_ShouldBroadcastNewsToAllClients()
        {
            var news = new News
            {
                NewsID = 1,
                EventID = 1,
                Title = "Test News",
                Content = "This is a test news content."
            };

            await _notificationHub.BroadcastNews(news);

            _mockClients.Verify(clients => clients.All.SendCoreAsync("ReceiveNews", It.Is<object[]>(o => o != null && o.Length == 1 && o[0] == news), default), Times.Once);
        }
    }
}