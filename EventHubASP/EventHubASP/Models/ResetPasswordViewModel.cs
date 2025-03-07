using System.ComponentModel.DataAnnotations;

namespace EventHubASP.Models
{
    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "Email е задължителен.")]
        [EmailAddress(ErrorMessage = "Невалиден имейл адрес.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Токенът е задължителен.")]
        public string Token { get; set; }

        [Required(ErrorMessage = "Моля, въведете нова парола.")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Паролата трябва да е между 6 и 100 символа.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Моля, потвърдете новата парола.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Паролите не съвпадат.")]
        public string ConfirmPassword { get; set; }
    }
}