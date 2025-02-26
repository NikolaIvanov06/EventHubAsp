using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventHubASP.Core;
using EventHubASP.DataAccess;
using EventHubASP.Models;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace EventHubASP.Tests.Tests
{
    [TestFixture]
    public class NewsServiceTests
    {
        private ApplicationDbContext _context;
        private Mock<INotificationService> _mockNotificationService;
        private NewsService _newsService;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;

            _context = new ApplicationDbContext(options);
            _mockNotificationService = new Mock<INotificationService>();

            _newsService = new NewsService(_context, _mockNotificationService.Object);
        }

        [TearDown]
        public void Teardown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public async Task CreateNewsAsync_ShouldAddNewsAndNotifySubscribers()
        {
            var news = new News
            {
                NewsID = 1,
                EventID = 1,
                Title = "Test News",
                Content = "This is a test news content."
            };

            var userId = Guid.NewGuid();
            var registration = new Registration
            {
                RegistrationID = 1,
                EventID = news.EventID,
                UserID = userId
            };

            await _context.Registrations.AddAsync(registration);
            await _context.SaveChangesAsync();

            await _newsService.CreateNewsAsync(news);

            var savedNews = await _context.News.FindAsync(news.NewsID);
            Assert.That(savedNews, Is.Not.Null);
            Assert.That(savedNews.Title, Is.EqualTo("Test News"));

            _mockNotificationService.Verify(x => x.CreateNotificationAsync(It.Is<Notification>(n =>
                n.UserID == userId &&
                n.NewsID == news.NewsID &&
                n.Message.Contains("New update for event 1: Test News"))), Times.Once);
        }

        [Test]
        public async Task GetNewsForUserAsync_ShouldReturnNewsForUser()
        {
            var userId = Guid.NewGuid();
            var eventId = 1;
            var registration = new Registration
            {
                RegistrationID = 1,
                EventID = eventId,
                UserID = userId
            };

            var news = new List<News>
            {
                new News { NewsID = 1, EventID = eventId, Title = "News 1", PublishedDate = DateTime.UtcNow , Content = "EXAMPLE"},
                new News { NewsID = 2, EventID = eventId, Title = "News 2", PublishedDate = DateTime.UtcNow.AddDays(-1) , Content = "EXAMPLE" }
            };

            await _context.Registrations.AddAsync(registration);
            await _context.News.AddRangeAsync(news);
            await _context.SaveChangesAsync();

            var result = await _newsService.GetNewsForUserAsync(userId);

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.Any(n => n.Title == "News 1"), Is.True);
            Assert.That(result.Any(n => n.Title == "News 2"), Is.True);
        }

        [Test]
        public async Task GetEventsByOrganizerAsync_ShouldReturnEventsForOrganizer()
        {
            var organizerId = Guid.NewGuid();
            var events = new List<Event>
    {
        new Event { EventID = 1, OrganizerID = organizerId, Title = "Event 1", Description = "Description 1", ImageUrl = "http://example.com/image1.jpg", Location = "Location 1" },
        new Event { EventID = 2, OrganizerID = organizerId, Title = "Event 2", Description = "Description 2", ImageUrl = "http://example.com/image2.jpg", Location = "Location 2" }
    };

            await _context.Events.AddRangeAsync(events);
            await _context.SaveChangesAsync();

            var result = await _newsService.GetEventsByOrganizerAsync(organizerId);

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.Any(e => e.Title == "Event 1"), Is.True);
            Assert.That(result.Any(e => e.Title == "Event 2"), Is.True);
        }
    }
}