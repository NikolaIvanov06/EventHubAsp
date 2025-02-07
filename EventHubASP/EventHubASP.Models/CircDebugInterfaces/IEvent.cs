using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHubASP.Models.CircDebugInterfaces
{
    public interface IEvent
    {
        int EventID { get; set; }
        ICollection<Registration> Registrations { get; set; }
    }
}
