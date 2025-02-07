using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using EventHubASP.Core;
using EventHubASP.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using EventHubASP.Core.Hubs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;

namespace EventHubASP.Controllers
{
    public class NewsController : Controller
    {
        private readonly INewsService _newsService;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly UserManager<User> _userManager;

        public NewsController(INewsService newsService, INotificationService notificationService, IHubContext<NotificationHub> hubContext, UserManager<User> userManager)
        {
            _newsService = newsService;
            _notificationService = notificationService;
            _hubContext = hubContext;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = Guid.Parse(_userManager.GetUserId(User));
            var events = await _newsService.GetEventsByOrganizerAsync(userId);
            var model = new NewsViewModel
            {
                AvailableEvents = events.Select(e => new SelectListItem { Value = e.EventID.ToString(), Text = e.Title }).ToList()
            };
            return View(model);
        }

        [Authorize(Roles = "Organizer")]
        [HttpPost]
        public async Task<IActionResult> Create(NewsViewModel model)
        {
            ModelState.Remove("AvailableEvents");

            if (ModelState.IsValid)
            {
                var news = new News
                {
                    Title = model.Title,
                    Content = model.Content,
                    EventID = model.EventID,
                    OrganizerID = Guid.Parse(_userManager.GetUserId(User)),
                    PublishedDate = DateTime.UtcNow
                };
                await _newsService.CreateNewsAsync(news);

                var participants = await _notificationService.GetParticipantsByEventIdAsync(news.EventID);
                foreach (var participant in participants)
                {
                    var notification = new Notification
                    {
                        UserID = participant.Id,
                        NewsID = news.NewsID,
                        Message = $"New news published: {news.Title}",
                        IsRead = false,
                        Date = DateTime.UtcNow
                    };
                    await _notificationService.CreateNotificationAsync(notification);
                    try
                    {
                        await _hubContext.Clients.User(participant.Id.ToString()).SendAsync("ReceiveNotification", notification.Message);
                        await _hubContext.Clients.All.SendAsync("ReceiveNews", news);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending notification/news: {ex}");
                    }
                }


                TempData["SuccessMessage"] = "Published!";
                return RedirectToAction("Index", "Home");
            }

            var userId = Guid.Parse(_userManager.GetUserId(User));
            var events = await _newsService.GetEventsByOrganizerAsync(userId);
            model.AvailableEvents = events.Select(e => new SelectListItem { Value = e.EventID.ToString(), Text = e.Title }).ToList();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetNewsForUser(Guid userId)
        {
            try
            {
                var news = await _newsService.GetNewsForUserAsync(userId);
                return Ok(news);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> MyFeed()
        {
            var userId = Guid.Parse(_userManager.GetUserId(User));
            var news = await _newsService.GetNewsForUserAsync(userId);
            return View(news);
        }
    }
}