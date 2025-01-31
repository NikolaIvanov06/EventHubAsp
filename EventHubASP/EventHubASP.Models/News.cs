using EventHubASP.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class News
{
    [Key]
    public int NewsID { get; set; }

    [ForeignKey("Event")]
    public int EventID { get; set; }

    public Guid OrganizerID { get; set; } // ✅ No [ForeignKey] attribute needed

    [Required]
    public string Title { get; set; }

    [Required]
    public string Content { get; set; }

    public DateTime PublishedDate { get; set; }

    public Event Event { get; set; }
    public User Organizer { get; set; } // ✅ EF will infer the FK relation
}
