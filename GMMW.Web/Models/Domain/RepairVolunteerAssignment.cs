using GMMW.Web.Data;

namespace GMMW.Web.Models.Domain;

public class RepairVolunteerAssignment
{
    public int RepairVolunteerAssignmentId { get; set; }
    public int RepairId { get; set; }
    
    public string ApplicationUserId { get; set; } = string.Empty;
    public decimal HoursSpent { get; set; }
    public string Notes { get; set; } = string.Empty;
    public Repair Repair { get; set; } = null!;
    public ApplicationUser ApplicationUser { get; set; } = null!;
}
