using EventHubASP.DataAccess;
using EventHubASP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHubASP.Core
{
    public interface INotificationService
    {
        Task<IEnumerable<User>> GetParticipantsByEventIdAsync(int eventId);
        Task CreateNotificationAsync(Notification notification);
        Task<IEnumerable<Notification>> GetNotificationsForUserAsync(Guid userId);
        Task<bool> MarkNotificationAsReadAsync(int notificationId, Guid userId);
    }


}
