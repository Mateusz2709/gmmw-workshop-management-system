using GMMW.Web.Data;
using GMMW.Web.Models.ViewModels;
using GMMW.Web.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace GMMW.Web.Services.Implementations
{
    // Handles self-service password changes for the currently signed-in user.
    public class PasswordService : IPasswordService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public PasswordService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // Changes the current user's password after validating the basic input and Identity result.
        public async Task ChangePasswordAsync(string currentUserId, ChangePasswordViewModel model)
        {
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                throw new InvalidOperationException("The current signed-in user could not be identified.");
            }

            ArgumentNullException.ThrowIfNull(model);

            if (model.NewPassword != model.ConfirmNewPassword)
            {
                throw new InvalidOperationException("The new password and confirmation password do not match.");
            }

            var user = await _userManager.FindByIdAsync(currentUserId);

            if (user is null)
            {
                throw new InvalidOperationException("The current signed-in user could not be found.");
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (!result.Succeeded)
            {
                var errorMessage = string.Join(" ", result.Errors.Select(error => error.Description));

                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(errorMessage)
                        ? "The password could not be changed."
                        : errorMessage);
            }
        }
    }
}