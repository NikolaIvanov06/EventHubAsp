using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace EventHubASP.Models;
public class Role : IdentityRole<Guid>
{
    public Role() { }
    public Role(string roleName) : base(roleName) { }
}
