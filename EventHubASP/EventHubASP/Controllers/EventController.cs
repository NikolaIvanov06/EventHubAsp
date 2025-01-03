using EventHubASP.Core;
using EventHubASP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHubASP.Controllers
{
    public class EventController : Controller
    {
        private readonly IEventService _eventService;

        public EventController(IEventService eventService)
        {
            _eventService = eventService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> BrowseEvents()
        {
            var events = await _eventService.GetAllEventsAsync();
            return View(events);
        }

        // User: Subscribe to an event
        [Authorize(Roles = "User")]
        [HttpPost]
        public async Task<IActionResult> Subscribe(int eventId)
        {
            var userId = int.Parse(User.FindFirst("UserID").Value);
            if (await _eventService.SubscribeToEventAsync(userId, eventId))
            {
                TempData["Message"] = "Successfully subscribed to the event.";
            }
            else
            {
                TempData["Error"] = "Unable to subscribe. You may already be registered.";
            }

            return RedirectToAction("BrowseEvents");
        }

        // Organizer: View events created by the logged-in organizer
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> MyEvents()
        {
            var organizerId = int.Parse(User.FindFirst("UserID").Value);
            var events = await _eventService.GetEventsByOrganizerAsync(organizerId);
            return View(events);
        }


        [HttpPost]
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> CreateEvent(EventViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Validation Error: {error.ErrorMessage}");
                }
                return View(viewModel);
            }

            // Map ViewModel to Event Model
            var newEvent = new Event
            {
                Title = viewModel.Title,
                Description = viewModel.Description,
                Date = viewModel.Date,
                Location = viewModel.Location,
                ImageUrl = string.IsNullOrWhiteSpace(viewModel.ImageUrl) ? "/images/default-event.jpg" : viewModel.ImageUrl,
                OrganizerID = int.Parse(User.FindFirst("UserID").Value)
            };

            await _eventService.CreateEventAsync(newEvent);

            TempData["Message"] = "Event created successfully!";
            return RedirectToAction("MyEvents");
        }

        [Authorize(Roles = "Organizer")]
        public IActionResult CreateEvent()
        {
            return View();
        }



        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> ManageParticipants(int eventId)
        {
            var organizerId = int.Parse(User.FindFirst("UserID").Value);

            if (await _eventService.IsOrganizerOfEventAsync(organizerId, eventId))
            {
                var participants = await _eventService.GetEventParticipantsAsync(eventId);
                return View(participants);
            }

            return Forbid();
        }



     
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageEvents()
        {
            var events = await _eventService.GetAllEventsAsync();
            return View(events);
        }

        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> EditEvent(int eventId)
        {
            var organizerId = int.Parse(User.FindFirst("UserID").Value);

            var eventToEdit = await _eventService.GetEventByIdAsync(eventId);
            if (eventToEdit == null || eventToEdit.OrganizerID != organizerId)
            {
                return Forbid();
            }

            return View(eventToEdit);
        }

        [HttpPost]
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> EditEvent(Event model)
        {
            if (ModelState.IsValid)
            {
                var organizerId = int.Parse(User.FindFirst("UserID").Value);

                if (await _eventService.UpdateEventAsync(model, organizerId))
                {
                    TempData["Message"] = "Event updated successfully.";
                    return RedirectToAction("MyEvents");
                }
            }

            TempData["Error"] = "Failed to update event.";
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> DeleteEvent(int eventId)
        {
            if (await _eventService.DeleteEventAsync(eventId))
            {
                TempData["Message"] = "Event deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to delete the event.";
            }

            return RedirectToAction("ManageEvents");
        }
    }
}
