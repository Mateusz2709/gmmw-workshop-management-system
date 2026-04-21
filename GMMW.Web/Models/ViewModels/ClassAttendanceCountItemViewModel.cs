using System.ComponentModel.DataAnnotations;

namespace GMMW.Web.Models.ViewModels
{
    public class ClassAttendanceCountItemViewModel
    {
        [Display(Name = "Class ID")]
        public int WorkshopClassId { get; set; }

        [Display(Name = "Class")]
        [StringLength(150)]
        public string ClassTitle { get; set; } = string.Empty;

        [Display(Name = "Class Date")]
        public DateTime ClassDate { get; set; }

        [Display(Name = "Volunteer")]
        [StringLength(150)]
        public string VolunteerName { get; set; } = string.Empty;

        [Display(Name = "Attendance Count")]
        public int AttendanceCount { get; set; }
    }
}
