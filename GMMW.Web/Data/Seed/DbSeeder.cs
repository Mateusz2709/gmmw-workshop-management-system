namespace GMMW.Web.Data.Seed;

using Microsoft.AspNetCore.Identity; // Lets this file use Identity classes like RoleManager, UserManager, and IdentityRole.
using Microsoft.Extensions.DependencyInjection;

public static class DbSeeder  // static means I do not need to create an object like: var seeder = new DbSeeder(); Instead, we will call the method directly from the class later, something like: DbSeeder.SomeMethod();
{
    // This method seeds the initial Identity data needed by the application,
    // including the default roles and the default admin user.
    public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
    {
        // Create a service scope so scoped services like UserManager and RoleManager can be resolved safely during startup seeding.
        // using var means: create the variable and automatically clean it up when the method finishes
        // serviceProvider.CreateScope() - This asks ASP.NET Core to create a new dependency-injection scope.
        using var scope = serviceProvider.CreateScope();

        // Get the Identity role manager so roles like SuperUser and WorkshopUser can be checked and created.
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Get the Identity user manager so the default admin account can be checked, created, and assigned to a role.
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Define the Identity roles that must exist in the system.
        var roles = new[] { "SuperUser", "WorkshopUser" };

        // Loop through each required role so we can check whether it exists and create it if needed.
        foreach (var role in roles)
        {
            // Only create the role if it does not already exist in the database.
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Define the default admin account that will be seeded for initial system access.
        var adminEmail = "admin@gmmw.local";
        var adminPassword = "Admin123!";

        // Check whether the default admin user already exists in the Identity tables.
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        // If the admin user does not exist yet, create it now.
        if (adminUser == null)
        {
            // Prepare a new Identity user object for the default admin account.
            adminUser = new ApplicationUser
            {
                // Use the admin email as the username for simple login setup.
                UserName = adminEmail,

                // Store the correct email address on the account.
                Email = adminEmail,

                // Store the admin user's first name.
                FirstName = "System",

                // Store the admin user's last name.
                LastName = "Administrator",

                // Mark the seeded admin as active.
                IsActive = true
            };

            // Actually create the admin user in ASP.NET Identity and save it to the database.
            // Identity will hash the password for us - it is not stored as plain text.
            var createAdminResult = await userManager.CreateAsync(adminUser, adminPassword);

            // If user creation failed, stop startup and show a clear error instead of failing silently.
            if (!createAdminResult.Succeeded)
            {
                throw new Exception("Failed to create the default admin user.");
            }
        }
        else
        {
            // Tracks whether any missing admin fields were fixed.
            var adminNeedsUpdate = false;

            // Add a first name if the existing seeded admin still has an empty one.
            if (string.IsNullOrWhiteSpace(adminUser.FirstName))
            {
                adminUser.FirstName = "System";
                adminNeedsUpdate = true;
            }

            // Add a last name if the existing seeded admin still has an empty one.
            if (string.IsNullOrWhiteSpace(adminUser.LastName))
            {
                adminUser.LastName = "Administrator";
                adminNeedsUpdate = true;
            }

            // Make sure the existing seeded admin stays active.
            if (!adminUser.IsActive)
            {
                adminUser.IsActive = true;
                adminNeedsUpdate = true;
            }

            // Save the updated admin values only if something actually changed.
            if (adminNeedsUpdate)
            {
                var updateAdminResult = await userManager.UpdateAsync(adminUser);

                // Stop startup if the existing admin could not be updated.
                if (!updateAdminResult.Succeeded)
                {
                    throw new Exception("Failed to update the default admin user.");
                }
            }
        }

        // Even if the admin user already existed before,
        // make sure it is definitely assigned to the SuperUser role.
        if (!await userManager.IsInRoleAsync(adminUser, "SuperUser"))
        {
            // Try to add the admin user to the SuperUser role.
            var addToRoleResult = await userManager.AddToRoleAsync(adminUser, "SuperUser");

            // If role assignment failed, stop startup and show a clear error.
            if (!addToRoleResult.Succeeded)
            {
                throw new Exception("Failed to assign the SuperUser role to the default admin user.");
            }
        }

        // Define the default workshop user account used for non-admin access testing.
        var workshopEmail = "workshop@gmmw.local";
        var workshopPassword = "Workshop123!";

        // Check whether the default workshop user already exists in the Identity tables.
        var workshopUser = await userManager.FindByEmailAsync(workshopEmail);

        // If the workshop user does not exist yet, create it now.
        if (workshopUser == null)
        {
            // Prepare a new Identity user object for the default workshop account.
            workshopUser = new ApplicationUser
            {
                // Use the workshop email as the username for simple login setup.
                UserName = workshopEmail,

                // Store the correct email address on the account.
                Email = workshopEmail,

                // Store the workshop user's first name.
                FirstName = "Workshop",

                // Store the workshop user's last name.
                LastName = "User",

                // Mark the seeded workshop user as active.
                IsActive = true
            };

            // Actually create the workshop user in ASP.NET Identity and save it to the database.
            var createWorkshopResult = await userManager.CreateAsync(workshopUser, workshopPassword);

            // If user creation failed, stop startup and show a clear error instead of failing silently.
            if (!createWorkshopResult.Succeeded)
            {
                throw new Exception("Failed to create the default workshop user.");
            }
        }
        else
        {
            // Tracks whether any missing workshop fields were fixed.
            var workshopNeedsUpdate = false;

            // Add a first name if the existing seeded workshop user still has an empty one.
            if (string.IsNullOrWhiteSpace(workshopUser.FirstName))
            {
                workshopUser.FirstName = "Workshop";
                workshopNeedsUpdate = true;
            }

            // Add a last name if the existing seeded workshop user still has an empty one.
            if (string.IsNullOrWhiteSpace(workshopUser.LastName))
            {
                workshopUser.LastName = "User";
                workshopNeedsUpdate = true;
            }

            // Make sure the existing seeded workshop user stays active.
            if (!workshopUser.IsActive)
            {
                workshopUser.IsActive = true;
                workshopNeedsUpdate = true;
            }

            // Save the updated workshop values only if something actually changed.
            if (workshopNeedsUpdate)
            {
                var updateWorkshopResult = await userManager.UpdateAsync(workshopUser);

                // Stop startup if the existing workshop user could not be updated.
                if (!updateWorkshopResult.Succeeded)
                {
                    throw new Exception("Failed to update the default workshop user.");
                }
            }
        }

        // Make sure the workshop user is assigned to the WorkshopUser role.
        if (!await userManager.IsInRoleAsync(workshopUser, "WorkshopUser"))
        {
            var addWorkshopRoleResult = await userManager.AddToRoleAsync(workshopUser, "WorkshopUser");

            if (!addWorkshopRoleResult.Succeeded)
            {
                throw new Exception("Failed to assign the WorkshopUser role to the default workshop user.");
            }
        }
    }
}