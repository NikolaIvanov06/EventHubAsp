using EventHubASP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHubASP.Core
{
    public interface IRoleChangeRequestService
    {
       Task<bool> CreateRequestAsync(Guid userId, string currentRole, string requestedRole);
       Task<IEnumerable<RoleChangeRequest>> GetPendingRequestsAsync();
       Task ApproveRequestAsync(Guid requestId);
       Task DenyRequestAsync(Guid requestId);
    }
}
