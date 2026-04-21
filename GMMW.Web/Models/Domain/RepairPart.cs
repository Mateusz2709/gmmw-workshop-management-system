using GMMW.Web.Models.Enums;

namespace GMMW.Web.Models.Domain
{
    public class RepairPart
    {
        public int RepairPartId { get; set; }
        public int RepairId { get; set; }
        public string PartName { get; set; } = string.Empty;
        public PartType PartType { get; set; } = PartType.Other;
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal LineTotal => Quantity * UnitCost;
        
        public Repair Repair { get; set; } = null!;
    }
}
