using GMMW.Web.Data;
using GMMW.Web.Models.ViewModels;
using GMMW.Web.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GMMW.Web.Services.Implementations
{
    // Handles admin-only user search, user updates, role management, and internal user creation.
    public class UserAdminService : IUserAdminService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserAdminService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // Returns one page of users matching the current search term together with a readable role summary.
        public async Task<List<UserListItemViewModel>> SearchUsersAsync(string? searchTerm, int pageNumber, int pageSize)
        {
            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            if (pageSize < 1)
            {
                pageSize = 1;
            }

            var pagedUsers = await BuildUserSearchQuery(searchTerm)
                .OrderBy(user => user.LastName)
                .ThenBy(user => user.FirstName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var results = new List<UserListItemViewModel>();

            foreach (var user in pagedUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);

                results.Add(new UserListItemViewModel
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    IsActive = user.IsActive,
                    RoleSummary = roles.Count == 0
                        ? string.Empty
                        : string.Join(", ", roles.OrderBy(role => role))
                });
            }

            return results;
        }

        // Returns how many users match the current search term for pagination.
        public async Task<int> GetUserSearchCountAsync(string? searchTerm)
        {
            return await BuildUserSearchQuery(searchTerm).CountAsync();
        }

        // Builds the shared read-only user search query used by both the paged results method and the count method.
        private IQueryable<ApplicationUser> BuildUserSearchQuery(string? searchTerm)
        {
            var query = _userManager.Users
                .AsNoTracking()
                .AsQueryable();

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return query;
            }

            var trimmedSearchTerm = searchTerm.Trim();
            var likePattern = $"%{trimmedSearchTerm}%";

            return query.Where(user =>
                EF.Functions.Like(user.FirstName, likePattern) ||
                EF.Functions.Like(user.LastName, likePattern) ||
                EF.Functions.Like(user.FirstName + " " + user.LastName, likePattern) ||
                (user.Email != null && EF.Functions.Like(user.Email, likePattern)));
        }

        // Loads one user into the edit view model used by the admin edit page.
        public async Task<UserEditViewModel?> GetUserByIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            return await _userManager.Users
                .AsNoTracking()
                .Where(user => user.Id == userId)
                .Select(user => new UserEditViewModel
                {
                    UserId = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email ?? string.Empty,
                    IsActive = user.IsActive
                })
                .FirstOrDefaultAsync();
        }

        // Updates an existing user while enforcing duplicate-email and self-disable protection rules.
        public async Task UpdateUserAsync(UserEditViewModel model, string currentUserId)
        {
            ArgumentNullException.ThrowIfNull(model);

            if (string.IsNullOrWhiteSpace(model.UserId))
            {
                throw new InvalidOperationException("A valid user must be selected.");
            }

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                throw new InvalidOperationException("The current user context is missing.");
            }

            var firstName = NormalizeRequiredText(model.FirstName, "First name");
            var lastName = NormalizeRequiredText(model.LastName, "Last name");
            var email = NormalizeRequiredText(model.Email, "Email address");
            var normalizedEmail = NormalizeEmailForComparison(email);

            var user = await _userManager.FindByIdAsync(model.UserId);

            if (user is null)
            {
                throw new InvalidOperationException("The selected user could not be found.");
            }

            if (user.Id == currentUserId && !model.IsActive)
            {
                throw new InvalidOperationException("You cannot disable your own account.");
            }

            var existingUserWithEmail = await _userManager.FindByEmailAsync(email);

            if (existingUserWithEmail is not null &&
                existingUserWithEmail.Id != user.Id &&
                NormalizeEmailForComparison(existingUserWithEmail.Email) == normalizedEmail)
            {
                throw new InvalidOperationException("Another user already uses that email address.");
            }

            user.FirstName = firstName;
            user.LastName = lastName;
            user.UserName = email;
            user.Email = email;
            user.IsActive = model.IsActive;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(GetIdentityErrorMessage(result, "The user could not be updated."));
            }
        }

        // Creates a new internal user and assigns the default WorkshopUser role.
        public async Task<string> CreateUserAsync(CreateUserViewModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            var firstName = NormalizeRequiredText(model.FirstName, "First name");
            var lastName = NormalizeRequiredText(model.LastName, "Last name");
            var email = NormalizeRequiredText(model.Email, "Email address");

            if (string.IsNullOrWhiteSpace(model.Password))
            {
                throw new InvalidOperationException("A password is required.");
            }

            if (model.Password != model.ConfirmPassword)
            {
                throw new InvalidOperationException("The password and confirmation password do not match.");
            }

            var existingUserWithEmail = await _userManager.FindByEmailAsync(email);

            if (existingUserWithEmail is not null)
            {
                throw new InvalidOperationException("Another user already uses that email address.");
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                IsActive = model.IsActive
            };

            var createResult = await _userManager.CreateAsync(user, model.Password);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(GetIdentityErrorMessage(createResult, "The user could not be created."));
            }

            var roleResult = await _userManager.AddToRoleAsync(user, "WorkshopUser");

            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);

                throw new InvalidOperationException(GetIdentityErrorMessage(roleResult, "The default user role could not be assigned."));
            }

            return user.Id;
        }

        // Loads the current WorkshopUser and SuperUser role state for one selected user.
        public async Task<UserRolesEditViewModel?> GetUserRolesByIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
            {
                return null;
            }

            var isWorkshopUser = await _userManager.IsInRoleAsync(user, "WorkshopUser");
            var isSuperUser = await _userManager.IsInRoleAsync(user, "SuperUser");

            return new UserRolesEditViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                IsWorkshopUser = isWorkshopUser,
                IsSuperUser = isSuperUser
            };
        }

        // Updates the selected user's role membership while enforcing self-protection rules.
        public async Task UpdateUserRolesAsync(UserRolesEditViewModel model, string currentUserId)
        {
            ArgumentNullException.ThrowIfNull(model);

            if (string.IsNullOrWhiteSpace(model.UserId))
            {
                throw new InvalidOperationException("A valid user must be selected.");
            }

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                throw new InvalidOperationException("The current user context is missing.");
            }

            var user = await _userManager.FindByIdAsync(model.UserId);

            if (user is null)
            {
                throw new InvalidOperationException("The selected user could not be found.");
            }

            if (user.Id == currentUserId && !model.IsSuperUser)
            {
                throw new InvalidOperationException("You cannot remove your own SuperUser role.");
            }

            await UpdateRoleMembershipAsync(user, "WorkshopUser", model.IsWorkshopUser);
            await UpdateRoleMembershipAsync(user, "SuperUser", model.IsSuperUser);
        }

        // Adds or removes one role for one user so the final membership matches the requested state.
        private async Task UpdateRoleMembershipAsync(ApplicationUser user, string roleName, bool shouldBeInRole)
        {
            var isCurrentlyInRole = await _userManager.IsInRoleAsync(user, roleName);

            if (shouldBeInRole && !isCurrentlyInRole)
            {
                var addResult = await _userManager.AddToRoleAsync(user, roleName);

                if (!addResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        GetIdentityErrorMessage(addResult, $"Failed to add the user to the {roleName} role."));
                }
            }
            else if (!shouldBeInRole && isCurrentlyInRole)
            {
                var removeResult = await _userManager.RemoveFromRoleAsync(user, roleName);

                if (!removeResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        GetIdentityErrorMessage(removeResult, $"Failed to remove the user from the {roleName} role."));
                }
            }
        }

        // Trims a required text field and blocks empty values before saving.
        private static string NormalizeRequiredText(string? value, string fieldName)
        {
            var trimmedValue = value?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(trimmedValue))
            {
                throw new InvalidOperationException($"{fieldName} is required.");
            }

            return trimmedValue;
        }

        // Builds a trimmed upper-case email value for duplicate comparison.
        private static string NormalizeEmailForComparison(string? email)
        {
            return email?.Trim().ToUpper() ?? string.Empty;
        }

        // Combines Identity error messages into one readable string.
        private static string GetIdentityErrorMessage(IdentityResult result, string fallbackMessage)
        {
            var errorMessage = string.Join(" ", result.Errors.Select(error => error.Description));
            return string.IsNullOrWhiteSpace(errorMessage) ? fallbackMessage : errorMessage;
        }

        // Resets another user's password from the protected admin area.
        public async Task ResetUserPasswordAsync(AdminResetPasswordViewModel model, string currentAdminUserId)
        {
            // Stops the method immediately if the incoming form model is missing.
            ArgumentNullException.ThrowIfNull(model);

            // Prevents an admin from using the admin reset flow for their own password.
            if (model.UserId == currentAdminUserId)
            {
                // Throws a controlled error because self-service password change must use the normal account page instead.
                throw new InvalidOperationException("Use the Change Password page to change your own password.");
            }

            // Loads the target user account that the admin wants to reset.
            var user = await _userManager.FindByIdAsync(model.UserId);

            // Stops the process if the selected user cannot be found.
            if (user is null)
            {
                // Throws a controlled error because the target account no longer exists or the ID is invalid.
                throw new InvalidOperationException("The selected user could not be found.");
            }

            // Generates a secure Identity reset token for the target user.
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Uses the generated token to replace the user's password with the new value from the admin form.
            var result = await _userManager.ResetPasswordAsync(user, resetToken, model.NewPassword);

            // Stops the process if Identity rejects the reset for validation or policy reasons.
            if (!result.Succeeded)
            {
                // Combines all Identity error messages into one readable message.
                var errorMessage = string.Join(" ", result.Errors.Select(error => error.Description));

                // Throws a controlled error so the page can show a clear failure reason.
                throw new InvalidOperationException(errorMessage);
            }
        }
    }
}
