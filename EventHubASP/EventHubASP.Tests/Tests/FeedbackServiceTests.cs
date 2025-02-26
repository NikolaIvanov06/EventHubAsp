using EventHubASP.Core;
using EventHubASP.DataAccess;
using EventHubASP.Models;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventHubASP.Tests.Tests
{
    [TestFixture]
    public class FeedbackServiceTests
    {
        private ApplicationDbContext _context;
        private FeedbackService _feedbackService;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test
                .Options;

            _context = new ApplicationDbContext(options);
            _feedbackService = new FeedbackService(_context);
        }

        [Test]
        public async Task SubmitFeedbackAsync_ShouldAddFeedbackToDatabase()
        {
            // Arrange
            var feedback = new Feedback
            {
                Id = 1,
                Name = "John Doe",
                Email = "john@example.com",
                Message = "Great service!",
                SubmittedDate = DateTime.Now
            };

            // Act
            await _feedbackService.SubmitFeedbackAsync(feedback);

            // Assert
            var result = await _context.Feedbacks.FirstOrDefaultAsync(f => f.Id == feedback.Id);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("John Doe"));
            Assert.That(result.Email, Is.EqualTo("john@example.com"));
            Assert.That(result.Message, Is.EqualTo("Great service!"));
            Assert.That(result.SubmittedDate, Is.EqualTo(feedback.SubmittedDate));
        }

        [Test]
        public async Task SubmitFeedbackAsync_ShouldPersistMultipleFeedbacks()
        {
            // Arrange
            var feedback1 = new Feedback
            {
                Id = 1,
                Name = "John Doe",
                Email = "john@example.com",
                Message = "Great service!",
                SubmittedDate = DateTime.Now
            };
            var feedback2 = new Feedback
            {
                Id = 2,
                Name = "Jane Smith",
                Email = "jane@example.com",
                Message = "Needs improvement",
                SubmittedDate = DateTime.Now.AddHours(1)
            };

            // Act
            await _feedbackService.SubmitFeedbackAsync(feedback1);
            await _feedbackService.SubmitFeedbackAsync(feedback2);

            // Assert
            var feedbacks = await _context.Feedbacks.ToListAsync();
            Assert.That(feedbacks, Has.Count.EqualTo(2));
            Assert.That(feedbacks.Any(f => f.Id == 1 && f.Name == "John Doe"), Is.True);
            Assert.That(feedbacks.Any(f => f.Id == 2 && f.Name == "Jane Smith"), Is.True);
        }

        [Test]
        public async Task GetAllFeedbackAsync_ShouldReturnEmptyList_WhenNoFeedbackExists()
        {
            // Act
            var result = await _feedbackService.GetAllFeedbackAsync();

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetAllFeedbackAsync_ShouldReturnAllFeedbacksInDescendingOrderBySubmittedDate()
        {
            // Arrange
            var feedback1 = new Feedback
            {
                Id = 1,
                Name = "John Doe",
                Email = "john@example.com",
                Message = "Great service!",
                SubmittedDate = DateTime.Now.AddDays(-1)
            };
            var feedback2 = new Feedback
            {
                Id = 2,
                Name = "Jane Smith",
                Email = "jane@example.com",
                Message = "Needs improvement",
                SubmittedDate = DateTime.Now
            };
            var feedback3 = new Feedback
            {
                Id = 3,
                Name = "Bob Johnson",
                Email = "bob@example.com",
                Message = "Awesome!",
                SubmittedDate = DateTime.Now.AddHours(-12)
            };

            await _context.Feedbacks.AddRangeAsync(feedback1, feedback2, feedback3);
            await _context.SaveChangesAsync();

            // Act
            var result = await _feedbackService.GetAllFeedbackAsync();

            // Assert
            var feedbackList = result.ToList();
            Assert.That(feedbackList, Has.Count.EqualTo(3));
            Assert.That(feedbackList[0].Id, Is.EqualTo(2)); // Latest: Jane
            Assert.That(feedbackList[1].Id, Is.EqualTo(3)); // Middle: Bob
            Assert.That(feedbackList[2].Id, Is.EqualTo(1)); // Oldest: John
            Assert.That(feedbackList[0].SubmittedDate, Is.GreaterThan(feedbackList[1].SubmittedDate));
            Assert.That(feedbackList[1].SubmittedDate, Is.GreaterThan(feedbackList[2].SubmittedDate));
        }

        [Test]
        public async Task GetAllFeedbackAsync_ShouldReturnSingleFeedback_WhenOnlyOneExists()
        {
            // Arrange
            var feedback = new Feedback
            {
                Id = 1,
                Name = "John Doe",
                Email = "john@example.com",
                Message = "Great service!",
                SubmittedDate = DateTime.Now
            };

            await _context.Feedbacks.AddAsync(feedback);
            await _context.SaveChangesAsync();

            // Act
            var result = await _feedbackService.GetAllFeedbackAsync();

            // Assert
            var feedbackList = result.ToList();
            Assert.That(feedbackList, Has.Count.EqualTo(1));
            Assert.That(feedbackList[0].Id, Is.EqualTo(1));
            Assert.That(feedbackList[0].Name, Is.EqualTo("John Doe"));
        }

        [TearDown]
        public void Teardown()
        {
            _context.Dispose();
        }
    }
}