using GMMW.Web.Models.Domain;
using GMMW.Web.Models.ViewModels;

namespace GMMW.Web.Services.Interfaces
{
    // Defines the contract for repair-part management and repair-part cost calculations.
    public interface IRepairPartService
    {
        // Returns all part rows linked to the selected repair.
        Task<List<RepairPart>> GetPartsByRepairIdAsync(int repairId);

        // Returns one repair part by its ID, or null if it is not found.
        Task<RepairPart?> GetRepairPartByIdAsync(int repairPartId);

        // Creates a new repair part from the create view model and returns the new part ID.
        Task<int> CreateRepairPartAsync(RepairPartCreateViewModel model);

        // Updates an existing repair part from the edit view model.
        Task UpdateRepairPartAsync(RepairPartEditViewModel model);

        // Deletes the selected repair part row.
        Task DeleteRepairPartAsync(int repairPartId);

        // Calculates the total cost of all parts linked to the selected repair.
        Task<decimal> CalculateTotalPartsCostAsync(int repairId);
    }
}
