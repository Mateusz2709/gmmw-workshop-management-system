namespace GMMW.Web.Models.Domain
{
    public class ClassAttendance
    {
        public int ClassAttendanceId { get; set; }
        public int WorkshopClassId { get; set; }
        public int MotoristId { get; set; }
        public DateTime AttendanceDate { get; set; }
        public string Notes { get; set; } = string.Empty;

        public WorkshopClass WorkshopClass { get; set; } = null!; 
        public Motorist Motorist { get; set; } = null!; 

    }
}
