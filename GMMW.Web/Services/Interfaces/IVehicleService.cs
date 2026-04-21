using GMMW.Web.Models.Domain;
using GMMW.Web.Models.ViewModels;

namespace GMMW.Web.Services.Interfaces
{
    // Defines the contract for vehicle-related business operations.
    public interface IVehicleService
    {
        // Returns one vehicle by its ID, or null if it is not found.
        Task<Vehicle?> GetVehicleByIdAsync(int vehicleId);

        // Returns one vehicle by its registration number, or null if it is not found.
        Task<Vehicle?> GetVehicleByRegistrationNumberAsync(string registrationNumber);

        // Creates a new vehicle from the create view model and returns the new vehicle ID.
        Task<int> CreateVehicleAsync(VehicleCreateViewModel model);

        // Updates an existing vehicle from the edit view model.
        Task UpdateVehicleAsync(VehicleEditViewModel model);

        // Deletes an existing vehicle record while applying service-layer safety checks.
        Task DeleteVehicleAsync(int vehicleId);

        // Returns lightweight vehicle summaries linked to the selected motorist.
        Task<List<MotoristLinkedVehicleViewModel>> GetVehicleSummariesByMotoristIdAsync(int motoristId);
    }
}
