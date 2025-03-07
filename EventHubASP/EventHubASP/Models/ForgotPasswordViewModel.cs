using System.ComponentModel.DataAnnotations;

namespace EventHubASP.Models
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string RecoveryCode { get; set; }
    }
}