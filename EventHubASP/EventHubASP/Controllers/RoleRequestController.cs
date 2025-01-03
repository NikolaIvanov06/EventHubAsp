using EventHubASP.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
            var userId = int.Parse(User.FindFirst("UserID").Value);

            var success = await _roleChangeRequestService.CreateRequestAsync(
                userId,
                "User",
                "Organizer"
            );

            if (success)
            {
                TempData["Message"] = "Your request has been submitted successfully!";
            }
            else
            {
                TempData["Error"] = "You have already submitted a request!";
            }

            return RedirectToAction("Index", "Home");
        }

        // Admin: View all pending role requests
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

        // Admin: Approve a role change request
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> ApproveRequest(int requestId)
        {
            await _roleChangeRequestService.ApproveRequestAsync(requestId);
            return RedirectToAction("ViewRoleRequests");
        }

        // Admin: Deny a role change request
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> DenyRequest(int requestId)
        {
            await _roleChangeRequestService.DenyRequestAsync(requestId);
            return RedirectToAction("ViewRoleRequests");
        }
    }
}
