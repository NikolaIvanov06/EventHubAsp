using EventHubASP.Core;
using EventHubASP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EventHubASP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadNotificationCount()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            Console.WriteLine("Hello World!");
            var notifications = await _notificationService.GetNotificationsForUserAsync(userId);
            return Ok(new { count = notifications.Count });
        }

        [HttpPost("mark-read")]
        public async Task<IActionResult> MarkNotificationsAsRead()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            await _notificationService.MarkAsReadAsync(userId);
            return Ok();
        }
    }
}
