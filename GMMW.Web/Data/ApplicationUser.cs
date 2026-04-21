using GMMW.Web.Models.Domain;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace GMMW.Web.Data
{
    // Extends the default Identity user with the extra fields needed by this project.
    public class ApplicationUser : IdentityUser
    {
        // Stores the user's first name.
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        // Stores the user's last name.
        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        // Tracks whether the account is currently active.
        public bool IsActive { get; set; } = true;

        // Returns a full display name built from the stored first and last name.
        public string FullName => $"{FirstName} {LastName}".Trim();

        // Links the user to any workshop classes they deliver.
        public ICollection<WorkshopClass> WorkshopClasses { get; set; } = new List<WorkshopClass>();

        // Links the user to any repair-volunteer assignment records.
        public ICollection<RepairVolunteerAssignment> RepairVolunteerAssignments { get; set; } = new List<RepairVolunteerAssignment>();
    }
}