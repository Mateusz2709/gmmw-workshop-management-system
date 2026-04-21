using GMMW.Web.Data;
using GMMW.Web.Models.Domain;
using GMMW.Web.Models.ViewModels;
using GMMW.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GMMW.Web.Services.Implementations
{
    public class AttendanceService : IAttendanceService
    {
        private readonly ApplicationDbContext _context;

        public AttendanceService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Checks whether the selected motorist already has an attendance record for the selected class.
        public async Task<bool> AttendanceExistsAsync(int workshopClassId, int motoristId)
        {
            return await _context.ClassAttendances
                .AnyAsync(attendance => attendance.WorkshopClassId == workshopClassId
                                     && attendance.MotoristId == motoristId);
        }

        // Records attendance for one motorist against one class after validating the main business rules.
        public async Task RecordAttendanceAsync(AttendanceRecordViewModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            var workshopClass = await _context.WorkshopClasses
                .AsNoTracking()
                .FirstOrDefaultAsync(workshopClass => workshopClass.WorkshopClassId == model.WorkshopClassId);

            if (workshopClass is null)
            {
                throw new InvalidOperationException("The selected class could not be found.");
            }

            var classStart = GetClassStartDateTime(workshopClass);

            if (classStart > DateTime.Now)
            {
                throw new InvalidOperationException("Attendance can only be recorded after the class has started.");
            }

            var motoristExists = await _context.Motorists
                .AsNoTracking()
                .AnyAsync(motorist => motorist.MotoristId == model.MotoristId);

            if (!motoristExists)
            {
                throw new InvalidOperationException("The selected motorist could not be found.");
            }

            if (await AttendanceExistsAsync(model.WorkshopClassId, model.MotoristId))
            {
                throw new InvalidOperationException("This motorist is already marked as attending the selected class.");
            }

            var attendance = new ClassAttendance
            {
                WorkshopClassId = model.WorkshopClassId,
                MotoristId = model.MotoristId,
                AttendanceDate = DateTime.Now,
                Notes = model.Notes?.Trim() ?? string.Empty
            };

            _context.ClassAttendances.Add(attendance);
            await _context.SaveChangesAsync();
        }

        // Returns all attendance rows recorded for one class, ordered alphabetically by motorist name.
        public async Task<List<ClassAttendance>> GetAttendanceByClassIdAsync(int workshopClassId)
        {
            return await _context.ClassAttendances
                .AsNoTracking()
                .Include(attendance => attendance.Motorist)
                .Where(attendance => attendance.WorkshopClassId == workshopClassId)
                .OrderBy(attendance => attendance.Motorist.LastName)
                .ThenBy(attendance => attendance.Motorist.FirstName)
                .ToListAsync();
        }

        // Removes one attendance record when it exists, for example if attendance was marked by mistake.
        public async Task RemoveAttendanceAsync(int workshopClassId, int motoristId)
        {
            var attendance = await _context.ClassAttendances
                .FirstOrDefaultAsync(attendance => attendance.WorkshopClassId == workshopClassId
                                                && attendance.MotoristId == motoristId);

            if (attendance is null)
            {
                return;
            }

            _context.ClassAttendances.Remove(attendance);
            await _context.SaveChangesAsync();
        }

        // Returns one summary row per completed class in the selected month, including classes with zero attendance.
        public async Task<List<ClassAttendanceCountItemViewModel>> GetAttendanceCountByClassReportAsync(int year, int month, string? deliveredByUserId)
        {
            var classesQuery = _context.WorkshopClasses
                .Include(workshopClass => workshopClass.DeliveredByUser)
                .Include(workshopClass => workshopClass.ClassAttendances)
                .Where(workshopClass =>
                    workshopClass.ClassDate.Year == year &&
                    workshopClass.ClassDate.Month == month);

            if (!string.IsNullOrWhiteSpace(deliveredByUserId))
            {
                classesQuery = classesQuery
                    .Where(workshopClass => workshopClass.DeliveredByUserId == deliveredByUserId);
            }

            var classes = await classesQuery
                .OrderBy(workshopClass => workshopClass.ClassDate)
                .ThenBy(workshopClass => workshopClass.Title)
                .ToListAsync();

            var now = DateTime.Now;

            return classes
                // Keeps only classes whose end date and time has already passed.
                .Where(workshopClass => workshopClass.ClassDate.Date.Add(workshopClass.EndTime) <= now)
                .Select(workshopClass => new ClassAttendanceCountItemViewModel
                {
                    WorkshopClassId = workshopClass.WorkshopClassId,
                    ClassTitle = workshopClass.Title,
                    ClassDate = workshopClass.ClassDate,
                    VolunteerName = workshopClass.DeliveredByUser == null
                        ? "—"
                        : workshopClass.DeliveredByUser.FullName,
                    AttendanceCount = workshopClass.ClassAttendances.Count
                })
                .ToList();
        }

        // Returns the total number of detailed attendance rows matching the selected month and optional deliverer filter.
        public async Task<int> GetAttendanceReportCountAsync(int year, int month, string? deliveredByUserId)
        {
            return await BuildAttendanceFilterQuery(year, month, deliveredByUserId)
                .CountAsync();
        }

        // Returns one page of detailed attendance report rows for the selected month and optional deliverer filter.
        public async Task<List<AttendanceReportItemViewModel>> SearchAttendanceReportAsync(int year, int month, string? deliveredByUserId, int pageNumber, int pageSize)
        {
            var attendanceRecords = await BuildAttendanceFilterQuery(year, month, deliveredByUserId)
                .Include(attendance => attendance.Motorist)
                .Include(attendance => attendance.WorkshopClass)
                    .ThenInclude(workshopClass => workshopClass.DeliveredByUser)
                .OrderByDescending(attendance => attendance.AttendanceDate)
                .ThenBy(attendance => attendance.Motorist.LastName)
                .ThenBy(attendance => attendance.Motorist.FirstName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return attendanceRecords
                .Select(MapAttendanceReportItem)
                .ToList();
        }

        // Builds the shared attendance report query for one selected month and optional deliverer filter.
        private IQueryable<ClassAttendance> BuildAttendanceFilterQuery(int year, int month, string? deliveredByUserId)
        {
            var (monthStart, nextMonthStart) = GetMonthRange(year, month);

            var query = _context.ClassAttendances
                .AsNoTracking()
                .Where(attendance =>
                    attendance.WorkshopClass.ClassDate >= monthStart &&
                    attendance.WorkshopClass.ClassDate < nextMonthStart);

            if (!string.IsNullOrWhiteSpace(deliveredByUserId))
            {
                query = query.Where(attendance => attendance.WorkshopClass.DeliveredByUserId == deliveredByUserId);
            }

            return query;
        }

        // Maps one attendance entity into the view model used by the detailed attendance report table.
        private static AttendanceReportItemViewModel MapAttendanceReportItem(ClassAttendance attendance)
        {
            return new AttendanceReportItemViewModel
            {
                MotoristName = $"{attendance.Motorist.FirstName} {attendance.Motorist.LastName}",
                ClassTitle = attendance.WorkshopClass.Title,
                ClassDate = attendance.WorkshopClass.ClassDate,
                AttendanceDate = attendance.AttendanceDate,
                VolunteerName = attendance.WorkshopClass.DeliveredByUser is null
                    ? "—"
                    : attendance.WorkshopClass.DeliveredByUser.FullName
            };
        }

        // Combines the class date and start time into one value so attendance timing rules can be checked clearly.
        private static DateTime GetClassStartDateTime(WorkshopClass workshopClass)
        {
            return workshopClass.ClassDate.Date + workshopClass.StartTime;
        }

        // Returns the start of the selected month and the start of the following month for accurate date-range filtering.
        private static (DateTime MonthStart, DateTime NextMonthStart) GetMonthRange(int year, int month)
        {
            var monthStart = new DateTime(year, month, 1);
            return (monthStart, monthStart.AddMonths(1));
        }
    }
}