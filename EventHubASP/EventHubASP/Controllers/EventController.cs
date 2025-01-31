using EventHubASP.Core;
using EventHubASP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

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
        public async Task<IActionResult> BrowseEvents(EventFilterViewModel filter)
        {
            var events = await _eventService.GetAllEventsAsync();

            if (filter.FilterByDate.HasValue)
            {
                events = events.Where(e => e.Date.Date == filter.FilterByDate.Value.Date);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
            {
                events = events.Where(e => e.Title.Contains(filter.SearchQuery, StringComparison.OrdinalIgnoreCase));
            }

            if (filter.SortByDate)
            {
                events = events.OrderByDescending(e => e.Date);
            }

            if (filter.SortByTitleLength)
            {
                events = events.OrderBy(e => e.Title);
            }

            ViewBag.Filter = filter;

            return View(events);
        }

        [Authorize(Roles = "User")]
        [HttpPost]
        public async Task<IActionResult> Subscribe(int eventId)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            if (await _eventService.SubscribeToEventAsync(userId, eventId))
            {
                TempData["SuccessMessage"] = "Successfully subscribed to the event.";
            }
            else
            {
                TempData["ErrorMessage"] = "Unable to subscribe. You may already be registered.";
            }

            return RedirectToAction("BrowseEvents");
        }

        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> MyEvents()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var organizerId = Guid.Parse(userIdClaim.Value);
            var events = await _eventService.GetEventsByOrganizerAsync(organizerId);
            return View(events);
        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> MySubscriptions()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var events = await _eventService.GetUserSubscriptionsAsync(userId);
            return View(events);
        }

        [Authorize(Roles = "User")]
        [HttpPost]
        public async Task<IActionResult> Unsubscribe(int eventId)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            if (await _eventService.UnsubscribeFromEventAsync(userId, eventId))
            {
                TempData["SuccessMessage"] = "Successfully unsubscribed from the event.";
            }
            else
            {
                TempData["ErrorMessage"] = "Unable to unsubscribe. Some sort of error.";
            }

            return RedirectToAction("BrowseEvents");
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

            var newEvent = new Event
            {
                Title = viewModel.Title,
                Description = viewModel.Description,
                Date = viewModel.Date,
                Location = viewModel.Location,
                ImageUrl = string.IsNullOrWhiteSpace(viewModel.ImageUrl) ? "/images/default-event.jpg" : viewModel.ImageUrl,
                OrganizerID = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)
            };

            await _eventService.CreateEventAsync(newEvent);

            TempData["SuccessMessage"] = "Event created successfully!";
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
            var organizerId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

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
            var organizerId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var eventToEdit = await _eventService.GetEventByIdAsync(eventId);
            if (eventToEdit == null || eventToEdit.OrganizerID != organizerId)
            {
                return Forbid();
            }

            var viewModel = new EventViewModel
            {
                Title = eventToEdit.Title,
                Description = eventToEdit.Description,
                Date = eventToEdit.Date,
                Location = eventToEdit.Location,
                ImageUrl = eventToEdit.ImageUrl
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> EditEvent(int eventId, EventViewModel viewModel)
        {
            Console.WriteLine($"EditEvent POST called with eventId: {eventId}");
            if (!ModelState.IsValid)
            {
                Console.WriteLine("Model state is invalid.");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Validation Error: {error.ErrorMessage}");
                }
                return View(viewModel);
            }

            Console.WriteLine($"Attempting to update event with ID: {eventId}");
            var organizerId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var updatedEvent = new Event
            {
                EventID = eventId,
                Title = viewModel.Title,
                Description = viewModel.Description,
                Date = viewModel.Date,
                Location = viewModel.Location,
                ImageUrl = string.IsNullOrWhiteSpace(viewModel.ImageUrl) ? "/images/default-event.jpg" : viewModel.ImageUrl,
                OrganizerID = organizerId
            };

            var success = await _eventService.UpdateEventAsync(updatedEvent, organizerId);

            if (success)
            {
                TempData["SuccessMessage"] = "Event updated successfully.";
                Console.WriteLine("Event updated successfully.");
                return RedirectToAction("MyEvents");
            }

            TempData["ErrorMessage"] = "Failed to update event.";
            Console.WriteLine("Failed to update event.");
            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Organizer, Admin")]
        public async Task<IActionResult> DeleteEvent(int eventId)
        {
            if (await _eventService.DeleteEventAsync(eventId))
            {
                TempData["SuccessMessage"] = "Successfully deleted the event!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete the event.";
            }
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("BrowseEvents");
            }
            else
            {
                return RedirectToAction("MyEvents");
            }
        }
    }
}