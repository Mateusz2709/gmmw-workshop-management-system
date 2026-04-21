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
    public class VehicleServiceTests
    {
        private ApplicationDbContext CreateTestDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [TestMethod]
        public async Task CreateVehicleAsync_DuplicateRegistration_ThrowsInvalidOperationException()
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
                RegistrationNumber = "AB12 CDE",
                Make = "Ford",
                Model = "Focus",
                Year = 2018,
                VehicleType = VehicleType.Car,
                MotoristId = 1
            });

            await context.SaveChangesAsync();

            var service = new VehicleService(context);

            var model = new VehicleCreateViewModel
            {
                RegistrationNumber = "AB12 CDE",
                Make = "Toyota",
                Model = "Yaris",
                Year = 2020,
                VehicleType = VehicleType.Car,
                MotoristId = 1
            };

            try
            {
                await service.CreateVehicleAsync(model);
                Assert.Fail("Expected InvalidOperationException was not thrown.");
            }
            catch (InvalidOperationException exception)
            {
                Assert.AreEqual("A vehicle with this registration number already exists.", exception.Message);
            }
        }

        [TestMethod]
        public async Task UpdateVehicleAsync_DuplicateRegistrationOnAnotherVehicle_ThrowsInvalidOperationException()
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

            await context.SaveChangesAsync();

            var service = new VehicleService(context);

            var model = new VehicleEditViewModel
            {
                VehicleId = 2,
                RegistrationNumber = "AB12 CDE",
                Make = "Toyota",
                Model = "Yaris",
                Year = 2020,
                VehicleType = VehicleType.Car,
                MotoristId = 1
            };

            try
            {
                await service.UpdateVehicleAsync(model);
                Assert.Fail("Expected InvalidOperationException was not thrown.");
            }
            catch (InvalidOperationException exception)
            {
                Assert.AreEqual("A vehicle with this registration number already exists.", exception.Message);
            }
        }

        [TestMethod]
        public async Task DeleteVehicleAsync_LinkedRepairs_ThrowsInvalidOperationException()
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
                VehicleId = 1
            });

            await context.SaveChangesAsync();

            var service = new VehicleService(context);

            try
            {
                await service.DeleteVehicleAsync(1);
                Assert.Fail("Expected InvalidOperationException was not thrown.");
            }
            catch (InvalidOperationException exception)
            {
                Assert.AreEqual("This vehicle cannot be deleted because repair records are still linked to it.", exception.Message);
            }

            var vehicleStillExists = await context.Vehicles.AnyAsync(vehicle => vehicle.VehicleId == 1);
            Assert.IsTrue(vehicleStillExists);
        }
    }
}
