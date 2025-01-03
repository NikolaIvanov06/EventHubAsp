using EventHubASP.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHubASP.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly RoleManagementService _roleService;

        public AdminController(RoleManagementService roleService)
        {
            _roleService = roleService;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageRoleRequests()
        {
            var requests = await _roleService.GetPendingRequestsAsync();
            return View(requests);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveRequest(int requestId)
        {
            await _roleService.ApproveRequestAsync(requestId);
            return RedirectToAction("ManageRoleRequests");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeclineRequest(int requestId)
        {
            await _roleService.DeclineRequestAsync(requestId);
            return RedirectToAction("ManageRoleRequests");
        }
    }

}
