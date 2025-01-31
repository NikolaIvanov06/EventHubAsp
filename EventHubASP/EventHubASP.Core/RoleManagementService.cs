using EventHubASP.DataAccess;
using EventHubASP.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventHubASP.Core
{
    public class RoleManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;

        public RoleManagementService(ApplicationDbContext context, UserManager<User> userManager, RoleManager<IdentityRole<Guid>> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<List<RoleChangeRequest>> GetPendingRequestsAsync()
        {
            return await _context.RoleChangeRequests
                .Where(r => !r.IsApproved)
                .Include(r => r.User)
                .ToListAsync();
        }

        public async Task ApproveRequestAsync(Guid requestId)
        {
            var request = await _context.RoleChangeRequests.FindAsync(requestId);
            if (request != null)
            {
                var user = await _userManager.FindByIdAsync(request.UserID.ToString());
                if (user != null)
                {
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    await _userManager.AddToRoleAsync(user, request.RequestedRole);

                    request.IsApproved = true;
                    _context.RoleChangeRequests.Remove(request);

                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task DeclineRequestAsync(Guid requestId)
        {
            var request = await _context.RoleChangeRequests.FindAsync(requestId);
            if (request != null)
            {
                _context.RoleChangeRequests.Remove(request);
                await _context.SaveChangesAsync();
            }
        }

        public async Task SubmitRoleChangeRequestAsync(Guid userId, string requestedRole)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user != null && !await _userManager.IsInRoleAsync(user, requestedRole))
            {
                var request = new RoleChangeRequest
                {
                    UserID = userId,
                    CurrentRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault(),
                    RequestedRole = requestedRole,
                    RequestDate = DateTime.Now,
                    IsApproved = false
                };

                _context.RoleChangeRequests.Add(request);
                await _context.SaveChangesAsync();
            }
        }
    }
}