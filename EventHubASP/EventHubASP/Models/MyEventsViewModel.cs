using System.Collections.Generic;

namespace EventHubASP.Models
{
    public class MyEventsViewModel
    {
        public IEnumerable<Event> UpcomingEvents { get; set; }
        public IEnumerable<Event> PastEvents { get; set; }
    }
}