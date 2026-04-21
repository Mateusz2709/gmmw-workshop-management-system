using GMMW.Web.Data;
using GMMW.Web.Models.Domain;
using GMMW.Web.Models.Enums;
using GMMW.Web.Models.ViewModels;
using GMMW.Web.Services.Implementations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GMMW.Tests
{
    [TestClass]
    public class VolunteerWorkServiceTests
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
        public async Task CreateAssignmentAsync_DuplicateVolunteerAssignment_ThrowsInvalidOperationException()
        {
            using var serviceProvider = CreateServiceProvider();
            using var scope = serviceProvider.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var createRoleResult = await roleManager.CreateAsync(new IdentityRole("WorkshopUser"));
            Assert.IsTrue(createRoleResult.Succeeded);

            context.Motorists.Add(new Motorist
            {
                MotoristId = 1,
                FirstName = "Adam",
                LastName = "Nowak",
                Email = "adam.nowak@example.com",
                PhoneNumber = "07123456789",
                Address = "1 Test Street"
            });

            context.Vehicles.Add(new Vehicle
            {
                VehicleId = 1,
                RegistrationNumber = "AB12 CDE",
                Make = "Ford",
                Model = "Focus",
                Year = 2018,
                VehicleType = VehicleType.Car,
                MotoristId = 1
            });

            context.Repairs.Add(new Repair
            {
                RepairId = 1,
                VehicleId = 1,
                RepairDate = DateTime.Today,
                FaultDescription = "Brake issue",
                WorkCarriedOut = string.Empty,
                RepairStatus = RepairStatus.Pending
            });

            await context.SaveChangesAsync();

            var volunteer = new ApplicationUser
            {
                Id = "user-1",
                UserName = "volunteer1@example.com",
                Email = "volunteer1@example.com",
                FirstName = "Anna",
                LastName = "Kowalska",
                IsActive = true
            };

            var createUserResult = await userManager.CreateAsync(volunteer);
            Assert.IsTrue(createUserResult.Succeeded);

            var addToRoleResult = await userManager.AddToRoleAsync(volunteer, "WorkshopUser");
            Assert.IsTrue(addToRoleResult.Succeeded);

            context.RepairVolunteerAssignments.Add(new RepairVolunteerAssignment
            {
                RepairId = 1,
                ApplicationUserId = "user-1",
                HoursSpent = 2m,
                Notes = "Already assigned"
            });

            await context.SaveChangesAsync();

            var service = new VolunteerWorkService(scopeFactory);

            var model = new RepairVolunteerAssignmentCreateViewModel
            {
                RepairId = 1,
                ApplicationUserId = "user-1",
                HoursSpent = 1.5m,
                Notes = "Duplicate attempt"
            };

            try
            {
                await service.CreateAssignmentAsync(model);
                Assert.Fail("Expected InvalidOperationException was not thrown.");
            }
            catch (InvalidOperationException exception)
            {
                Assert.AreEqual("This volunteer is already assigned to the selected repair.", exception.Message);
            }

            var assignmentCount = await context.RepairVolunteerAssignments.CountAsync(assignment =>
                assignment.RepairId == 1 && assignment.ApplicationUserId == "user-1");

            Assert.AreEqual(1, assignmentCount);
        }
    }
}
