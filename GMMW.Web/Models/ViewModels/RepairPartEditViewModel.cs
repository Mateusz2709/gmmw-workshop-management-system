using System.ComponentModel.DataAnnotations;
using GMMW.Web.Models.Enums;

namespace GMMW.Web.Models.ViewModels
{
    public class RepairPartEditViewModel
    {
        public int RepairPartId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "A valid repair must be selected.")]
        public int RepairId { get; set; }

        [Required(ErrorMessage = "Part name is required.")]
        [StringLength(100, ErrorMessage = "Part name cannot be longer than 100 characters.")]
        public string PartName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Part type is required.")]
        public PartType PartType { get; set; } = PartType.Other;

        [Required(ErrorMessage = "Quantity is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int? Quantity { get; set; }

        [Required(ErrorMessage = "Unit cost is required.")]
        [Range(typeof(decimal), "0.01", "999999.99", ErrorMessage = "Unit cost must be greater than 0.")]
        public decimal? UnitCost { get; set; }
    }
}