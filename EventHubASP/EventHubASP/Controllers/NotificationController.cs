using Microsoft.AspNetCore.Mvc;
using EventHubASP.Models;
using EventHubASP.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;

namespace EventHubASP.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<User> _userManager;

        public NotificationController(INotificationService notificationService, UserManager<User> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        [HttpGet("user-notifications")]
        public async Task<IActionResult> GetUserNotifications()
        {
            var userId = Guid.Parse(_userManager.GetUserId(User));
            var notifications = await _notificationService.GetNotificationsForUserAsync(userId);
            return Ok(notifications);
        }

        [HttpPost("mark-as-read/{notificationId}")]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            var userId = Guid.Parse(_userManager.GetUserId(User));
            var result = await _notificationService.MarkNotificationAsReadAsync(notificationId, userId);
            if (result)
            {
                return Ok();
            }
            return BadRequest("Failed to mark notification as read.");
        }
    }
}