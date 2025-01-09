using EventHubASP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHubASP.Core
{

    public interface IEventService
    {
        Task<IEnumerable<Event>> GetAllEventsAsync();
        Task<IEnumerable<Event>> GetEventsByOrganizerAsync(int organizerId);
        Task<IEnumerable<User>> GetEventParticipantsAsync(int eventId);
        Task<bool> SubscribeToEventAsync(int userId, int eventId);
        Task<bool> UnsubscribeFromEventAsync(int userId, int eventId);
        Task CreateEventAsync(Event model);
        Task<Event> GetEventByIdAsync(int eventId);
        Task<bool> UpdateEventAsync(Event updatedEvent, int organizerId);
        Task<bool> DeleteEventAsync(int eventId);
        Task<bool> IsOrganizerOfEventAsync(int organizerId, int eventId);
        Task<IEnumerable<Event>> GetUserSubscriptionsAsync(int userId);

    }


}
