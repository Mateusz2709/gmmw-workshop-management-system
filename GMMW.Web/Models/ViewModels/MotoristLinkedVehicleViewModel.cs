namespace GMMW.Web.Models.ViewModels
{
    public class MotoristLinkedVehicleViewModel
    {
        public int VehicleId { get; set; }
        public string RegistrationNumber { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public DateTime? LatestRepairDate { get; set; }
    }
}
