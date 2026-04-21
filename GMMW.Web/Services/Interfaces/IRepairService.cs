using GMMW.Web.Models.Domain;
using GMMW.Web.Models.ViewModels;

namespace GMMW.Web.Services.Interfaces
{
    // Defines the contract for repair booking, repair updates, and repair-search operations.
    public interface IRepairService
    {
        // Returns all repairs linked to the selected vehicle.
        Task<List<Repair>> GetRepairsByVehicleIdAsync(int vehicleId);

        // Returns one repair by its ID, or null if it is not found.
        Task<Repair?> GetRepairByIdAsync(int repairId);

        // Creates a new repair from the booking view model and returns the new repair ID.
        Task<int> CreateRepairAsync(RepairCreateViewModel model);

        // Updates an existing repair from the edit view model.
        Task UpdateRepairAsync(RepairEditViewModel model);

        // Returns one page of repairs linked to the selected vehicle.
        Task<List<Repair>> GetRepairsByVehicleIdPagedAsync(int vehicleId, int pageNumber, int pageSize);

        // Returns the total number of repairs linked to the selected vehicle.
        Task<int> GetRepairCountByVehicleIdAsync(int vehicleId);

        // Returns one page of repairs matching the current repair-search filters.
        Task<List<Repair>> SearchRepairsAsync(
            int? repairId,
            string? registrationNumber,
            string? ownerName,
            int pageNumber,
            int pageSize);

        // Returns the total number of repairs matching the current repair-search filters.
        Task<int> GetRepairSearchCountAsync(
            int? repairId,
            string? registrationNumber,
            string? ownerName);
    }
}
