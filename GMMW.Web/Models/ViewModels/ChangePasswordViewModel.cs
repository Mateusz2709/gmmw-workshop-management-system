using System.ComponentModel.DataAnnotations;

namespace GMMW.Web.Models.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Display(Name = "Current Password")]
        [Required(ErrorMessage = "Current password is required.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Display(Name = "New Password")]
        [Required(ErrorMessage = "New password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "New password must be between 6 and 100 characters.")]
        public string NewPassword { get; set; } = string.Empty;

        [Display(Name = "Confirm New Password")]
        [Required(ErrorMessage = "Please confirm the new password.")]
        [Compare(nameof(NewPassword), ErrorMessage = "New password and confirmation must match.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
