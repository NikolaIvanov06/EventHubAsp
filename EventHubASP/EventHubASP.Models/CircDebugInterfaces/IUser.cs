using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHubASP.Models.CircDebugInterfaces
{
    public interface IUser
    {
        Guid UserID { get; set; }
        ICollection<Notification> Notifications { get; set; }
    }
}
