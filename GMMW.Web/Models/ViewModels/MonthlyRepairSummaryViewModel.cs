using System.ComponentModel.DataAnnotations;

namespace GMMW.Web.Models.ViewModels
{
    public class MonthlyRepairSummaryViewModel
    {
        [Display(Name = "Number of Repairs")]
        public int RepairCount { get; set; }

        [Display(Name = "Average Repair Cost")]
        public decimal AverageRepairCost { get; set; }
    }
}