using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using EventHubASP.Core;
using EventHubASP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EventHubASP.Controllers
{
    public class EventController : Controller
    {
        private readonly IEventService _eventService;
        private readonly Cloudinary _cloudinary;
        private readonly IWebHostEnvironment _environment;

        public EventController(IEventService eventService, Cloudinary cloudinary, IWebHostEnvironment environment)
        {
            _eventService = eventService;
            _cloudinary = cloudinary;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var eventItem = await _eventService.GetEventByIdAsync(id);
            if (eventItem == null)
            {
                return NotFound();
            }
            return View(eventItem);
        }

        [Authorize(Roles = "Organizer")]
        [HttpGet]
        public async Task<IActionResult> EditDetails(int id)
        {
            var eventItem = await _eventService.GetEventByIdAsync(id);
            if (eventItem == null)
            {
                return NotFound();
            }

            var organizerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (!await _eventService.IsOrganizerOfEventAsync(organizerId, id))
            {
                return Forbid();
            }

            return View(eventItem);
        }

        [Authorize(Roles = "Organizer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDetails(int id, string customContent)
        {
            var organizerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            Console.WriteLine($"Attempting to save custom content for EventID {id}: {customContent?.Substring(0, Math.Min(50, customContent?.Length ?? 0)) ?? "null"}");

            // Delegate the save operation to the service
            var success = await _eventService.SaveCustomDetailsAsync(id, customContent, organizerId);

            if (!success)
            {
                Console.WriteLine($"Save failed for EventID {id}. Returning to edit view.");
                var eventModel = await _eventService.GetEventByIdAsync(id);
                if (eventModel == null)
                {
                    Console.WriteLine($"Event with ID {id} not found after save failure.");
                    return NotFound();
                }
                return View(eventModel);
            }

            Console.WriteLine($"Save successful for EventID {id}. Redirecting to Details.");
            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Roles = "Organizer")]
        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "No file uploaded" });
            }
            if (!file.ContentType.StartsWith("image/"))
            {
                return Json(new { success = false, message = "Only image files are allowed" });
            }

            try
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, file.OpenReadStream()),
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                var imageUrl = uploadResult.SecureUrl.ToString();

                return Json(new { location = imageUrl }); // Use 'location' for TinyMCE compatibility
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading image: {ex.Message}");
                return Json(new { success = false, message = "Image upload failed: " + ex.Message });
            }
        }

        [Authorize(Roles = "Organizer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadVideo(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                Console.WriteLine("No file uploaded or file length is 0");
                return Json(new { success = false, message = "No file uploaded" });
            }

            Console.WriteLine($"Received file: {file.FileName}, ContentType: {file.ContentType}, Length: {file.Length}");

            if (!file.ContentType.StartsWith("video/"))
            {
                Console.WriteLine("Invalid file type: Only video files are allowed");
                return Json(new { success = false, message = "Only video files are allowed" });
            }

            try
            {
                var uploadParams = new VideoUploadParams()
                {
                    File = new FileDescription(file.FileName, file.OpenReadStream())
                };

                Console.WriteLine("Uploading to Cloudinary...");
                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                var videoUrl = uploadResult.SecureUrl.ToString();
                Console.WriteLine($"Upload successful, URL: {videoUrl}");

                return Json(new { success = true, url = videoUrl });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading video: {ex.Message}");
                return Json(new { success = false, message = "Video upload failed: " + ex.Message });
            }
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
                TempData["ErrorMessage"] = "Event creation failed due to validation errors.";
                return View(viewModel);
            }

            if (viewModel.fileUpload != null)
            {
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(viewModel.fileUpload.FileName, viewModel.fileUpload.OpenReadStream())
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                viewModel.ImageUrl = uploadResult.SecureUrl.ToString();
            }

            if (string.IsNullOrWhiteSpace(viewModel.ImageUrl))
            {
                TempData["ErrorMessage"] = "Please provide an image URL or upload a file.";
                return View(viewModel);
            }

            var newEvent = new Event
            {
                Title = viewModel.Title,
                Description = viewModel.Description,
                Date = viewModel.Date,
                Location = viewModel.Location,
                ImageUrl = viewModel.ImageUrl,
                OrganizerID = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)
            };

            try
            {
                await _eventService.CreateEventAsync(newEvent);
                TempData["SuccessMessage"] = "Event created successfully!";
                return RedirectToAction("MyEvents");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating event: {ex.Message}");
                TempData["ErrorMessage"] = "Event creation failed. Please try again.";
                return View(viewModel);
            }
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
                TempData["ErrorMessage"] = "Event not found or you do not have permission to edit this event.";
                return RedirectToAction("MyEvents");
            }

            var viewModel = new EventViewModel
            {
                EventID = eventToEdit.EventID,
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
        public async Task<IActionResult> EditEvent(EventViewModel viewModel)
        {
            Console.WriteLine($"EditEvent POST called with eventId: {viewModel.EventID}");
            if (!ModelState.IsValid)
            {
                Console.WriteLine("Model state is invalid.");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Validation Error: {error.ErrorMessage}");
                }
                TempData["ErrorMessage"] = "Event update failed due to validation errors.";
                return View(viewModel);
            }

            var organizerId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var existingEvent = await _eventService.GetEventByIdAsync(viewModel.EventID);

            if (existingEvent == null || existingEvent.OrganizerID != organizerId)
            {
                TempData["ErrorMessage"] = "Event not found or you do not have permission to edit this event.";
                return RedirectToAction("MyEvents");
            }

            string newImageUrl = viewModel.ImageUrl;

            if (viewModel.fileUpload != null)
            {
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(viewModel.fileUpload.FileName, viewModel.fileUpload.OpenReadStream())
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                newImageUrl = uploadResult.SecureUrl.ToString();

                if (!string.IsNullOrWhiteSpace(existingEvent.ImageUrl) && existingEvent.ImageUrl.Contains("cloudinary.com"))
                {
                    var publicId = existingEvent.ImageUrl.Split('/').Last().Split('.').First();
                    await _cloudinary.DestroyAsync(new DeletionParams(publicId));
                }
            }
            else if (!string.IsNullOrWhiteSpace(viewModel.ImageUrl) && viewModel.ImageUrl != existingEvent.ImageUrl)
            {
                if (!string.IsNullOrWhiteSpace(existingEvent.ImageUrl) && existingEvent.ImageUrl.Contains("cloudinary.com"))
                {
                    var publicId = existingEvent.ImageUrl.Split('/').Last().Split('.').First();
                    await _cloudinary.DestroyAsync(new DeletionParams(publicId));
                }
            }

            if (string.IsNullOrWhiteSpace(newImageUrl))
            {
                TempData["ErrorMessage"] = "Please provide an image URL or upload a file.";
                return View(viewModel);
            }

            existingEvent.Title = viewModel.Title;
            existingEvent.Description = viewModel.Description;
            existingEvent.Date = viewModel.Date;
            existingEvent.Location = viewModel.Location;
            existingEvent.ImageUrl = newImageUrl;

            try
            {
                await _eventService.UpdateEventAsync(existingEvent, organizerId);
                TempData["SuccessMessage"] = "Event updated successfully.";
                return RedirectToAction("MyEvents");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating event: {ex.Message}");
                TempData["ErrorMessage"] = "Event update failed. Please try again.";
                return View(viewModel);
            }
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