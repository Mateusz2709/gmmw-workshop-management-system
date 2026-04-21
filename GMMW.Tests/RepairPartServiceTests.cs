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
    public class RepairPartServiceTests
    {
        private ApplicationDbContext CreateTestDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [TestMethod]
        public async Task CreateRepairPartAsync_AddPart_RecalculatesParentRepairTotalCost()
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

            context.Repairs.Add(new Repair
            {
                RepairId = 1,
                VehicleId = 1,
                RepairDate = DateTime.Today,
                FaultDescription = "Brake issue",
                WorkCarriedOut = string.Empty,
                RepairStatus = RepairStatus.Pending,
                TotalCost = 0m
            });

            await context.SaveChangesAsync();

            var service = new RepairPartService(context);

            var model = new RepairPartCreateViewModel
            {
                RepairId = 1,
                PartName = "Brake Pad Set",
                PartType = (PartType)0,
                Quantity = 2,
                UnitCost = 10m
            };

            var repairPartId = await service.CreateRepairPartAsync(model);

            var createdPart = await context.RepairParts
                .FirstOrDefaultAsync(part => part.RepairPartId == repairPartId);

            var updatedRepair = await context.Repairs
                .FirstOrDefaultAsync(repair => repair.RepairId == 1);

            Assert.IsNotNull(createdPart);
            Assert.AreEqual(1, createdPart.RepairId);
            Assert.AreEqual("Brake Pad Set", createdPart.PartName);
            Assert.AreEqual(2, createdPart.Quantity);
            Assert.AreEqual(10m, createdPart.UnitCost);

            Assert.IsNotNull(updatedRepair);
            Assert.AreEqual(20m, updatedRepair.TotalCost);
        }

        [TestMethod]
        public async Task DeleteRepairPartAsync_DeletePart_RecalculatesParentRepairTotalCost()
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

            context.Repairs.Add(new Repair
            {
                RepairId = 1,
                VehicleId = 1,
                RepairDate = DateTime.Today,
                FaultDescription = "Brake issue",
                WorkCarriedOut = string.Empty,
                RepairStatus = RepairStatus.Pending,
                TotalCost = 30m
            });

            context.RepairParts.Add(new RepairPart
            {
                RepairPartId = 1,
                RepairId = 1,
                PartName = "Brake Pad Set",
                PartType = (PartType)0,
                Quantity = 2,
                UnitCost = 10m
            });

            context.RepairParts.Add(new RepairPart
            {
                RepairPartId = 2,
                RepairId = 1,
                PartName = "Brake Fluid",
                PartType = (PartType)0,
                Quantity = 1,
                UnitCost = 10m
            });

            await context.SaveChangesAsync();

            var service = new RepairPartService(context);

            await service.DeleteRepairPartAsync(1);

            var deletedPartStillExists = await context.RepairParts
                .AnyAsync(part => part.RepairPartId == 1);

            var remainingPartExists = await context.RepairParts
                .AnyAsync(part => part.RepairPartId == 2);

            var updatedRepair = await context.Repairs
                .FirstOrDefaultAsync(repair => repair.RepairId == 1);

            Assert.IsFalse(deletedPartStillExists);
            Assert.IsTrue(remainingPartExists);
            Assert.IsNotNull(updatedRepair);
            Assert.AreEqual(10m, updatedRepair.TotalCost);
        }
    }
}
