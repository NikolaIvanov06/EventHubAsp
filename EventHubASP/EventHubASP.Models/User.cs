using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventHubASP.Models
{
    public class User : IdentityUser<Guid>
    {
        public User()
        {
            Registrations = new List<Registration>();
            Notifications = new List<Notification>();
        }

        public override string UserName { get; set; }
        public override string Email { get; set; }

        public string? HashedRecoveryCode { get; set; }

        public ICollection<Registration> Registrations { get; set; }
        public ICollection<Notification> Notifications { get; set; }
    }
}
