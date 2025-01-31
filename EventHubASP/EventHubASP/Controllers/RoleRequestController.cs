using EventHubASP.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace EventHubASP.Controllers
{
    public class RoleRequestController : Controller
    {
        private readonly IRoleChangeRequestService _roleChangeRequestService;

        public RoleRequestController(IRoleChangeRequestService roleChangeRequestService)
        {
            _roleChangeRequestService = roleChangeRequestService;
        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> RequestOrganizerRole()
        {
            var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

            var success = await _roleChangeRequestService.CreateRequestAsync(
                userId,
                "User",
                "Organizer"
            );

            if (success)
            {
                TempData["SuccessMessage"] = "Your request has been submitted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "You have already submitted a request!";
            }

            return RedirectToAction("Index", "Home");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ViewRoleRequests()
        {
            var requests = await _roleChangeRequestService.GetPendingRequestsAsync();
            return View(requests);
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> ApproveRequest(Guid requestId)
        {
            await _roleChangeRequestService.ApproveRequestAsync(requestId);
            return RedirectToAction("ViewRoleRequests");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> DenyRequest(Guid requestId)
        {
            await _roleChangeRequestService.DenyRequestAsync(requestId);
            return RedirectToAction("ViewRoleRequests");
        }
    }
}