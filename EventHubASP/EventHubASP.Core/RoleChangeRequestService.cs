using EventHubASP.DataAccess;
using EventHubASP.Models;
using Microsoft.EntityFrameworkCore;

namespace EventHubASP.Core
{
    public class RoleChangeRequestService : IRoleChangeRequestService
    {
        private readonly ApplicationDbContext _context;

        public RoleChangeRequestService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CreateRequestAsync(int userId, string currentRole, string requestedRole)
        {
            if (await _context.RoleChangeRequests.AnyAsync(r => r.UserID == userId && !r.IsApproved))
            {
                return false; // Prevent duplicate requests
            }

            var request = new RoleChangeRequest
            {
                UserID = userId,
                CurrentRole = currentRole,
                RequestedRole = requestedRole
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

        public async Task ApproveRequestAsync(int requestId)
        {
            var request = await _context.RoleChangeRequests.FindAsync(requestId);
            if (request != null)
            {
                var user = await _context.Users.FindAsync(request.UserID);
                if (user != null)
                {
                    user.RoleID = 2; // Organizer Role
                    _context.RoleChangeRequests.Remove(request);
                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task DenyRequestAsync(int requestId)
        {
            var request = await _context.RoleChangeRequests.FindAsync(requestId);
            if (request != null)
            {
                _context.RoleChangeRequests.Remove(request);
                await _context.SaveChangesAsync();
            }
        }
    }
}
