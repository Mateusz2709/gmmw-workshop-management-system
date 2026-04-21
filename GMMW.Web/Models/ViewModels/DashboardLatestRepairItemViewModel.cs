namespace GMMW.Web.Models.ViewModels
{
    public class DashboardLatestRepairItemViewModel
    {
        public int RepairId { get; set; }

        public DateTime RepairDate { get; set; }

        public string VehicleDisplay { get; set; } = string.Empty;

        public string OwnerName { get; set; } = string.Empty;

        public string StatusText { get; set; } = string.Empty;

        public string FaultSummary { get; set; } = string.Empty;
    }
}
