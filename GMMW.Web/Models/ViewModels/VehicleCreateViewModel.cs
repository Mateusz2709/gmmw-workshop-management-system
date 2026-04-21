using System.ComponentModel.DataAnnotations;
using GMMW.Web.Models.Enums;

namespace GMMW.Web.Models.ViewModels
{
    public class VehicleCreateViewModel
    {
        [Required(ErrorMessage = "Registration number is required.")]
        [StringLength(20, ErrorMessage = "Registration number cannot be longer than 20 characters.")]
        public string RegistrationNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Make is required.")]
        [StringLength(50, ErrorMessage = "Make cannot be longer than 50 characters.")]
        public string Make { get; set; } = string.Empty;

        [Required(ErrorMessage = "Model is required.")]
        [StringLength(50, ErrorMessage = "Model cannot be longer than 50 characters.")]
        public string Model { get; set; } = string.Empty;

        [Range(1886, 2100, ErrorMessage = "Year must be between 1886 and 2100.")]
        public int? Year { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "A valid vehicle type must be selected.")]
        public VehicleType VehicleType { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "A valid motorist must be selected.")]
        public int MotoristId { get; set; }
    }
}