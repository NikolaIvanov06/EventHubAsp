using System;

namespace EventHubASP.Models
{
    public class EventFilterViewModel
    {
        public bool SortByDate { get; set; }
        public bool SortByTitleLength { get; set; }
        public DateTime? FilterByDate { get; set; }
        public string SearchQuery { get; set; }
    }
}