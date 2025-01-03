using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHubASP.Models
{
    public class News
    {
        [Key]
        public int NewsID { get; set; }
        public int EventID { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime PublishedDate { get; set; }

        public Event Event { get; set; }
    }

}
