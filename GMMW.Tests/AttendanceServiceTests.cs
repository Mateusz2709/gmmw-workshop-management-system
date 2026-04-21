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
    public class AttendanceServiceTests
    {
        private ApplicationDbContext CreateTestDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [TestMethod]
        public async Task RecordAttendanceAsync_DuplicateAttendance_ThrowsInvalidOperationException()
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

            context.WorkshopClasses.Add(new WorkshopClass
            {
                WorkshopClassId = 1,
                Title = "Basic Brake Maintenance",
                ClassDate = DateTime.Today.AddDays(-1),
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(11, 0, 0),
                DeliveredByUserId = "user-1",
                Description = "Test class"
            });

            context.ClassAttendances.Add(new ClassAttendance
            {
                WorkshopClassId = 1,
                MotoristId = 1,
                AttendanceDate = DateTime.Now.AddMinutes(-10),
                Notes = "Already marked present"
            });

            await context.SaveChangesAsync();

            var service = new AttendanceService(context);

            var model = new AttendanceRecordViewModel
            {
                WorkshopClassId = 1,
                MotoristId = 1,
                Notes = "Duplicate attempt"
            };

            try
            {
                await service.RecordAttendanceAsync(model);
                Assert.Fail("Expected InvalidOperationException was not thrown.");
            }
            catch (InvalidOperationException exception)
            {
                Assert.AreEqual("This motorist is already marked as attending the selected class.", exception.Message);
            }

            var attendanceCount = await context.ClassAttendances.CountAsync(attendance =>
                attendance.WorkshopClassId == 1 && attendance.MotoristId == 1);

            Assert.AreEqual(1, attendanceCount);
        }
    }
}
