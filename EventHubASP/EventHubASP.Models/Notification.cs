using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventHubASP.Models;
public class Notification
{
    [Key]
    public int NotificationID { get; set; }

    [ForeignKey("User")]
    public Guid UserID { get; set; }

    [ForeignKey("News")]
    public int NewsID { get; set; }

    [Required]
    public string Message { get; set; }

    public bool IsRead { get; set; }

    public DateTime Date { get; set; }

    // Navigation Properties
    public User User { get; set; }
    public News News { get; set; }
}
