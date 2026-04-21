using System.ComponentModel.DataAnnotations;
using GMMW.Web.Models.Enums;

namespace GMMW.Web.Models.ViewModels
{
    public class MonthlyPartsReportItemViewModel
    {
        [Display(Name = "Part Name")]
        [StringLength(150)]
        public string PartName { get; set; } = string.Empty;

        [Display(Name = "Part Type")]
        public PartType PartType { get; set; }

        [Display(Name = "Quantity")]
        public int Quantity { get; set; }

        [Display(Name = "Unit Cost")]
        public decimal UnitCost { get; set; }

        [Display(Name = "Line Total")]
        public decimal LineTotal { get; set; }
    }
}