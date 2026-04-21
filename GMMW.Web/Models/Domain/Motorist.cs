namespace GMMW.Web.Models.Domain
{
    public class Motorist
    {
        public int MotoristId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        // one Motorist → many Vehicles
        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();  
        public ICollection<ClassAttendance> ClassAttendances { get; set; } = new List<ClassAttendance>(); 
    }
}
