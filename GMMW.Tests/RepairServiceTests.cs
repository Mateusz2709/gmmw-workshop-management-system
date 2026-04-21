using GMMW.Web.Data;
using GMMW.Web.Models.Domain;
using GMMW.Web.Models.Enums;
using GMMW.Web.Models.ViewModels;
using GMMW.Web.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GMMW.Tests
{
    [TestClass]
    public class RepairServiceTests
    {
        private ApplicationDbContext CreateTestDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [TestMethod]
        public async Task CreateRepairAsync_NewRepair_SetsDefaultPendingStatus()
        {
            using var context = CreateTestDbContext();

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

            await context.SaveChangesAsync();

            var service = new RepairService(context);

            var model = new RepairCreateViewModel
            {
                VehicleId = 1,
                RepairDate = DateTime.Today,
                FaultDescription = "Brake issue"
            };

            var repairId = await service.CreateRepairAsync(model);

            var createdRepair = await context.Repairs
                .FirstOrDefaultAsync(repair => repair.RepairId == repairId);

            Assert.IsNotNull(createdRepair);
            Assert.AreEqual(1, createdRepair.VehicleId);
            Assert.AreEqual(DateTime.Today, createdRepair.RepairDate);
            Assert.AreEqual("Brake issue", createdRepair.FaultDescription);
            Assert.AreEqual(string.Empty, createdRepair.WorkCarriedOut);
            Assert.AreEqual(RepairStatus.Pending, createdRepair.RepairStatus);
        }

        [TestMethod]
        public async Task UpdateRepairAsync_DifferentVehicleId_ThrowsInvalidOperationException()
        {
            using var context = CreateTestDbContext();

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

            context.Vehicles.Add(new Vehicle
            {
                VehicleId = 2,
                RegistrationNumber = "XY34 ZZZ",
                Make = "Toyota",
                Model = "Yaris",
                Year = 2020,
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

            var service = new RepairService(context);

            var model = new RepairEditViewModel
            {
                RepairId = 1,
                VehicleId = 2,
                RepairDate = DateTime.Today,
                FaultDescription = "Brake issue updated",
                WorkCarriedOut = "Pads checked",
                RepairStatus = RepairStatus.Pending
            };

            try
            {
                await service.UpdateRepairAsync(model);
                Assert.Fail("Expected InvalidOperationException was not thrown.");
            }
            catch (InvalidOperationException exception)
            {
                Assert.AreEqual("A repair cannot be reassigned to a different vehicle.", exception.Message);
            }

            var repairStillLinkedToOriginalVehicle = await context.Repairs
                .AnyAsync(repair => repair.RepairId == 1 && repair.VehicleId == 1);

            Assert.IsTrue(repairStillLinkedToOriginalVehicle);
        }
    }
}
