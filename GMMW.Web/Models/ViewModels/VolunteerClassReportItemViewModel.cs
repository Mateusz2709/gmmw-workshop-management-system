using System.ComponentModel.DataAnnotations;

namespace GMMW.Web.Models.ViewModels
{
    public class VolunteerClassReportItemViewModel
    {
        [Display(Name = "Class")]
        [StringLength(150)]
        public string ClassTitle { get; set; } = string.Empty;

        [Display(Name = "Class Date")]
        public DateTime ClassDate { get; set; }

        [Display(Name = "Start Time")]
        public TimeSpan StartTime { get; set; }

        [Display(Name = "End Time")]
        public TimeSpan EndTime { get; set; }

        [Display(Name = "Volunteer")]
        [StringLength(150)]
        public string VolunteerName { get; set; } = string.Empty;
    }
}
