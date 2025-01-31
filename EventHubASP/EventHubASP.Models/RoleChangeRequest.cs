using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventHubASP.Models;
public class RoleChangeRequest
{
    [Key]
    public Guid RequestID { get; set; }

    [ForeignKey("User")]
    public Guid UserID { get; set; }

    [Required]
    public string CurrentRole { get; set; }

    [Required]
    public string RequestedRole { get; set; }

    public DateTime RequestDate { get; set; }

    public bool IsApproved { get; set; }

    public User User { get; set; }
}
