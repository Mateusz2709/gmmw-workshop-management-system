using GMMW.Web.Data;

namespace GMMW.Web.Models.Domain
{
    public class WorkshopClass
    {
        public int WorkshopClassId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime ClassDate { get; set; }
        public TimeSpan StartTime { get; set; }  
        public TimeSpan EndTime { get; set; }
        public string DeliveredByUserId { get; set; } = string.Empty; 
        public ApplicationUser DeliveredByUser { get; set; } = null!; 
        public string Description { get; set; } = string.Empty;
    
        public ICollection<ClassAttendance> ClassAttendances { get; set; } = new List<ClassAttendance>();
    }
}
