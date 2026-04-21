using GMMW.Web.Data;
using GMMW.Web.Models.Enums;
using GMMW.Web.Models.ViewModels;
using GMMW.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GMMW.Web.Services.Implementations
{
    // Handles the read-only reporting queries used by the reports area.
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Returns the detailed monthly list of repair parts used within the selected month.
        public async Task<List<MonthlyPartsReportItemViewModel>> GetMonthlyPartsReportAsync(int month, int year)
        {
            var (startDate, endDate) = GetMonthRange(month, year);

            return await _context.RepairParts
                .AsNoTracking()
                .Where(part => part.Repair.RepairDate >= startDate && part.Repair.RepairDate < endDate)
                .OrderBy(part => part.PartType)
                .ThenBy(part => part.PartName)
                .Select(part => new MonthlyPartsReportItemViewModel
                {
                    PartName = part.PartName,
                    PartType = part.PartType,
                    Quantity = part.Quantity,
                    UnitCost = part.UnitCost,
                    LineTotal = part.Quantity * part.UnitCost
                })
                .ToListAsync();
        }

        // Returns the monthly grouped summary showing how many parts of each type were used.
        public async Task<List<MonthlyPartTypeSummaryItemViewModel>> GetMonthlyPartTypeSummaryAsync(int month, int year)
        {
            var (startDate, endDate) = GetMonthRange(month, year);

            return await _context.RepairParts
                .AsNoTracking()
                .Where(part => part.Repair.RepairDate >= startDate && part.Repair.RepairDate < endDate)
                .GroupBy(part => part.PartType)
                .Select(group => new MonthlyPartTypeSummaryItemViewModel
                {
                    PartType = group.Key,
                    TotalQuantityUsed = group.Sum(part => part.Quantity),
                    TotalCost = group.Sum(part => part.Quantity * part.UnitCost)
                })
                .OrderBy(item => item.PartType)
                .ToListAsync();
        }

        // Returns the number of completed repairs and the average completed repair cost for the selected month.
        public async Task<MonthlyRepairSummaryViewModel> GetMonthlyRepairSummaryAsync(int month, int year)
        {
            var (startDate, endDate) = GetMonthRange(month, year);

            var repairsQuery = _context.Repairs
                .AsNoTracking()
                .Where(repair =>
                    repair.RepairDate >= startDate &&
                    repair.RepairDate < endDate &&
                    repair.RepairStatus == RepairStatus.Completed);

            var repairCount = await repairsQuery.CountAsync();

            var averageRepairCost = repairCount == 0
                ? 0m
                : await repairsQuery.AverageAsync(repair => repair.TotalCost);

            return new MonthlyRepairSummaryViewModel
            {
                RepairCount = repairCount,
                AverageRepairCost = averageRepairCost
            };
        }

        // Returns the classes scheduled for one selected day.
        public async Task<List<DailyClassReportItemViewModel>> GetDailyClassesReportAsync(DateTime selectedDate)
        {
            var dayStart = selectedDate.Date;
            var dayEnd = dayStart.AddDays(1);

            return await _context.WorkshopClasses
                .AsNoTracking()
                .Where(workshopClass => workshopClass.ClassDate >= dayStart && workshopClass.ClassDate < dayEnd)
                .OrderBy(workshopClass => workshopClass.StartTime)
                .ThenBy(workshopClass => workshopClass.Title)
                .Select(workshopClass => new DailyClassReportItemViewModel
                {
                    ClassTitle = workshopClass.Title,
                    ClassDate = workshopClass.ClassDate,
                    StartTime = workshopClass.StartTime,
                    EndTime = workshopClass.EndTime,
                    VolunteerName = workshopClass.DeliveredByUser != null
                        ? workshopClass.DeliveredByUser.FirstName + " " + workshopClass.DeliveredByUser.LastName
                        : "—"
                })
                .ToListAsync();
        }

        // Returns only classes that have already been delivered by the selected volunteer in the chosen month.
        public async Task<List<VolunteerClassReportItemViewModel>> GetVolunteerClassReportAsync(int month, int year, string volunteerUserId)
        {
            if (string.IsNullOrWhiteSpace(volunteerUserId))
            {
                return new List<VolunteerClassReportItemViewModel>();
            }

            var (startDate, endDate) = GetMonthRange(month, year);
            var now = DateTime.Now;
            var today = now.Date;
            var currentTime = now.TimeOfDay;

            return await _context.WorkshopClasses
                .AsNoTracking()
                .Where(workshopClass =>
                    workshopClass.ClassDate >= startDate &&
                    workshopClass.ClassDate < endDate &&
                    workshopClass.DeliveredByUserId == volunteerUserId &&
                    (
                        workshopClass.ClassDate < today ||
                        (workshopClass.ClassDate == today && workshopClass.EndTime <= currentTime)
                    ))
                .OrderBy(workshopClass => workshopClass.ClassDate)
                .ThenBy(workshopClass => workshopClass.StartTime)
                .ThenBy(workshopClass => workshopClass.Title)
                .Select(workshopClass => new VolunteerClassReportItemViewModel
                {
                    ClassTitle = workshopClass.Title,
                    ClassDate = workshopClass.ClassDate,
                    StartTime = workshopClass.StartTime,
                    EndTime = workshopClass.EndTime,
                    VolunteerName = workshopClass.DeliveredByUser != null
                        ? workshopClass.DeliveredByUser.FirstName + " " + workshopClass.DeliveredByUser.LastName
                        : "—"
                })
                .ToListAsync();
        }

        // Returns the inclusive start of the selected month and the exclusive start of the next month.
        private static (DateTime StartDate, DateTime EndDate) GetMonthRange(int month, int year)
        {
            if (month < 1 || month > 12)
            {
                throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");
            }

            if (year < 1 || year > 9999)
            {
                throw new ArgumentOutOfRangeException(nameof(year), "Year must be a valid year.");
            }

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            return (startDate, endDate);
        }
    }
}
