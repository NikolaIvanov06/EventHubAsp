using EventHubASP.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventHubASP.Core
{
    public interface IEventService
    {
        Task<IEnumerable<Event>> GetAllEventsAsync();
        Task<IEnumerable<Event>> GetEventsByOrganizerAsync(Guid organizerId);
        Task<IEnumerable<User>> GetEventParticipantsAsync(int eventId);
        Task<bool> SubscribeToEventAsync(Guid userId, int eventId);
        Task<bool> UnsubscribeFromEventAsync(Guid userId, int eventId);
        Task CreateEventAsync(Event model);
        Task<Event> GetEventByIdAsync(int eventId);
        Task<bool> UpdateEventAsync(Event updatedEvent, Guid organizerId);
        Task<bool> DeleteEventAsync(int eventId);
        Task<bool> IsOrganizerOfEventAsync(Guid organizerId, int eventId);
        Task<IEnumerable<Event>> GetUserSubscriptionsAsync(Guid userId);
        Task<bool> SaveCustomDetailsAsync(int eventId, string customContent, Guid organizerId);
    }
}