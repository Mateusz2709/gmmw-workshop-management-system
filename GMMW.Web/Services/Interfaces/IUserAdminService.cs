using GMMW.Web.Models.ViewModels;

namespace GMMW.Web.Services.Interfaces
{
    // Defines the contract for admin-only internal user management operations.
    public interface IUserAdminService
    {
        // Returns one page of users matching the current admin search term.
        Task<List<UserListItemViewModel>> SearchUsersAsync(string? searchTerm, int pageNumber, int pageSize);

        // Returns the total number of users matching the current admin search term.
        Task<int> GetUserSearchCountAsync(string? searchTerm);

        // Returns one user's editable account details, or null if the user is not found.
        Task<UserEditViewModel?> GetUserByIdAsync(string userId);

        // Updates an existing internal user while applying service-layer protection rules.
        Task UpdateUserAsync(UserEditViewModel model, string currentUserId);

        // Creates a new internal user account and returns the new user ID.
        Task<string> CreateUserAsync(CreateUserViewModel model);

        // Returns one user's current role-edit model, or null if the user is not found.
        Task<UserRolesEditViewModel?> GetUserRolesByIdAsync(string userId);

        // Updates a user's role assignments while applying service-layer protection rules.
        Task UpdateUserRolesAsync(UserRolesEditViewModel model, string currentUserId);

        // Resets another user's password from the protected admin area.
        Task ResetUserPasswordAsync(AdminResetPasswordViewModel model, string currentAdminUserId);
    }
}
