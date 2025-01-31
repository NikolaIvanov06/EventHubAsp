using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventHubASP.Models;
public class Registration
{
    [Key]
    public int RegistrationID { get; set; }

    [ForeignKey("User")]
    public Guid UserID { get; set; }

    [ForeignKey("Event")]
    public int EventID { get; set; }

    [Required]
    public DateTime RegistrationDate { get; set; }

    public User User { get; set; }
    public Event Event { get; set; }
}
