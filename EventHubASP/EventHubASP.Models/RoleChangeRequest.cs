using System.ComponentModel.DataAnnotations;

namespace EventHubASP.Models
{
    public class RoleChangeRequest
    {
        [Key]
        public int RequestID { get; set; }

        public int UserID { get; set; }
        public string CurrentRole { get; set; }
        public string RequestedRole { get; set; }
        public DateTime RequestDate { get; set; }
        public bool IsApproved { get; set; }

        public User User { get; set; }
    }
}
