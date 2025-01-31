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
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

     //  public async Task<List<Notification>> GetNotificationsForUserAsync(int userId)
     //  {
     //      return await _context.Notifications
     //          .Where(n => n.UserID == userId && !n.IsRead)
     //          .OrderByDescending(n => n.Date)
     //          .ToListAsync();
     //  }
     //
        public async Task CreateNotificationsForEventAsync(int eventId,  string content)
        {
            var subscribers = await _context.Registrations
                .Where(r => r.EventID == eventId)
                .Select(r => r.UserID)
                .ToListAsync();

            foreach (var userId in subscribers)
            {
                var notification = new Notification
                {
                    UserID = userId,
                    Message = content,
                    Date = DateTime.UtcNow,
                    IsRead = false
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
        }



        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = _context.Notifications
                .Where(n => notificationId == n.NotificationID ).First();


                notification.IsRead = true;
            

            await _context.SaveChangesAsync();
        }


        public async Task CreateNotificationAsync(Notification notification)
        {
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }
    }

}
