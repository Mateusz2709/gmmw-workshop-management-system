using System.ComponentModel.DataAnnotations;
using GMMW.Web.Models.Enums;

namespace GMMW.Web.Models.ViewModels
{
    public class RepairEditViewModel
    {
        public int RepairId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "A valid vehicle must be selected.")]
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "Repair date is required.")]
        public DateTime RepairDate { get; set; } = DateTime.Today;

        [StringLength(1000, ErrorMessage = "Fault description cannot be longer than 1000 characters.")]
        [Required(ErrorMessage = "Fault description is required.")]
        public string FaultDescription { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Work carried out cannot be longer than 2000 characters.")]
        public string WorkCarriedOut { get; set; } = string.Empty;

        [Required(ErrorMessage = "Repair status is required.")]
        public RepairStatus RepairStatus { get; set; } = RepairStatus.Pending;
    }
}
