using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventHubASP.Core;
using EventHubASP.DataAccess;
using EventHubASP.Models;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace EventHubASP.Tests.Tests
{
    [TestFixture]
    public class EventServiceTests
    {
        private ApplicationDbContext _context;
        private EventService _eventService;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;

            _context = new ApplicationDbContext(options);
            _eventService = new EventService(_context);
        }

        [TearDown]
        public void Teardown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public async Task GetAllEventsAsync_ShouldReturnAllEvents()
        {
            var events = new List<Event>
            {
                new Event { EventID = 1, Title = "Event 1", Description = "Description 1", ImageUrl = "http://example.com/image1.jpg", Location = "Location 1" },
                new Event { EventID = 2, Title = "Event 2", Description = "Description 2", ImageUrl = "http://example.com/image2.jpg", Location = "Location 2" }
            };

            await _context.Events.AddRangeAsync(events);
            await _context.SaveChangesAsync();

            var result = await _eventService.GetAllEventsAsync();

            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result.Any(e => e.Title == "Event 1"), Is.True);
            Assert.That(result.Any(e => e.Title == "Event 2"), Is.True);
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

            var result = await _eventService.GetEventsByOrganizerAsync(organizerId);

            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result.Any(e => e.Title == "Event 1"), Is.True);
            Assert.That(result.Any(e => e.Title == "Event 2"), Is.True);
        }

        [Test]
        public async Task SubscribeToEventAsync_ShouldAddRegistration()
        {
            var userId = Guid.NewGuid();
            var eventId = 1;

            var result = await _eventService.SubscribeToEventAsync(userId, eventId);

            Assert.That(result, Is.True);
            var registration = await _context.Registrations.FirstOrDefaultAsync(r => r.UserID == userId && r.EventID == eventId);
            Assert.That(registration, Is.Not.Null);
        }

        [Test]
        public async Task SubscribeToEventAsync_ShouldReturnFalse_WhenAlreadySubscribed()
        {
            var userId = Guid.NewGuid();
            var eventId = 1;
            var registration = new Registration { UserID = userId, EventID = eventId };

            await _context.Registrations.AddAsync(registration);
            await _context.SaveChangesAsync();

            var result = await _eventService.SubscribeToEventAsync(userId, eventId);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CreateEventAsync_ShouldAddEvent()
        {
            var eventModel = new Event
            {
                EventID = 1,
                Title = "New Event",
                Description = "New Event Description",
                Location = "New Event Location"
            };

            await _eventService.CreateEventAsync(eventModel);

            var savedEvent = await _context.Events.FindAsync(eventModel.EventID);
            Assert.That(savedEvent, Is.Not.Null);
            Assert.That(savedEvent.Title, Is.EqualTo("New Event"));
        }

        [Test]
        public async Task CreateEventAsync_ShouldSetDefaultImageUrl_WhenImageUrlIsNull()
        {
            var eventModel = new Event
            {
                EventID = 1,
                Title = "New Event",
                Description = "New Event Description",
                Location = "New Event Location",
                ImageUrl = null
            };

            await _eventService.CreateEventAsync(eventModel);

            var savedEvent = await _context.Events.FindAsync(eventModel.EventID);
            Assert.That(savedEvent, Is.Not.Null);
            Assert.That(savedEvent.ImageUrl, Is.EqualTo("~/logo.jpg"));
        }

        [Test]
        public async Task GetEventByIdAsync_ShouldReturnEvent()
        {
            var eventModel = new Event
            {
                EventID = 1,
                Title = "Event 1",
                Description = "Description 1",
                ImageUrl = "http://example.com/image1.jpg",
                Location = "Location 1"
            };

            await _context.Events.AddAsync(eventModel);
            await _context.SaveChangesAsync();

            var result = await _eventService.GetEventByIdAsync(eventModel.EventID);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Title, Is.EqualTo("Event 1"));
        }

        [Test]
        public async Task UpdateEventAsync_ShouldUpdateEvent()
        {
            var organizerId = Guid.NewGuid();
            var eventModel = new Event
            {
                EventID = 1,
                OrganizerID = organizerId,
                Title = "Event 1",
                Description = "Description 1",
                ImageUrl = "http://example.com/image1.jpg",
                Location = "Location 1"
            };

            await _context.Events.AddAsync(eventModel);
            await _context.SaveChangesAsync();

            var updatedEvent = new Event
            {
                EventID = 1,
                Title = "Updated Event",
                Description = "Updated Description",
                Date = DateTime.Now,
                Location = "Updated Location",
                ImageUrl = "http://example.com/updatedimage.jpg"
            };

            var result = await _eventService.UpdateEventAsync(updatedEvent, organizerId);

            Assert.That(result, Is.True);
            var savedEvent = await _context.Events.FindAsync(eventModel.EventID);
            Assert.That(savedEvent.Title, Is.EqualTo("Updated Event"));
        }

        [Test]
        public async Task UpdateEventAsync_ShouldReturnFalse_WhenEventNotFound()
        {
            var organizerId = Guid.NewGuid();
            var updatedEvent = new Event
            {
                EventID = 1,
                Title = "Updated Event",
                Description = "Updated Description",
                Date = DateTime.Now,
                Location = "Updated Location",
                ImageUrl = "http://example.com/updatedimage.jpg"
            };

            var result = await _eventService.UpdateEventAsync(updatedEvent, organizerId);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task UpdateEventAsync_ShouldReturnFalse_WhenOrganizerMismatch()
        {
            var organizerId = Guid.NewGuid();
            var anotherOrganizerId = Guid.NewGuid();
            var eventModel = new Event
            {
                EventID = 1,
                OrganizerID = anotherOrganizerId,
                Title = "Event 1",
                Description = "Description 1",
                ImageUrl = "http://example.com/image1.jpg",
                Location = "Location 1"
            };

            await _context.Events.AddAsync(eventModel);
            await _context.SaveChangesAsync();

            var updatedEvent = new Event
            {
                EventID = 1,
                Title = "Updated Event",
                Description = "Updated Description",
                Date = DateTime.Now,
                Location = "Updated Location",
                ImageUrl = "http://example.com/updatedimage.jpg"
            };

            var result = await _eventService.UpdateEventAsync(updatedEvent, organizerId);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task GetUserSubscriptionsAsync_ShouldReturnUserSubscriptions()
        {
            var userId = Guid.NewGuid();
            var eventId = 1;
            var registration = new Registration
            {
                RegistrationID = 1,
                EventID = eventId,
                UserID = userId
            };

            var eventModel = new Event
            {
                EventID = eventId,
                Title = "Event 1",
                Description = "Description 1",
                ImageUrl = "http://example.com/image1.jpg",
                Location = "Location 1"
            };

            await _context.Registrations.AddAsync(registration);
            await _context.Events.AddAsync(eventModel);
            await _context.SaveChangesAsync();

            var result = await _eventService.GetUserSubscriptionsAsync(userId);

            Assert.That(result.Count(), Is.EqualTo(1));
            Assert.That(result.Any(e => e.Title == "Event 1"), Is.True);
        }
        [Test]
        public async Task UnsubscribeFromEventAsync_ShouldRemoveRegistration()
        {
            var userId = Guid.NewGuid();
            var eventId = 1;
            var registration = new Registration
            {
                RegistrationID = 1,
                EventID = eventId,
                UserID = userId
            };

            await _context.Registrations.AddAsync(registration);
            await _context.SaveChangesAsync();

            var result = await _eventService.UnsubscribeFromEventAsync(userId, eventId);

            Assert.That(result, Is.True);
            var removedRegistration = await _context.Registrations.FirstOrDefaultAsync(r => r.UserID == userId && r.EventID == eventId);
            Assert.That(removedRegistration, Is.Null);
        }

        [Test]
        public async Task DeleteEventAsync_ShouldRemoveEvent()
        {
            var eventModel = new Event
            {
                EventID = 1,
                Title = "Event 1",
                Description = "Description 1",
                ImageUrl = "http://example.com/image1.jpg",
                Location = "Location 1"
            };

            await _context.Events.AddAsync(eventModel);
            await _context.SaveChangesAsync();

            var result = await _eventService.DeleteEventAsync(eventModel.EventID);

            Assert.That(result, Is.True);
            var deletedEvent = await _context.Events.FindAsync(eventModel.EventID);
            Assert.That(deletedEvent, Is.Null);
        }

        [Test]
        public async Task DeleteEventAsync_ShouldReturnFalse_WhenEventNotFound()
        {
            var result = await _eventService.DeleteEventAsync(1);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task IsOrganizerOfEventAsync_ShouldReturnTrueIfOrganizer()
        {
            var organizerId = Guid.NewGuid();
            var eventModel = new Event
            {
                EventID = 1,
                OrganizerID = organizerId,
                Title = "Event 1",
                Description = "Description 1",
                ImageUrl = "http://example.com/image1.jpg",
                Location = "Location 1"
            };

            await _context.Events.AddAsync(eventModel);
            await _context.SaveChangesAsync();

            var result = await _eventService.IsOrganizerOfEventAsync(organizerId, eventModel.EventID);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task IsOrganizerOfEventAsync_ShouldReturnFalse_WhenNotOrganizer()
        {
            var organizerId = Guid.NewGuid();
            var anotherOrganizerId = Guid.NewGuid();
            var eventModel = new Event
            {
                EventID = 1,
                OrganizerID = anotherOrganizerId,
                Title = "Event 1",
                Description = "Description 1",
                ImageUrl = "http://example.com/image1.jpg",
                Location = "Location 1"
            };

            await _context.Events.AddAsync(eventModel);
            await _context.SaveChangesAsync();

            var result = await _eventService.IsOrganizerOfEventAsync(organizerId, eventModel.EventID);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task GetEventParticipantsAsync_ShouldReturnParticipants()
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

            var result = await _eventService.GetEventParticipantsAsync(eventId);

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.Any(u => u.UserName == "User1"), Is.True);
            Assert.That(result.Any(u => u.UserName == "User2"), Is.True);
        }
    }
}