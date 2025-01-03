using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventHubASP.Models;
public class Event
{
    [Key]
    public int EventID { get; set; }

    [Required]
    [StringLength(100)]
    public string Title { get; set; }

    [Required]
    [StringLength(500)]
    public string Description { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    [StringLength(100)]
    public string Location { get; set; }

    [ForeignKey("Organizer")]
    public int OrganizerID { get; set; }

    public string ImageUrl { get; set; } = "/images/default-event.jpg";
    // Navigation Properties
    public User Organizer { get; set; }
    public ICollection<Registration> Registrations { get; set; }
    public ICollection<News> News { get; set; } = new List<News>();
}
