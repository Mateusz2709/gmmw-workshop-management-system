using GMMW.Web.Models.Enums;

namespace GMMW.Web.Models.Domain
{
    public class Vehicle
    {
        public int VehicleId { get; set; }
        public string RegistrationNumber { get; set; } = string.Empty;
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int? Year { get; set; } 
        public VehicleType VehicleType { get; set; }
        public int MotoristId { get; set; }
        
        public Motorist Motorist { get; set; } = null!;
        public ICollection<Repair> Repairs { get; set; } = new List<Repair>(); 
    }
}
