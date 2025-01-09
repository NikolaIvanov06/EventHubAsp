using System.ComponentModel.DataAnnotations;

namespace EventHubASP.Models
{
    public class EventViewModel
    {
        [Required]
        public int EventID { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The title must be between 1 and 100 characters.")]
        public string Title { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "The description must be between 1 and 500 characters.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Please select a valid date.")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The location must be between 1 and 100 characters.")]
        public string Location { get; set; }

        [Url(ErrorMessage = "Please provide a valid URL.")]
        public string? ImageUrl { get; set; }
    }
}
