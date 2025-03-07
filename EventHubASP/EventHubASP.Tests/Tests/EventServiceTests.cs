using EventHubASP.Core;
using EventHubASP.DataAccess;
using EventHubASP.Models;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventHubASP.Tests.Tests
{
    [TestFixture]
    public class EventServiceTests
    {
        private ApplicationDbContext _context;
        private EventService _eventService;
        private List<Event> _events;
        private List<Registration> _registrations;
        private List<EventDetails> _eventDetails;
        private List<User> _users;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            var organizerId1 = Guid.NewGuid();
            var organizerId2 = Guid.NewGuid();
            var userId1 = Guid.NewGuid();
            var userId2 = Guid.NewGuid();

            _events = new List<Event>
            {
                new Event { EventID = 1, Title = "Test Event 1", Description = "Description 1", Date = DateTime.Now.AddDays(7), Location = "Location 1", OrganizerID = organizerId1, ImageUrl = "~/image1.jpg" },
                new Event { EventID = 2, Title = "Test Event 2", Description = "Description 2", Date = DateTime.Now.AddDays(14), Location = "Location 2", OrganizerID = organizerId1, ImageUrl = "~/image2.jpg" },
                new Event { EventID = 3, Title = "Test Event 3", Description = "Description 3", Date = DateTime.Now.AddDays(21), Location = "Location 3", OrganizerID = organizerId2, ImageUrl = "~/image3.jpg" }
            };

            _users = new List<User>
            {
                new User { Id = userId1, UserName = "user1", Email = "user1@example.com" },
                new User { Id = userId2, UserName = "user2", Email = "user2@example.com" }
            };

            _registrations = new List<Registration>
            {
                new Registration { RegistrationID = 1, UserID = userId1, EventID = 1, User = _users[0] },
                new Registration { RegistrationID = 2, UserID = userId1, EventID = 2, User = _users[0] },
                new Registration { RegistrationID = 3, UserID = userId2, EventID = 2, User = _users[1] }
            };

            _eventDetails = new List<EventDetails>
            {
                new EventDetails { Id = 1, EventID = 1, CustomContent = "Custom content for event 1" }
            };

            _events[0].Registrations = new List<Registration> { _registrations[0] };
            _events[1].Registrations = new List<Registration> { _registrations[1], _registrations[2] };
            _events[0].CustomDetails = _eventDetails[0];

            _users[0].Registrations = new List<Registration> { _registrations[0], _registrations[1] };
            _users[1].Registrations = new List<Registration> { _registrations[2] };

            _context.Events.AddRange(_events);
            _context.Users.AddRange(_users);
            _context.Registrations.AddRange(_registrations);
            _context.EventDetails.AddRange(_eventDetails);
            _context.SaveChanges();

            _eventService = new EventService(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public async Task GetAllEventsAsync_ShouldReturnAllEvents()
        {
            var result = await _eventService.GetAllEventsAsync();
            Assert.That(result.Count(), Is.EqualTo(3));
            Assert.That(result.Select(e => e.Title), Is.EquivalentTo(new[] { "Test Event 1", "Test Event 2", "Test Event 3" }));
        }

        [Test]
        public async Task GetEventsByOrganizerAsync_ShouldReturnEventsForSpecificOrganizer()
        {
            var organizerId = _events[0].OrganizerID;
            var result = await _eventService.GetEventsByOrganizerAsync(organizerId);
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result.Select(e => e.Title), Is.EquivalentTo(new[] { "Test Event 1", "Test Event 2" }));
        }

        [Test]
        public async Task SubscribeToEventAsync_ShouldReturnTrue_WhenSubscriptionIsNew()
        {
            var userId = Guid.NewGuid();
            var eventId = 3;
            var result = await _eventService.SubscribeToEventAsync(userId, eventId);
            Assert.That(result, Is.True);
            Assert.That(await _context.Registrations.AnyAsync(r => r.UserID == userId && r.EventID == eventId), Is.True);
        }

        [Test]
        public async Task SubscribeToEventAsync_ShouldReturnFalse_WhenSubscriptionExists()
        {
            var userId = _registrations[0].UserID;
            var eventId = _registrations[0].EventID;
            var result = await _eventService.SubscribeToEventAsync(userId, eventId);
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CreateEventAsync_ShouldAddEventToDatabase()
        {
            var newEvent = new Event
            {
                EventID = 4,
                Title = "New Test Event",
                Description = "New Description",
                Date = DateTime.Now.AddDays(30),
                Location = "New Location",
                OrganizerID = Guid.NewGuid()
            };
            await _eventService.CreateEventAsync(newEvent);
            var savedEvent = await _context.Events.FindAsync(4);
            Assert.That(savedEvent, Is.Not.Null);
            Assert.That(savedEvent.Title, Is.EqualTo("New Test Event"));
            Assert.That(savedEvent.ImageUrl, Is.EqualTo("~/logo.jpg"));
        }

        [Test]
        public async Task CreateEventAsync_ShouldUseProvidedImageUrl_WhenImageUrlIsNotEmpty()
        {
            var newEvent = new Event
            {
                EventID = 5,
                Title = "Event With Image",
                Description = "Description",
                Date = DateTime.Now.AddDays(30),
                Location = "Location",
                OrganizerID = Guid.NewGuid(),
                ImageUrl = "~/custom-image.jpg"
            };
            await _eventService.CreateEventAsync(newEvent);
            var savedEvent = await _context.Events.FindAsync(5);
            Assert.That(savedEvent, Is.Not.Null);
            Assert.That(savedEvent.ImageUrl, Is.EqualTo("~/custom-image.jpg"));
        }



        [Test]
        public async Task GetEventByIdAsync_ShouldReturnEvent_WhenEventExists()
        {
            var result = await _eventService.GetEventByIdAsync(1);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Title, Is.EqualTo("Test Event 1"));
            Assert.That(result.CustomDetails, Is.Not.Null);
            Assert.That(result.CustomDetails.CustomContent, Is.EqualTo("Custom content for event 1"));
        }

        [Test]
        public async Task GetEventByIdAsync_ShouldReturnNull_WhenEventDoesNotExist()
        {
            var result = await _eventService.GetEventByIdAsync(999);
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task SaveCustomDetailsAsync_ShouldCreateNewEventDetails_WhenDetailsDoNotExist()
        {
            var eventId = 2;
            var customContent = "New custom content";
            var organizerId = _events[1].OrganizerID;
            var result = await _eventService.SaveCustomDetailsAsync(eventId, customContent, organizerId);
            Assert.That(result, Is.True);
            var updatedEvent = await _context.Events.Include(e => e.CustomDetails).FirstOrDefaultAsync(e => e.EventID == eventId);
            Assert.That(updatedEvent.CustomDetails, Is.Not.Null);
            Assert.That(updatedEvent.CustomDetails.CustomContent, Is.EqualTo(customContent));
        }

        [Test]
        public async Task SaveCustomDetailsAsync_ShouldUpdateExistingEventDetails_WhenDetailsExist()
        {
            var eventId = 1;
            var customContent = "Updated custom content";
            var organizerId = _events[0].OrganizerID;
            var result = await _eventService.SaveCustomDetailsAsync(eventId, customContent, organizerId);
            Assert.That(result, Is.True);
            var updatedEvent = await _context.Events.Include(e => e.CustomDetails).FirstOrDefaultAsync(e => e.EventID == eventId);
            Assert.That(updatedEvent.CustomDetails, Is.Not.Null);
            Assert.That(updatedEvent.CustomDetails.CustomContent, Is.EqualTo(customContent));
        }



        [Test]
        public async Task UpdateEventAsync_ShouldReturnTrue_WhenUpdateIsSuccessful()
        {
            var eventToUpdate = new Event
            {
                EventID = 1,
                Title = "Updated Title",
                Description = "Updated Description",
                Date = DateTime.Now.AddDays(10),
                Location = "Updated Location",
                ImageUrl = "~/updated-image.jpg"
            };
            var organizerId = _events[0].OrganizerID;
            var result = await _eventService.UpdateEventAsync(eventToUpdate, organizerId);
            Assert.That(result, Is.True);
            var updatedEvent = await _context.Events.FindAsync(1);
            Assert.That(updatedEvent.Title, Is.EqualTo("Updated Title"));
            Assert.That(updatedEvent.Description, Is.EqualTo("Updated Description"));
            Assert.That(updatedEvent.Location, Is.EqualTo("Updated Location"));
            Assert.That(updatedEvent.ImageUrl, Is.EqualTo("~/updated-image.jpg"));
        }

        [Test]
        public async Task UpdateEventAsync_ShouldReturnFalse_WhenEventDoesNotExist()
        {
            var eventToUpdate = new Event { EventID = 999, Title = "Updated Title" };
            var organizerId = Guid.NewGuid();
            var result = await _eventService.UpdateEventAsync(eventToUpdate, organizerId);
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task UpdateEventAsync_ShouldReturnFalse_WhenUserIsNotOrganizer()
        {
            var eventToUpdate = new Event { EventID = 1, Title = "Updated Title" };
            var wrongOrganizerId = Guid.NewGuid();
            var result = await _eventService.UpdateEventAsync(eventToUpdate, wrongOrganizerId);
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task GetUserSubscriptionsAsync_ShouldReturnEventsUserIsSubscribedTo()
        {
            var userId = _registrations[0].UserID;
            var result = await _eventService.GetUserSubscriptionsAsync(userId);
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result.Select(e => e.EventID), Is.EquivalentTo(new[] { 1, 2 }));
        }

        [Test]
        public async Task UnsubscribeFromEventAsync_ShouldReturnTrue_WhenUnsubscribeIsSuccessful()
        {
            var userId = _registrations[0].UserID;
            var eventId = _registrations[0].EventID;
            var result = await _eventService.UnsubscribeFromEventAsync(userId, eventId);
            Assert.That(result, Is.True);
            Assert.That(await _context.Registrations.AnyAsync(r => r.UserID == userId && r.EventID == eventId), Is.False);
        }

        [Test]
        public async Task UnsubscribeFromEventAsync_ShouldReturnFalse_WhenRegistrationDoesNotExist()
        {
            var userId = Guid.NewGuid();
            var eventId = 1;
            var result = await _eventService.UnsubscribeFromEventAsync(userId, eventId);
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task DeleteEventAsync_ShouldReturnTrue_WhenDeleteIsSuccessful()
        {
            var eventId = 3;
            var result = await _eventService.DeleteEventAsync(eventId);
            Assert.That(result, Is.True);
            Assert.That(await _context.Events.AnyAsync(e => e.EventID == eventId), Is.False);
        }

        [Test]
        public async Task DeleteEventAsync_ShouldReturnFalse_WhenEventDoesNotExist()
        {
            var eventId = 999;
            var result = await _eventService.DeleteEventAsync(eventId);
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task IsOrganizerOfEventAsync_ShouldReturnTrue_WhenUserIsOrganizer()
        {
            var organizerId = _events[0].OrganizerID;
            var eventId = _events[0].EventID;
            var result = await _eventService.IsOrganizerOfEventAsync(organizerId, eventId);
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task IsOrganizerOfEventAsync_ShouldReturnFalse_WhenUserIsNotOrganizer()
        {
            var wrongOrganizerId = Guid.NewGuid();
            var eventId = _events[0].EventID;
            var result = await _eventService.IsOrganizerOfEventAsync(wrongOrganizerId, eventId);
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task GetEventParticipantsAsync_ShouldReturnUsersRegisteredForEvent()
        {
            var eventId = 2;
            var result = await _eventService.GetEventParticipantsAsync(eventId);
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result.Select(u => u.UserName), Is.EquivalentTo(new[] { "user1", "user2" }));
        }

        [Test]
        public async Task GetEventParticipantsAsync_ShouldReturnEmptyList_WhenNoParticipants()
        {
            var eventId = 3;
            var result = await _eventService.GetEventParticipantsAsync(eventId);
            Assert.That(result.Count(), Is.EqualTo(0));
        }
    }
}