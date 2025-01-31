namespace EventHubASP.Models
{

        public class NewsCreateViewModel
        {
            public string Title { get; set; }
            public string Content { get; set; }
            public int EventId { get; set; }
            public List<EventViewModel> Events { get; set; } = new List<EventViewModel>();
        }
    
}
