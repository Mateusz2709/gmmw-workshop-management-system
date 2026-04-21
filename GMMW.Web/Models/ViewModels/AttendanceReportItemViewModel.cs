using System.ComponentModel.DataAnnotations;

namespace GMMW.Web.Models.ViewModels
{
    public class AttendanceReportItemViewModel
    {
        [Display(Name = "Motorist")]
        [StringLength(150)]
        public string MotoristName { get; set; } = string.Empty;

        [Display(Name = "Class")]
        [StringLength(150)]
        public string ClassTitle { get; set; } = string.Empty;

        [Display(Name = "Class Date")]
        public DateTime ClassDate { get; set; }

        [Display(Name = "Attendance Date")]
        public DateTime AttendanceDate { get; set; }

        [Display(Name = "Volunteer")]
        [StringLength(150)]
        public string VolunteerName { get; set; } = string.Empty;
    }
}
