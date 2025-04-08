using EventHubASP.Core;
using EventHubASP.DataAccess;
using EventHubASP.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class EventService : IEventService
{
    private readonly ApplicationDbContext _context;

    public EventService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Event>> GetAllEventsAsync()
    {
        return await _context.Events.ToListAsync();
    }

    public async Task<IEnumerable<Event>> GetEventsByOrganizerAsync(Guid organizerId)
    {
        return await _context.Events.Where(e => e.OrganizerID == organizerId).ToListAsync();
    }

    public async Task<bool> SubscribeToEventAsync(Guid userId, int eventId)
    {
        if (!await _context.Registrations.AnyAsync(r => r.UserID == userId && r.EventID == eventId))
        {
            _context.Registrations.Add(new Registration { UserID = userId, EventID = eventId });
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public async Task CreateEventAsync(Event model)
    {
        if (string.IsNullOrWhiteSpace(model.ImageUrl))
        {
            model.ImageUrl = "~/logo.jpg";
        }

        _context.Events.Add(model);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving event: {ex.Message}");
            throw;
        }
    }

    public async Task<Event> GetEventByIdAsync(int eventId)
    {
        var eventModel = await _context.Events
            .Include(e => e.CustomDetails)
            .FirstOrDefaultAsync(e => e.EventID == eventId);

        if (eventModel != null && eventModel.CustomDetails != null)
        {
            Console.WriteLine($"Retrieved EventID {eventId}, CustomContent: {eventModel.CustomDetails.CustomContent?.Substring(0, Math.Min(50, eventModel.CustomDetails.CustomContent?.Length ?? 0))}");
        }
        else
        {
            Console.WriteLine($"Retrieved EventID {eventId}, CustomDetails or CustomContent is null.");
        }

        return eventModel;
    }
    public async Task<bool> SaveCustomDetailsAsync(int eventId, string customContent, Guid organizerId)
    {
        try
        {
            if (string.IsNullOrEmpty(customContent))
            {
                Console.WriteLine("Custom content is empty or null.");
                throw new ArgumentException("Custom content cannot be empty.", nameof(customContent));
            }

            var existingEvent = await _context.Events
                .Include(e => e.CustomDetails)
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (existingEvent == null)
            {
                Console.WriteLine($"Event with ID {eventId} not found.");
                throw new KeyNotFoundException($"Event with ID {eventId} not found.");
            }

            if (existingEvent.OrganizerID != organizerId)
            {
                Console.WriteLine($"Unauthorized access attempt for EventID {eventId} by OrganizerID {organizerId}.");
                throw new UnauthorizedAccessException("You are not authorized to edit this event.");
            }

            if (existingEvent.CustomDetails == null)
            {
                Console.WriteLine($"Creating new CustomDetails for EventID {eventId}.");
                existingEvent.CustomDetails = new EventDetails
                {
                    EventID = eventId,
                    CustomContent = customContent
                };
                _context.EventDetails.Add(existingEvent.CustomDetails);
            }
            else
            {
                Console.WriteLine($"Updating CustomContent for EventID {eventId}: {customContent.Substring(0, Math.Min(50, customContent.Length))}");
                existingEvent.CustomDetails.CustomContent = customContent;
                _context.Entry(existingEvent.CustomDetails).State = EntityState.Modified;
            }

            var changes = await _context.SaveChangesAsync();
            Console.WriteLine($"Saved {changes} changes for EventID {eventId} with content: {customContent.Substring(0, Math.Min(50, customContent.Length))}");
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Console.WriteLine($"Concurrency conflict: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving custom details: {ex.Message}");
            return false;
        }
    }



    public async Task<bool> UpdateEventAsync(Event updatedEvent, Guid organizerId)
    {
        var existingEvent = await _context.Events.FindAsync(updatedEvent.EventID);

        if (existingEvent != null && existingEvent.OrganizerID == organizerId)
        {

            existingEvent.Title = updatedEvent.Title;
            existingEvent.Description = updatedEvent.Description;
            existingEvent.Date = updatedEvent.Date;
            existingEvent.Location = updatedEvent.Location;
            existingEvent.ImageUrl = updatedEvent.ImageUrl;

            _context.Events.Update(existingEvent);
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public async Task<IEnumerable<Event>> GetUserSubscriptionsAsync(Guid userId)
    {
        return await _context.Events
            .Where(e => e.Registrations.Any(r => r.UserID == userId))
            .ToListAsync();
    }

    public async Task<bool> UnsubscribeFromEventAsync(Guid userId, int eventId)
    {
        var registration = await _context.Registrations
            .FirstOrDefaultAsync(r => r.UserID == userId && r.EventID == eventId);



        if (registration != null)
        {
            _context.Registrations.Remove(registration);
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<bool> DeleteEventAsync(int eventId)
    {
        var eventToRemove = await _context.Events.FindAsync(eventId);
        if (eventToRemove != null)
        {
            _context.Events.Remove(eventToRemove);
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public async Task<bool> IsOrganizerOfEventAsync(Guid organizerId, int eventId)
    {
        return await _context.Events.AnyAsync(e => e.EventID == eventId && e.OrganizerID == organizerId);
    }

    public async Task<IEnumerable<User>> GetEventParticipantsAsync(int eventId)
    {
        return await _context.Registrations
            .Where(r => r.EventID == eventId)
            .Include(r => r.User.Registrations)
            .Select(r => r.User)
            .ToListAsync();
    }
}