using System.ComponentModel.DataAnnotations;

namespace GMMW.Web.Models.ViewModels
{
    public class UserRolesEditViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public bool IsWorkshopUser { get; set; }

        public bool IsSuperUser { get; set; }
    }
}
