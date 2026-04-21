using GMMW.Web.Data;
using GMMW.Web.Models.Domain;
using GMMW.Web.Models.ViewModels;
using GMMW.Web.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GMMW.Tests
{
    [TestClass]
    public class MotoristServiceTests
    {
        private ApplicationDbContext CreateTestDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [TestMethod]
        public async Task CreateMotoristAsync_DuplicateEmail_ThrowsInvalidOperationException()
        {
            using var context = CreateTestDbContext();

            context.Motorists.Add(new Motorist
            {
                FirstName = "Adam",
                LastName = "Nowak",
                Email = "adam.nowak@example.com",
                PhoneNumber = "07123456789",
                Address = "1 Test Street"
            });

            await context.SaveChangesAsync();

            var service = new MotoristService(context);

            var model = new MotoristCreateViewModel
            {
                FirstName = "Another",
                LastName = "Person",
                Email = "adam.nowak@example.com",
                PhoneNumber = "07999999999",
                Address = "2 Test Street"
            };

            try
            {
                await service.CreateMotoristAsync(model);
                Assert.Fail("Expected InvalidOperationException was not thrown.");
            }
            catch(InvalidOperationException exception)
            {
                Assert.AreEqual("A motorist with this email address already exists.", exception.Message);
            }
        }
    }
}