using System.ComponentModel.DataAnnotations;

namespace GMMW.Web.Models.ViewModels
{
    public class ClassCreateViewModel
    {
        [Required(ErrorMessage = "Class title is required.")]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Class date is required.")]
        public DateTime ClassDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Start time is required.")]
        public TimeSpan? StartTime { get; set; }

        [Required(ErrorMessage = "End time is required.")]
        public TimeSpan? EndTime { get; set; }

        [Required(ErrorMessage = "A delivering user must be selected.")]
        public string DeliveredByUserId { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;
    }
}