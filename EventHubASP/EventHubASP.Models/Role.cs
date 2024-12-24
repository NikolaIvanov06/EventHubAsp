using System.ComponentModel.DataAnnotations;

namespace EventHubASP.Models;
public class Role
{
    [Key]
    public int RoleID { get; set; }

    [Required]
    [StringLength(20)]
    public string RoleName { get; set; }

    public ICollection<User> Users { get; set; }
}
