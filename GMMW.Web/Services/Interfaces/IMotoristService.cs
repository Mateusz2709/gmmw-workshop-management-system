using GMMW.Web.Models.Domain; // Imports domain entity classes such as Motorist for service method signatures.
using GMMW.Web.Models.ViewModels; // Imports the motorist form view models used for create and edit service operations.
namespace GMMW.Web.Services.Interfaces
{
    // Defines the contract for motorist-related business operations.
    public interface IMotoristService
    {

        // Searches motorists by a text term such as name, email, or phone number.
        Task<List<Motorist>> SearchMotoristsAsync(string? searchTerm, int pageNumber, int pageSize);

        // Gets one motorist record by its ID for details or edit loading.
        Task<Motorist?> GetMotoristByIdAsync(int motoristId);

        // Creates a new motorist record from the validated create form and returns its new ID.
        Task<int> CreateMotoristAsync(MotoristCreateViewModel model);

        // Updates an existing motorist record using the validated edit form data.
        Task UpdateMotoristAsync(MotoristEditViewModel model);

        // Deletes a motorist record by ID when it is safe to remove it.
        Task DeleteMotoristAsync(int motoristId);

        Task<int> GetMotoristSearchCountAsync(string? searchTerm);
    }
}
