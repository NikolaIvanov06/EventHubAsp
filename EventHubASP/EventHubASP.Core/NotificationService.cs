using EventHubASP.Core.Hubs;
using EventHubASP.Core;
using EventHubASP.DataAccess;
using EventHubASP.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<User>> GetParticipantsByEventIdAsync(int eventId)
    {
        return await _context.Registrations
            .Where(r => r.EventID == eventId)
            .Select(r => r.User)
            .ToListAsync();
    }

    public async Task CreateNotificationAsync(Notification notification)
    {
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }
    public async Task<IEnumerable<Notification>> GetNotificationsForUserAsync(Guid userId)
    {
        return await _context.Notifications
            .Where(n => n.UserID == userId)
            .ToListAsync();
    }

    public async Task<bool> MarkNotificationAsReadAsync(int notificationId, Guid userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.NotificationID == notificationId && n.UserID == userId);

        if (notification != null)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }
}

