using System.ComponentModel.DataAnnotations;

namespace GMMW.Web.Models.ViewModels
{
    public class MotoristEditViewModel
    {
        public int MotoristId { get; set; }

        [StringLength(50, ErrorMessage = "First name cannot be longer than 50 characters.")]
        [Required(ErrorMessage = "First name is required.")]
        public string FirstName { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Last name cannot be longer than 50 characters.")]
        [Required(ErrorMessage = "Last name is required.")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Email cannot be longer than 100 characters.")]
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "A valid email address is required.")]
        public string Email { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Phone number cannot be longer than 20 characters.")]
        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "A valid phone number is required.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Address cannot be longer than 200 characters.")]
        public string Address { get; set; } = string.Empty;
    }
}
