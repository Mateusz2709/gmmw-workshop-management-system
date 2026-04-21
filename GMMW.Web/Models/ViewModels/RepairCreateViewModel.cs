using System.ComponentModel.DataAnnotations;

namespace GMMW.Web.Models.ViewModels
{
    public class RepairCreateViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "A valid vehicle must be selected.")]
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "Repair date is required.")]
        public DateTime RepairDate { get; set; } = DateTime.Today;

        [StringLength(1000, ErrorMessage = "Fault description cannot be longer than 1000 characters.")]
        [Required(ErrorMessage = "Fault description is required.")]
        public string FaultDescription { get; set; } = string.Empty;

    }
}