using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHubASP.Models
{
    public class EventDetails
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Event")]
        public int EventID { get; set; }

        public string CustomContent { get; set; }

        public virtual Event Event { get; set; }
    }
}
