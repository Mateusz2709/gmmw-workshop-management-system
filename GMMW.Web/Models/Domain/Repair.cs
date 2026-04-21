using GMMW.Web.Models.Enums;

namespace GMMW.Web.Models.Domain
{
    public class Repair
    {
        public int RepairId { get; set; }
        public DateTime RepairDate { get; set; }
        public string FaultDescription { get; set; } = string.Empty;
        public int VehicleId { get; set; }
        public string WorkCarriedOut { get; set; } = string.Empty;
        
        public Vehicle Vehicle { get; set; } = null!;
        public decimal TotalCost { get; set; }
        public RepairStatus RepairStatus { get; set; } = RepairStatus.Pending;
        
        public ICollection<RepairPart> RepairParts { get; set; } = new List<RepairPart>();
  
        public ICollection<RepairVolunteerAssignment> RepairVolunteerAssignments { get; set; } = new List<RepairVolunteerAssignment>();
    }
}
