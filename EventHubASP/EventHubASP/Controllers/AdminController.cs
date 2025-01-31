using EventHubASP.Core;
using EventHubASP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventHubASP.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly RoleManagementService _roleService;
        private readonly UserService _userService;

        public AdminController(RoleManagementService roleService, UserService userService)
        {
            _roleService = roleService;
            _userService = userService;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageRoleRequests()
        {
            var requests = await _roleService.GetPendingRequestsAsync();
            return View(requests);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveRequest(Guid requestId)
        {
            await _roleService.ApproveRequestAsync(requestId);
            TempData["SuccessMessage"] = "Successfully approved User's request.";
            return RedirectToAction("ManageRoleRequests");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeclineRequest(Guid requestId)
        {
            await _roleService.DeclineRequestAsync(requestId);
            TempData["SuccessMessage"] = "Successfully denied User's request.";
            return RedirectToAction("ManageRoleRequests");
        }


        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(Guid userId)
        {
            var result = await _userService.DeleteUserAsync(userId);
            if (result)
            {
                TempData["SuccessMessage"] = "User deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete user.";
            }

            return RedirectToAction("ManageUsers");
        }
    }
}