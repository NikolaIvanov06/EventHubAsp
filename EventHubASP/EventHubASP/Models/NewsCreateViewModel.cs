using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventHubASP.Models
{

        public class NewsCreateViewModel
        {
            public string Title { get; set; }
            public string Content { get; set; }
            public int EventId { get; set; }
            public IEnumerable<SelectListItem> AvailableEvents { get; set; } = new List<SelectListItem>();
        }
    
}
