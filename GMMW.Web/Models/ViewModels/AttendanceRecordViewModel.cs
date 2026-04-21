using System.ComponentModel.DataAnnotations;

namespace GMMW.Web.Models.ViewModels
{
    public class AttendanceRecordViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "A valid class must be selected.")]
        public int WorkshopClassId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "A valid motorist must be selected.")]
        public int MotoristId { get; set; }

        public DateTime AttendanceDate { get; set; } = DateTime.Now;

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;
    }
}
