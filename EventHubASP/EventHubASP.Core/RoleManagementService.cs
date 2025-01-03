using EventHubASP.DataAccess;
using EventHubASP.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHubASP.Core
{
    public class RoleManagementService
    {
        private readonly ApplicationDbContext _context;

        public RoleManagementService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<RoleChangeRequest>> GetPendingRequestsAsync()
        {
            return await _context.RoleChangeRequests
                .Where(r => !r.IsApproved)
                .Include(r => r.User)
                .ToListAsync();
        }

        public async Task ApproveRequestAsync(int requestId)
        {
            var request = await _context.RoleChangeRequests.FindAsync(requestId);
            if (request != null)
            {
                var user = await _context.Users.FindAsync(request.UserID);
                if (user != null)
                {
                    user.RoleID = _context.Roles.FirstOrDefault(r => r.RoleName == request.RequestedRole).RoleID;
                    request.IsApproved = true;
                    _context.RoleChangeRequests.Remove(request);

                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task DeclineRequestAsync(int requestId)
        {
            var request = await _context.RoleChangeRequests.FindAsync(requestId);
            if (request != null)
            {
                _context.RoleChangeRequests.Remove(request);
                await _context.SaveChangesAsync();
            }
        }

        public async Task SubmitRoleChangeRequestAsync(int userId, string requestedRole)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null && user.Role.RoleName != requestedRole)
            {
                var request = new RoleChangeRequest
                {
                    UserID = userId,
                    CurrentRole = user.Role.RoleName,
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
