namespace GMMW.Web.Models.ViewModels
{
    public class VolunteerTimeReportItemViewModel
    {
        public string VolunteerName { get; set; } = string.Empty;
        public int RepairCount { get; set; }
        public decimal TotalHours { get; set; }
        public decimal AverageHoursPerRepair { get; set; }
    }
}
