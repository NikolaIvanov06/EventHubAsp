using EventHubASP.Core.Hubs;
using EventHubASP.DataAccess;
using EventHubASP.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventHubASP.Core
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(ApplicationDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task CreateNotificationAsync(Notification notification)
        {
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.User(notification.UserID.ToString())
                .SendAsync("ReceiveNotification", notification.Message);
        }

        public async Task<List<Notification>> GetNotificationsForUserAsync(Guid userId)
        {
            return await _context.Notifications
                .Where(n => n.UserID == userId && !n.IsRead)
                .OrderByDescending(n => n.Date)
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(Guid userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserID == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<User>> GetParticipantsByEventIdAsync(int eventId)
        {
            return await _context.Registrations
                .Where(r => r.EventID == eventId)
                .Select(r => r.User)
                .ToListAsync();
        }
    }
}