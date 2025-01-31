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
    public class RoleChangeRequestService : IRoleChangeRequestService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;

        public RoleChangeRequestService(ApplicationDbContext context, UserManager<User> userManager, RoleManager<IdentityRole<Guid>> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<bool> CreateRequestAsync(Guid userId, string currentRole, string requestedRole)
        {
            if (await _context.RoleChangeRequests.AnyAsync(r => r.UserID == userId && !r.IsApproved))
            {
                return false; 
            }

            var request = new RoleChangeRequest
            {
                UserID = userId,
                CurrentRole = currentRole,
                RequestedRole = requestedRole,
                RequestDate = DateTime.Now,
                IsApproved = false
            };

            _context.RoleChangeRequests.Add(request);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<RoleChangeRequest>> GetPendingRequestsAsync()
        {
            return await _context.RoleChangeRequests
                .Include(r => r.User)
                .Where(r => !r.IsApproved)
                .ToListAsync();
        }

        public async Task ApproveRequestAsync(Guid requestId)
        {
            var request = await _context.RoleChangeRequests.FirstOrDefaultAsync(r => r.RequestID == requestId);
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

        public async Task DenyRequestAsync(Guid requestId)
        {
            var request = await _context.RoleChangeRequests.FirstOrDefaultAsync(r => r.RequestID == requestId);
            if (request != null)
            {
                _context.RoleChangeRequests.Remove(request);
                await _context.SaveChangesAsync();
            }
        }


    }
}