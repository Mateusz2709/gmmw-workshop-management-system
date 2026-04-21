using GMMW.Web.Data;
using GMMW.Web.Models.Enums;
using GMMW.Web.Models.ViewModels;
using GMMW.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using GMMW.Web.Models.Domain;

namespace GMMW.Web.Services.Implementations
{
    // Loads the summary counts and short lists shown on the dashboard page.
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Builds the full dashboard summary from the current database state.
        public async Task<DashboardSummaryViewModel> GetDashboardSummaryAsync()
        {
            var now = DateTime.Now;
            var upcomingClassesQuery = BuildUpcomingClassesQuery(now);

            return new DashboardSummaryViewModel
            {
                TotalMotorists = await _context.Motorists
                    .AsNoTracking()
                    .CountAsync(),

                TotalVehicles = await _context.Vehicles
                    .AsNoTracking()
                    .CountAsync(),

                ActiveRepairs = await _context.Repairs
                    .AsNoTracking()
                    .CountAsync(repair => repair.RepairStatus != RepairStatus.Completed),

                UpcomingClasses = await upcomingClassesQuery.CountAsync(),

                LatestRepairs = await _context.Repairs
                    .AsNoTracking()
                    .OrderByDescending(repair => repair.RepairDate)
                    .Take(5)
                    .Select(repair => new DashboardLatestRepairItemViewModel
                    {
                        RepairId = repair.RepairId,
                        RepairDate = repair.RepairDate,
                        VehicleDisplay = $"{repair.Vehicle.RegistrationNumber} - {repair.Vehicle.Make} {repair.Vehicle.Model}",
                        OwnerName = $"{repair.Vehicle.Motorist.FirstName} {repair.Vehicle.Motorist.LastName}",
                        StatusText = repair.RepairStatus.ToString(),
                        FaultSummary = repair.FaultDescription
                    })
                    .ToListAsync(),

                ClassesSoon = await upcomingClassesQuery
                    .OrderBy(workshopClass => workshopClass.ClassDate)
                    .ThenBy(workshopClass => workshopClass.StartTime)
                    .Take(5)
                    .Select(workshopClass => new DashboardUpcomingClassItemViewModel
                    {
                        WorkshopClassId = workshopClass.WorkshopClassId,
                        Title = workshopClass.Title,
                        ClassDate = workshopClass.ClassDate,
                        StartTime = workshopClass.StartTime,
                        EndTime = workshopClass.EndTime,
                        DelivererName = workshopClass.DeliveredByUser.FullName,
                        Notes = workshopClass.Description
                    })
                    .ToListAsync()
            };
        }

        // Returns only classes that have not started yet based on the stored class date and start time.
        private IQueryable<WorkshopClass> BuildUpcomingClassesQuery(DateTime now)
        {
            var today = now.Date;
            var currentTime = now.TimeOfDay;

            return _context.WorkshopClasses
                .AsNoTracking()
                .Where(workshopClass =>
                    workshopClass.ClassDate > today ||
                    (workshopClass.ClassDate == today && workshopClass.StartTime > currentTime));
        }
    }
}