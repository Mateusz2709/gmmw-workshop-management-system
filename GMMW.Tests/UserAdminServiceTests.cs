using GMMW.Web.Data;
using GMMW.Web.Models.Domain;
using GMMW.Web.Models.ViewModels;
using GMMW.Web.Services.Implementations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GMMW.Tests
{
    [TestClass]
    public class UserAdminServiceTests
    {
        private ServiceProvider CreateServiceProvider()
        {
            var services = new ServiceCollection();
            var databaseName = Guid.NewGuid().ToString();

            services.AddLogging();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));

            services.AddIdentityCore<ApplicationUser>()
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            return services.BuildServiceProvider();
        }

        [TestMethod]
        public async Task UpdateUserAsync_SelfDisable_ThrowsInvalidOperationException()
        {
            using var serviceProvider = CreateServiceProvider();
            using var scope = serviceProvider.CreateScope();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var adminUser = new ApplicationUser
            {
                Id = "admin-1",
                UserName = "admin@example.com",
                Email = "admin@example.com",
                FirstName = "Admin",
                LastName = "User",
                IsActive = true
            };

            var createUserResult = await userManager.CreateAsync(adminUser);
            Assert.IsTrue(createUserResult.Succeeded);

            var service = new UserAdminService(userManager);

            var model = new UserEditViewModel
            {
                UserId = "admin-1",
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@example.com",
                IsActive = false
            };

            try
            {
                await service.UpdateUserAsync(model, "admin-1");
                Assert.Fail("Expected InvalidOperationException was not thrown.");
            }
            catch (InvalidOperationException exception)
            {
                Assert.AreEqual("You cannot disable your own account.", exception.Message);
            }

            var reloadedUser = await userManager.FindByIdAsync("admin-1");

            Assert.IsNotNull(reloadedUser);
            Assert.IsTrue(reloadedUser.IsActive);
        }

        [TestMethod]
        public async Task UpdateUserRolesAsync_SelfRemoveSuperUserRole_ThrowsInvalidOperationException()
        {
            using var serviceProvider = CreateServiceProvider();
            using var scope = serviceProvider.CreateScope();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var createSuperUserRoleResult = await roleManager.CreateAsync(new IdentityRole("SuperUser"));
            Assert.IsTrue(createSuperUserRoleResult.Succeeded);

            var createWorkshopUserRoleResult = await roleManager.CreateAsync(new IdentityRole("WorkshopUser"));
            Assert.IsTrue(createWorkshopUserRoleResult.Succeeded);

            var adminUser = new ApplicationUser
            {
                Id = "admin-1",
                UserName = "admin@example.com",
                Email = "admin@example.com",
                FirstName = "Admin",
                LastName = "User",
                IsActive = true
            };

            var createUserResult = await userManager.CreateAsync(adminUser);
            Assert.IsTrue(createUserResult.Succeeded);

            var addSuperUserRoleResult = await userManager.AddToRoleAsync(adminUser, "SuperUser");
            Assert.IsTrue(addSuperUserRoleResult.Succeeded);

            var addWorkshopUserRoleResult = await userManager.AddToRoleAsync(adminUser, "WorkshopUser");
            Assert.IsTrue(addWorkshopUserRoleResult.Succeeded);

            var service = new UserAdminService(userManager);

            var model = new UserRolesEditViewModel
            {
                UserId = "admin-1",
                FullName = "Admin User",
                Email = "admin@example.com",
                IsWorkshopUser = true,
                IsSuperUser = false
            };

            try
            {
                await service.UpdateUserRolesAsync(model, "admin-1");
                Assert.Fail("Expected InvalidOperationException was not thrown.");
            }
            catch (InvalidOperationException exception)
            {
                Assert.AreEqual("You cannot remove your own SuperUser role.", exception.Message);
            }

            var reloadedUser = await userManager.FindByIdAsync("admin-1");
            Assert.IsNotNull(reloadedUser);

            var isStillSuperUser = await userManager.IsInRoleAsync(reloadedUser, "SuperUser");
            Assert.IsTrue(isStillSuperUser);
        }
    }
}
