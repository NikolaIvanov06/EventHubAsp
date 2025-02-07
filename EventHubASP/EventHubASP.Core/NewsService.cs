using EventHubASP.DataAccess;
using EventHubASP.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventHubASP.Core
{
    public class NewsService : INewsService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public NewsService(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<News> CreateNewsAsync(News news)
        {
            news.PublishedDate = DateTime.UtcNow;
            _context.News.Add(news);
            await _context.SaveChangesAsync();

            var subscribers = await _context.Registrations
                .Where(r => r.EventID == news.EventID)
                .Select(r => r.UserID)
                .ToListAsync();

            foreach (var userId in subscribers)
            {
                await _notificationService.CreateNotificationAsync(new Notification
                {
                    UserID = userId,
                    NewsID = news.NewsID,
                    Message = $"New update for event {news.EventID}: {news.Title}",
                    IsRead = false,
                    Date = DateTime.UtcNow
                });
            }

            return news;
        }

        public async Task<List<News>> GetNewsForUserAsync(Guid userId)
        {
            var eventIds = await _context.Registrations
                .Where(r => r.UserID == userId)
                .Select(r => r.EventID)
                .ToListAsync();

            return await _context.News
                .Where(n => eventIds.Contains(n.EventID))
                .OrderByDescending(n => n.PublishedDate)
                .ToListAsync();
        }

        public async Task<List<Event>> GetEventsByOrganizerAsync(Guid organizerId)
        {
            return await _context.Events
                .Where(e => e.OrganizerID == organizerId)
                .ToListAsync();
        }
    }
}
