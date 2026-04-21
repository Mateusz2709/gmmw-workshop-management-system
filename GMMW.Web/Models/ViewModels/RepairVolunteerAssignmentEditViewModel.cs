using System.ComponentModel.DataAnnotations;

namespace GMMW.Web.Models.ViewModels
{
    public class RepairVolunteerAssignmentEditViewModel
    {
        public int RepairVolunteerAssignmentId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "A valid repair must be selected.")]
        public int RepairId { get; set; }

        [Required(ErrorMessage = "A volunteer must be selected.")]
        public string ApplicationUserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hours spent is required.")]
        [Range(typeof(decimal), "0.01", "1000", ErrorMessage = "Hours spent must be greater than zero.")]
        public decimal? HoursSpent { get; set; }

        [StringLength(1000, ErrorMessage = "Notes cannot be longer than 1000 characters.")]
        public string Notes { get; set; } = string.Empty;
    }
}