using EventHubASP.Core;
using EventHubASP.DataAccess;
using EventHubASP.Models;
using Microsoft.EntityFrameworkCore;

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

    public async Task<IEnumerable<Event>> GetEventsByOrganizerAsync(int organizerId)
    {
        return await _context.Events.Where(e => e.OrganizerID == organizerId).ToListAsync();
    }

    public async Task<IEnumerable<User>> GetEventParticipantsAsync(int eventId)
    {
        return await _context.Registrations
            .Where(r => r.EventID == eventId)
            .Select(r => r.User)
            .ToListAsync();
    }

    public async Task<bool> SubscribeToEventAsync(int userId, int eventId)
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
        return await _context.Events.FindAsync(eventId);
    }

    public async Task<bool> UpdateEventAsync(Event updatedEvent, int organizerId)
    {
        var existingEvent = await _context.Events.FindAsync(updatedEvent.EventID);

        if (existingEvent != null && existingEvent.OrganizerID == organizerId)
        {
            // Update the existing event with new values
            existingEvent.Title = updatedEvent.Title;
            existingEvent.Description = updatedEvent.Description;
            existingEvent.Date = updatedEvent.Date;
            existingEvent.Location = updatedEvent.Location;
            existingEvent.ImageUrl = updatedEvent.ImageUrl;

            // Save changes
            _context.Events.Update(existingEvent);
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public async Task<IEnumerable<Event>> GetUserSubscriptionsAsync(int userId)
    {
        return await _context.Events
            .Where(e => e.Registrations.Any(r => r.UserID == userId))
            .ToListAsync();
    }

    public async Task<bool> UnsubscribeFromEventAsync(int userId, int eventId)
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


    public async Task<bool> IsOrganizerOfEventAsync(int organizerId, int eventId)
    {
        return await _context.Events.AnyAsync(e => e.EventID == eventId && e.OrganizerID == organizerId);
    }
}
