using GMMW.Web.Data;
using GMMW.Web.Models.Domain;
using GMMW.Web.Models.Enums;
using GMMW.Web.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GMMW.Tests
{
    [TestClass]
    public class ReportServiceTests
    {
        private ApplicationDbContext CreateTestDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [TestMethod]
        public async Task GetMonthlyRepairSummaryAsync_ReturnsCorrectRepairCountAndAverageCost()
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
                RepairDate = new DateTime(2026, 4, 5),
                FaultDescription = "Brake issue",
                WorkCarriedOut = "Brake pads replaced",
                RepairStatus = RepairStatus.Completed,
                TotalCost = 20m
            });

            context.Repairs.Add(new Repair
            {
                RepairId = 2,
                VehicleId = 1,
                RepairDate = new DateTime(2026, 4, 10),
                FaultDescription = "Oil leak",
                WorkCarriedOut = "Seal replaced",
                RepairStatus = RepairStatus.Completed,
                TotalCost = 40m
            });

            context.Repairs.Add(new Repair
            {
                RepairId = 3,
                VehicleId = 1,
                RepairDate = new DateTime(2026, 4, 15),
                FaultDescription = "Battery issue",
                WorkCarriedOut = string.Empty,
                RepairStatus = RepairStatus.Pending,
                TotalCost = 100m
            });

            context.Repairs.Add(new Repair
            {
                RepairId = 4,
                VehicleId = 1,
                RepairDate = new DateTime(2026, 5, 2),
                FaultDescription = "Tyre issue",
                WorkCarriedOut = "Tyre replaced",
                RepairStatus = RepairStatus.Completed,
                TotalCost = 60m
            });

            await context.SaveChangesAsync();

            var service = new ReportService(context);

            var result = await service.GetMonthlyRepairSummaryAsync(4, 2026);

            Assert.AreEqual(2, result.RepairCount);
            Assert.AreEqual(30m, result.AverageRepairCost);
        }

        [TestMethod]
        public async Task GetMonthlyPartsReportAsync_ReturnsCorrectTotalsForSelectedMonth()
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
                RepairDate = new DateTime(2026, 4, 5),
                FaultDescription = "Brake issue",
                WorkCarriedOut = "Brake pads replaced",
                RepairStatus = RepairStatus.Completed,
                TotalCost = 35m
            });

            context.Repairs.Add(new Repair
            {
                RepairId = 2,
                VehicleId = 1,
                RepairDate = new DateTime(2026, 5, 2),
                FaultDescription = "Tyre issue",
                WorkCarriedOut = "Tyre replaced",
                RepairStatus = RepairStatus.Completed,
                TotalCost = 100m
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
                UnitCost = 15m
            });

            context.RepairParts.Add(new RepairPart
            {
                RepairPartId = 3,
                RepairId = 2,
                PartName = "Tyre",
                PartType = (PartType)0,
                Quantity = 1,
                UnitCost = 100m
            });

            await context.SaveChangesAsync();

            var service = new ReportService(context);

            var result = await service.GetMonthlyPartsReportAsync(4, 2026);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(35m, result.Sum(item => item.LineTotal));
            Assert.IsTrue(result.Any(item => item.PartName == "Brake Pad Set" && item.LineTotal == 20m));
            Assert.IsTrue(result.Any(item => item.PartName == "Brake Fluid" && item.LineTotal == 15m));
        }

        [TestMethod]
        public async Task GetMonthlyPartTypeSummaryAsync_ReturnsCorrectGroupedTotals()
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
                RepairDate = new DateTime(2026, 4, 5),
                FaultDescription = "Brake issue",
                WorkCarriedOut = "Brake pads replaced",
                RepairStatus = RepairStatus.Completed,
                TotalCost = 55m
            });

            context.Repairs.Add(new Repair
            {
                RepairId = 2,
                VehicleId = 1,
                RepairDate = new DateTime(2026, 5, 2),
                FaultDescription = "Tyre issue",
                WorkCarriedOut = "Tyre replaced",
                RepairStatus = RepairStatus.Completed,
                TotalCost = 100m
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
                UnitCost = 15m
            });

            context.RepairParts.Add(new RepairPart
            {
                RepairPartId = 3,
                RepairId = 1,
                PartName = "Bulb",
                PartType = (PartType)1,
                Quantity = 2,
                UnitCost = 5m
            });

            context.RepairParts.Add(new RepairPart
            {
                RepairPartId = 4,
                RepairId = 2,
                PartName = "Tyre",
                PartType = (PartType)1,
                Quantity = 1,
                UnitCost = 100m
            });

            await context.SaveChangesAsync();

            var service = new ReportService(context);

            var result = await service.GetMonthlyPartTypeSummaryAsync(4, 2026);

            Assert.AreEqual(2, result.Count);

            var firstGroup = result.First(item => item.PartType == (PartType)0);
            Assert.AreEqual(3, firstGroup.TotalQuantityUsed);
            Assert.AreEqual(35m, firstGroup.TotalCost);

            var secondGroup = result.First(item => item.PartType == (PartType)1);
            Assert.AreEqual(2, secondGroup.TotalQuantityUsed);
            Assert.AreEqual(10m, secondGroup.TotalCost);
        }

        [TestMethod]
        public async Task SearchAttendanceReportAsync_MonthAndDelivererFilter_ReturnsCorrectAttendanceResults()
        {
            using var context = CreateTestDbContext();

            context.Users.Add(new ApplicationUser
            {
                Id = "user-1",
                UserName = "anna@example.com",
                Email = "anna@example.com",
                FirstName = "Anna",
                LastName = "Kowalska",
                IsActive = true
            });

            context.Users.Add(new ApplicationUser
            {
                Id = "user-2",
                UserName = "jan@example.com",
                Email = "jan@example.com",
                FirstName = "Jan",
                LastName = "Nowak",
                IsActive = true
            });

            context.Motorists.Add(new Motorist
            {
                MotoristId = 1,
                FirstName = "Adam",
                LastName = "Nowak",
                Email = "adam.nowak@example.com",
                PhoneNumber = "07123456789",
                Address = "1 Test Street"
            });

            context.Motorists.Add(new Motorist
            {
                MotoristId = 2,
                FirstName = "Ewa",
                LastName = "Kowalska",
                Email = "ewa.kowalska@example.com",
                PhoneNumber = "07999999999",
                Address = "2 Test Street"
            });

            context.WorkshopClasses.Add(new WorkshopClass
            {
                WorkshopClassId = 1,
                Title = "Basic Brake Maintenance",
                ClassDate = new DateTime(2026, 4, 5),
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(11, 0, 0),
                DeliveredByUserId = "user-1",
                Description = "April class by Anna"
            });

            context.WorkshopClasses.Add(new WorkshopClass
            {
                WorkshopClassId = 2,
                Title = "Tyre Safety Basics",
                ClassDate = new DateTime(2026, 4, 12),
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(12, 0, 0),
                DeliveredByUserId = "user-2",
                Description = "April class by Jan"
            });

            context.WorkshopClasses.Add(new WorkshopClass
            {
                WorkshopClassId = 3,
                Title = "Oil Check Basics",
                ClassDate = new DateTime(2026, 5, 3),
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(10, 0, 0),
                DeliveredByUserId = "user-1",
                Description = "May class by Anna"
            });

            context.ClassAttendances.Add(new ClassAttendance
            {
                WorkshopClassId = 1,
                MotoristId = 1,
                AttendanceDate = new DateTime(2026, 4, 5, 11, 5, 0),
                Notes = "Present"
            });

            context.ClassAttendances.Add(new ClassAttendance
            {
                WorkshopClassId = 2,
                MotoristId = 2,
                AttendanceDate = new DateTime(2026, 4, 12, 12, 5, 0),
                Notes = "Present"
            });

            context.ClassAttendances.Add(new ClassAttendance
            {
                WorkshopClassId = 3,
                MotoristId = 1,
                AttendanceDate = new DateTime(2026, 5, 3, 10, 5, 0),
                Notes = "Present"
            });

            await context.SaveChangesAsync();

            var attendanceService = new AttendanceService(context);

            var result = await attendanceService.SearchAttendanceReportAsync(2026, 4, "user-1", 1, 10);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Adam Nowak", result[0].MotoristName);
            Assert.AreEqual("Basic Brake Maintenance", result[0].ClassTitle);
            Assert.AreEqual(new DateTime(2026, 4, 5), result[0].ClassDate);
            Assert.AreEqual("Anna Kowalska", result[0].VolunteerName);
        }
    }
}