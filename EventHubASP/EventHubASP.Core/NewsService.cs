using EventHubASP.DataAccess;
using EventHubASP.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHubASP.Core
{
    public class NewsService : INewsService
    {
        private readonly ApplicationDbContext _context;

        public NewsService(ApplicationDbContext context)
        {
            _context = context;
        }

   //    public async Task<List<News>> GetNewsForUserAsync(int userId)
   //    {
   //        var subscribedEventIds = await _context.Registrations
   //            .Where(r => r.UserID == userId)
   //            .Select(r => r.EventID)
   //            .ToListAsync();
   //
   //        return await _context.News
   //            .Where(n => subscribedEventIds.Contains(n.EventID))
   //            .OrderByDescending(n => n.PublishedDate)
   //            .ToListAsync();
   //    }
   //
        public async Task CreateNewsAsync(News news)
        {
            _context.News.Add(news);
            await _context.SaveChangesAsync();

            var subscribers = await _context.Registrations
                .Where(r => r.EventID == news.EventID)
                .Select(r => r.UserID)
                .ToListAsync();

            foreach (var userId in subscribers)
            {
                var notification = new Notification
                {
                    UserID = userId,
                    Message = $"News '{news.Title}' has been published for an event you're subscribed to.",
                    Date = DateTime.UtcNow,
                    IsRead = false
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
        }

    }

}
