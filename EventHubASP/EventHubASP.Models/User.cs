using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventHubASP.Models;
public class User
{
    [Key]
    public int UserID { get; set; }

    [Required]
    [StringLength(50)]
    public string Username { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    public string PasswordHash { get; set; }

    [ForeignKey("Role")]
    public int RoleID { get; set; }


    public Role Role { get; set; }
    public ICollection<Event> Events { get; set; }
    public ICollection<Registration> Registrations { get; set; }
}
