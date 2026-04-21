namespace GMMW.Web.Models.ViewModels
{
    public class DashboardUpcomingClassItemViewModel
    {
        public int WorkshopClassId { get; set; }

        public string Title { get; set; } = string.Empty;

        public DateTime ClassDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public string DelivererName { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;
    }
}
