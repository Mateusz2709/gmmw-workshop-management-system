using GMMW.Web.Models.ViewModels;

namespace GMMW.Web.Services.Interfaces
{
    // Defines the contract for self-service password-change operations.
    public interface IPasswordService
    {
        // Changes the password for the currently signed-in user by using the posted form model.
        Task ChangePasswordAsync(string currentUserId, ChangePasswordViewModel model);
    }
}