using EventHubASP.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventHubASP.Core
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(Notification notification);
        Task<List<Notification>> GetNotificationsForUserAsync(Guid userId);
        Task MarkAsReadAsync(Guid userId);
        Task<List<User>> GetParticipantsByEventIdAsync(int eventId);
    }
}