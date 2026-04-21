using GMMW.Web.Models.Domain;
using GMMW.Web.Models.ViewModels;

namespace GMMW.Web.Services.Interfaces
{
    // Defines the contract for repair-volunteer assignment management and volunteer time reporting.
    public interface IVolunteerWorkService
    {
        // Returns the selectable internal users who can be assigned to repair work.
        Task<List<ApplicationUserOptionViewModel>> GetAvailableRepairVolunteersAsync();

        // Returns all volunteer assignments linked to the selected repair.
        Task<List<RepairVolunteerAssignment>> GetAssignmentsByRepairIdAsync(int repairId);

        // Returns one volunteer assignment in edit-model form, or null if it is not found.
        Task<RepairVolunteerAssignmentEditViewModel?> GetAssignmentForEditAsync(int repairVolunteerAssignmentId);

        // Creates a new volunteer assignment from the create view model and returns the new assignment ID.
        Task<int> CreateAssignmentAsync(RepairVolunteerAssignmentCreateViewModel model);

        // Updates an existing volunteer assignment from the edit view model.
        Task UpdateAssignmentAsync(RepairVolunteerAssignmentEditViewModel model);

        // Deletes the selected volunteer assignment.
        Task DeleteAssignmentAsync(int repairVolunteerAssignmentId);

        // Returns the volunteer time report for one selected volunteer in the chosen month and year.
        Task<List<VolunteerTimeReportItemViewModel>> GetVolunteerTimeReportAsync(int year, int month, string volunteerUserId);
    }
}