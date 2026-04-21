using System.ComponentModel.DataAnnotations;
using GMMW.Web.Models.Enums;

namespace GMMW.Web.Models.ViewModels
{
    public class MonthlyPartTypeSummaryItemViewModel
    {
        [Display(Name = "Part Type")]
        public PartType PartType { get; set; }

        [Display(Name = "Total Quantity Used")]
        public int TotalQuantityUsed { get; set; }

        [Display(Name = "Total Cost")]
        public decimal TotalCost { get; set; }
    }
}