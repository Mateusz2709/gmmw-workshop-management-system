namespace GMMW.Web.Models.ViewModels
{
    public class DashboardSummaryViewModel
    {
        public int TotalMotorists { get; set; }

        public int TotalVehicles { get; set; }

        public int ActiveRepairs { get; set; }

        public int UpcomingClasses { get; set; }

        public List<DashboardLatestRepairItemViewModel> LatestRepairs { get; set; } = new();

        public List<DashboardUpcomingClassItemViewModel> ClassesSoon { get; set; } = new();
    }
}
