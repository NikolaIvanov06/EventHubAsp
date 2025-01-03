using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHubASP.Models
{
    public class Notification
    {
        [Key]
        public int NotificationID { get; set; }
        public int UserID { get; set; }
        public int NewsID { get; set; }
        public bool IsRead { get; set; }

        // Navigation Properties
        public User User { get; set; }
        public News News { get; set; }
    }

}
