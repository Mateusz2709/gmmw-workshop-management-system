using GMMW.Web.Models.Domain;
using GMMW.Web.Models.ViewModels;

namespace GMMW.Web.Services.Interfaces
{
    // Defines the contract for workshop-class-related business operations.
    public interface IClassService
    {
        // Returns one workshop class by its ID, or null if it is not found.
        Task<WorkshopClass?> GetWorkshopClassByIdAsync(int workshopClassId);

        // Creates a new workshop class from the create view model and returns the new class ID.
        Task<int> CreateWorkshopClassAsync(ClassCreateViewModel model);

        // Updates an existing workshop class from the edit view model.
        Task UpdateWorkshopClassAsync(ClassEditViewModel model);

        // Returns the selectable delivering users for the class create and edit forms.
        Task<List<ApplicationUserOptionViewModel>> GetAvailableClassDeliverersAsync();

        // Returns the total number of classes matching the selected day.
        Task<int> GetClassSearchCountAsync(DateTime selectedDate);

        // Returns one page of classes matching the selected day.
        Task<List<WorkshopClass>> SearchClassesAsync(DateTime selectedDate, int pageNumber, int pageSize);

        // Returns the total number of classes matching the selected month and optional deliverer.
        Task<int> GetClassSearchCountByMonthAsync(int year, int month, string? delivererUserId);

        // Returns one page of classes matching the selected month and optional deliverer.
        Task<List<WorkshopClass>> SearchClassesByMonthAsync(
            int year,
            int month,
            string? delivererUserId,
            int pageNumber,
            int pageSize);
    }
}