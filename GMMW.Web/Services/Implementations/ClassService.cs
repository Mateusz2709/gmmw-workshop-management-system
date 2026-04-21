using Microsoft.EntityFrameworkCore;
using GMMW.Web.Data;
using GMMW.Web.Models.Domain;
using GMMW.Web.Models.ViewModels;
using GMMW.Web.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using GMMW.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace GMMW.Web.Services.Implementations
{
    // Handles workshop class creation, editing, lookup, filtering, and deliverer selection rules.
    public class ClassService : IClassService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<ClassUpdatesHub> _classUpdatesHubContext;

        public ClassService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IHubContext<ClassUpdatesHub> classUpdatesHubContext)
        {
            _context = context;
            _userManager = userManager;
            _classUpdatesHubContext = classUpdatesHubContext;
        }

        // Loads one class by ID together with the linked delivering user for details or edit screens.
        public async Task<WorkshopClass?> GetWorkshopClassByIdAsync(int workshopClassId)
        {
            return await _context.WorkshopClasses
                .AsNoTracking()
                .Include(workshopClass => workshopClass.DeliveredByUser)
                .FirstOrDefaultAsync(workshopClass => workshopClass.WorkshopClassId == workshopClassId);
        }

        // Creates a new class record after validating its times and selected deliverer.
        public async Task<int> CreateWorkshopClassAsync(ClassCreateViewModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            ValidateClassTimes(model.StartTime, model.EndTime);
            await ValidateDelivererAsync(model.DeliveredByUserId);

            var workshopClass = new WorkshopClass
            {
                Title = model.Title.Trim(),
                ClassDate = model.ClassDate.Date,
                StartTime = model.StartTime!.Value,
                EndTime = model.EndTime!.Value,
                DeliveredByUserId = model.DeliveredByUserId,
                Description = model.Description?.Trim() ?? string.Empty
            };

            await _context.WorkshopClasses.AddAsync(workshopClass);
            await _context.SaveChangesAsync();
            await NotifyClassesUpdatedAsync();

            return workshopClass.WorkshopClassId;
        }

        // Updates an existing class record and only blocks timetable changes once the class has started.
        public async Task UpdateWorkshopClassAsync(ClassEditViewModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            ValidateClassTimes(model.StartTime, model.EndTime);
            await ValidateDelivererAsync(model.DeliveredByUserId);

            var workshopClass = await _context.WorkshopClasses
                .FirstOrDefaultAsync(workshopClass => workshopClass.WorkshopClassId == model.WorkshopClassId);

            if (workshopClass is null)
            {
                throw new InvalidOperationException("The selected class could not be found.");
            }

            var existingClassStartDateTime = GetClassStartDateTime(workshopClass);

            // Checks whether any timetable field is being changed compared with the stored class record.
            var isTimetableChanged =
                workshopClass.ClassDate.Date != model.ClassDate.Date ||
                workshopClass.StartTime != model.StartTime!.Value ||
                workshopClass.EndTime != model.EndTime!.Value;

            // Once the class has started, timetable fields must stay fixed, but other fields may still be updated.
            if (DateTime.Now >= existingClassStartDateTime && isTimetableChanged)
            {
                throw new InvalidOperationException("This class has already started, so its timetable fields can no longer be changed.");
            }

            workshopClass.Title = model.Title.Trim();
            workshopClass.DeliveredByUserId = model.DeliveredByUserId;
            workshopClass.Description = model.Description?.Trim() ?? string.Empty;

            // Only updates timetable values while the class has not started yet.
            if (DateTime.Now < existingClassStartDateTime)
            {
                workshopClass.ClassDate = model.ClassDate.Date;
                workshopClass.StartTime = model.StartTime!.Value;
                workshopClass.EndTime = model.EndTime!.Value;
            }

            await _context.SaveChangesAsync();
            await NotifyClassesUpdatedAsync();
        }

        // Returns the active WorkshopUser accounts that can be selected as class deliverers.
        public async Task<List<ApplicationUserOptionViewModel>> GetAvailableClassDeliverersAsync()
        {
            var workshopUsers = await _userManager.GetUsersInRoleAsync("WorkshopUser");

            return workshopUsers
                .Where(user => user.IsActive)
                .OrderBy(user => user.FirstName)
                .ThenBy(user => user.LastName)
                .Select(user => new ApplicationUserOptionViewModel
                {
                    UserId = user.Id,
                    DisplayName = user.FullName
                })
                .ToList();
        }

        // Returns how many classes exist on one selected day.
        public async Task<int> GetClassSearchCountAsync(DateTime selectedDate)
        {
            var (dayStart, dayEnd) = GetDayRange(selectedDate);

            return await _context.WorkshopClasses
                .AsNoTracking()
                .CountAsync(workshopClass => workshopClass.ClassDate >= dayStart && workshopClass.ClassDate < dayEnd);
        }

        // Returns one page of classes scheduled for the selected day.
        public async Task<List<WorkshopClass>> SearchClassesAsync(DateTime selectedDate, int pageNumber, int pageSize)
        {
            var (dayStart, dayEnd) = GetDayRange(selectedDate);

            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            if (pageSize < 1)
            {
                pageSize = 1;
            }

            return await _context.WorkshopClasses
                .AsNoTracking()
                .Include(workshopClass => workshopClass.DeliveredByUser)
                .Where(workshopClass => workshopClass.ClassDate >= dayStart && workshopClass.ClassDate < dayEnd)
                .OrderBy(workshopClass => workshopClass.StartTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        // Returns how many classes match the selected month and optional deliverer filter.
        public async Task<int> GetClassSearchCountByMonthAsync(int year, int month, string? delivererUserId)
        {
            var (monthStart, monthEnd) = GetMonthRange(year, month);

            var query = _context.WorkshopClasses
                .AsNoTracking()
                .Where(workshopClass => workshopClass.ClassDate >= monthStart && workshopClass.ClassDate < monthEnd);

            if (!string.IsNullOrWhiteSpace(delivererUserId))
            {
                query = query.Where(workshopClass => workshopClass.DeliveredByUserId == delivererUserId);
            }

            return await query.CountAsync();
        }

        // Returns one page of classes matching the selected month and optional deliverer filter.
        public async Task<List<WorkshopClass>> SearchClassesByMonthAsync(int year, int month, string? delivererUserId, int pageNumber, int pageSize)
        {
            var (monthStart, monthEnd) = GetMonthRange(year, month);

            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            if (pageSize < 1)
            {
                pageSize = 1;
            }

            var query = _context.WorkshopClasses
                .AsNoTracking()
                .Include(workshopClass => workshopClass.DeliveredByUser)
                .Where(workshopClass => workshopClass.ClassDate >= monthStart && workshopClass.ClassDate < monthEnd);

            if (!string.IsNullOrWhiteSpace(delivererUserId))
            {
                query = query.Where(workshopClass => workshopClass.DeliveredByUserId == delivererUserId);
            }

            return await query
                .OrderBy(workshopClass => workshopClass.ClassDate)
                .ThenBy(workshopClass => workshopClass.StartTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        // Validates the selected class start and end times before create or update.
        private static void ValidateClassTimes(TimeSpan? startTime, TimeSpan? endTime)
        {
            if (startTime is null || endTime is null)
            {
                throw new InvalidOperationException("Start time and end time are required.");
            }

            if (endTime <= startTime)
            {
                throw new InvalidOperationException("End time must be later than start time.");
            }
        }

        // Confirms that the selected deliverer exists, is active, and belongs to the WorkshopUser role.
        private async Task ValidateDelivererAsync(string deliveredByUserId)
        {
            if (string.IsNullOrWhiteSpace(deliveredByUserId))
            {
                throw new InvalidOperationException("A valid class deliverer must be selected.");
            }

            var user = await _userManager.FindByIdAsync(deliveredByUserId);

            if (user is null)
            {
                throw new InvalidOperationException("The selected class deliverer could not be found.");
            }

            if (!user.IsActive)
            {
                throw new InvalidOperationException("The selected class deliverer is not active.");
            }

            var isWorkshopUser = await _userManager.IsInRoleAsync(user, "WorkshopUser");

            if (!isWorkshopUser)
            {
                throw new InvalidOperationException("The selected class deliverer is not allowed to deliver classes.");
            }
        }

        // Combines the stored class date and start time so started-class rules can be checked clearly.
        private static DateTime GetClassStartDateTime(WorkshopClass workshopClass)
        {
            return workshopClass.ClassDate.Date.Add(workshopClass.StartTime);
        }

        // Returns the start of the selected month and the start of the following month.
        private static (DateTime MonthStart, DateTime MonthEnd) GetMonthRange(int year, int month)
        {
            var monthStart = new DateTime(year, month, 1);
            return (monthStart, monthStart.AddMonths(1));
        }

        // Returns the start of the selected day and the start of the following day.
        private static (DateTime DayStart, DateTime DayEnd) GetDayRange(DateTime selectedDate)
        {
            var dayStart = selectedDate.Date;
            return (dayStart, dayStart.AddDays(1));
        }

        // Sends a real-time notification to connected clients that class data has changed.
        private async Task NotifyClassesUpdatedAsync()
        {
            // Sends a message to all connected SignalR clients using the "ClassesUpdated" event name.
            await _classUpdatesHubContext.Clients.All.SendAsync("ClassesUpdated");
        }
    }
}