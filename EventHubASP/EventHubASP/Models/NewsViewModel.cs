using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventHubASP.Models
{
    public class NewsViewModel
    {
        [Required]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        [Required]
        public int EventID { get; set; }

        public List<SelectListItem> AvailableEvents { get; set; }
    }
}