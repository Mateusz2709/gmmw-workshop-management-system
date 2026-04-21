using GMMW.Web.Data;
using GMMW.Web.Models.Domain;
using GMMW.Web.Models.Enums;
using GMMW.Web.Models.ViewModels;
using GMMW.Web.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GMMW.Web.Services.Implementations
{
    // Handles volunteer assignment lookup, create, update, delete, and volunteer time reporting.
    public class VolunteerWorkService : IVolunteerWorkService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public VolunteerWorkService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        // Returns the active WorkshopUser accounts that can be assigned to repairs.
        public async Task<List<ApplicationUserOptionViewModel>> GetAvailableRepairVolunteersAsync()
        {
            using var scope = _scopeFactory.CreateScope();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var workshopUsers = await userManager.GetUsersInRoleAsync("WorkshopUser");

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

        // Returns all volunteer assignments linked to one repair together with volunteer details for display.
        public async Task<List<RepairVolunteerAssignment>> GetAssignmentsByRepairIdAsync(int repairId)
        {
            using var scope = _scopeFactory.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            return await context.RepairVolunteerAssignments
                .AsNoTracking()
                .Include(assignment => assignment.ApplicationUser)
                .Where(assignment => assignment.RepairId == repairId)
                .OrderBy(assignment => assignment.ApplicationUser.FirstName)
                .ThenBy(assignment => assignment.ApplicationUser.LastName)
                .ToListAsync();
        }

        // Loads one existing assignment into the edit view model used by the repair edit workflow.
        public async Task<RepairVolunteerAssignmentEditViewModel?> GetAssignmentForEditAsync(int repairVolunteerAssignmentId)
        {
            using var scope = _scopeFactory.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            return await context.RepairVolunteerAssignments
                .AsNoTracking()
                .Where(assignment => assignment.RepairVolunteerAssignmentId == repairVolunteerAssignmentId)
                .Select(assignment => new RepairVolunteerAssignmentEditViewModel
                {
                    RepairVolunteerAssignmentId = assignment.RepairVolunteerAssignmentId,
                    RepairId = assignment.RepairId,
                    ApplicationUserId = assignment.ApplicationUserId,
                    HoursSpent = assignment.HoursSpent,
                    Notes = assignment.Notes
                })
                .FirstOrDefaultAsync();
        }

        // Creates a new volunteer assignment after validating the repair, volunteer, hours, and duplicate rules.
        public async Task<int> CreateAssignmentAsync(RepairVolunteerAssignmentCreateViewModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            using var scope = _scopeFactory.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            await EnsureRepairExistsAndIsEditableAsync(context, model.RepairId);
            await EnsureVolunteerExistsIsActiveAndIsWorkshopUserAsync(userManager, model.ApplicationUserId);

            var hoursSpent = ValidateHoursSpent(model.HoursSpent);

            var duplicateExists = await context.RepairVolunteerAssignments
                .AnyAsync(assignment =>
                    assignment.RepairId == model.RepairId &&
                    assignment.ApplicationUserId == model.ApplicationUserId);

            if (duplicateExists)
            {
                throw new InvalidOperationException("This volunteer is already assigned to the selected repair.");
            }

            var assignment = new RepairVolunteerAssignment
            {
                RepairId = model.RepairId,
                ApplicationUserId = model.ApplicationUserId,
                HoursSpent = hoursSpent,
                Notes = model.Notes?.Trim() ?? string.Empty
            };

            context.RepairVolunteerAssignments.Add(assignment);
            await context.SaveChangesAsync();

            return assignment.RepairVolunteerAssignmentId;
        }

        // Updates an existing volunteer assignment while keeping the parent repair and duplicate rules protected.
        public async Task UpdateAssignmentAsync(RepairVolunteerAssignmentEditViewModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            using var scope = _scopeFactory.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var existingAssignment = await context.RepairVolunteerAssignments
                .FirstOrDefaultAsync(assignment =>
                    assignment.RepairVolunteerAssignmentId == model.RepairVolunteerAssignmentId);

            if (existingAssignment is null)
            {
                throw new InvalidOperationException("The selected volunteer assignment could not be found.");
            }

            if (existingAssignment.RepairId != model.RepairId)
            {
                throw new InvalidOperationException("Volunteer assignments cannot be moved to another repair.");
            }

            await EnsureRepairExistsAndIsEditableAsync(context, existingAssignment.RepairId);
            await EnsureVolunteerExistsIsActiveAndIsWorkshopUserAsync(userManager, model.ApplicationUserId);

            var hoursSpent = ValidateHoursSpent(model.HoursSpent);

            var duplicateExists = await context.RepairVolunteerAssignments
                .AnyAsync(assignment =>
                    assignment.RepairVolunteerAssignmentId != model.RepairVolunteerAssignmentId &&
                    assignment.RepairId == existingAssignment.RepairId &&
                    assignment.ApplicationUserId == model.ApplicationUserId);

            if (duplicateExists)
            {
                throw new InvalidOperationException("This volunteer is already assigned to the selected repair.");
            }

            existingAssignment.ApplicationUserId = model.ApplicationUserId;
            existingAssignment.HoursSpent = hoursSpent;
            existingAssignment.Notes = model.Notes?.Trim() ?? string.Empty;

            await context.SaveChangesAsync();
        }

        // Deletes one volunteer assignment row when the repair still allows volunteer changes.
        public async Task DeleteAssignmentAsync(int repairVolunteerAssignmentId)
        {
            using var scope = _scopeFactory.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var existingAssignment = await context.RepairVolunteerAssignments
                .FirstOrDefaultAsync(assignment =>
                    assignment.RepairVolunteerAssignmentId == repairVolunteerAssignmentId);

            if (existingAssignment is null)
            {
                throw new InvalidOperationException("The selected volunteer assignment could not be found.");
            }

            await EnsureRepairExistsAndIsEditableAsync(context, existingAssignment.RepairId);

            context.RepairVolunteerAssignments.Remove(existingAssignment);
            await context.SaveChangesAsync();
        }

        // Returns the monthly volunteer-hours report, optionally narrowed to one selected volunteer.
        public async Task<List<VolunteerTimeReportItemViewModel>> GetVolunteerTimeReportAsync(int year, int month, string volunteerUserId)
        {
            using var scope = _scopeFactory.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var (startDate, endDate) = GetMonthRange(year, month);

            var query = context.RepairVolunteerAssignments
                .AsNoTracking()
                .Where(assignment =>
                    assignment.Repair.RepairDate >= startDate &&
                    assignment.Repair.RepairDate < endDate);

            if (!string.IsNullOrWhiteSpace(volunteerUserId))
            {
                query = query.Where(assignment => assignment.ApplicationUserId == volunteerUserId);
            }

            return await query
                .GroupBy(assignment => new
                {
                    assignment.ApplicationUserId,
                    assignment.ApplicationUser.FirstName,
                    assignment.ApplicationUser.LastName
                })
                .Select(group => new VolunteerTimeReportItemViewModel
                {
                    VolunteerName = ((group.Key.FirstName ?? string.Empty) + " " + (group.Key.LastName ?? string.Empty)).Trim(),
                    RepairCount = group.Count(),
                    TotalHours = group.Sum(assignment => assignment.HoursSpent),
                    AverageHoursPerRepair = group.Average(assignment => assignment.HoursSpent)
                })
                .OrderBy(item => item.VolunteerName)
                .ToListAsync();
        }

        // Confirms that the selected repair exists and still allows volunteer assignment changes.
        private async Task EnsureRepairExistsAndIsEditableAsync(ApplicationDbContext context, int repairId)
        {
            var repair = await context.Repairs
                .AsNoTracking()
                .FirstOrDefaultAsync(existingRepair => existingRepair.RepairId == repairId);

            if (repair is null)
            {
                throw new InvalidOperationException("The selected repair could not be found.");
            }

            if (repair.RepairStatus == RepairStatus.Completed)
            {
                throw new InvalidOperationException("Volunteer assignments cannot be changed for a completed repair.");
            }
        }

        // Confirms that the selected internal user exists, is active, and belongs to the WorkshopUser role.
        private async Task EnsureVolunteerExistsIsActiveAndIsWorkshopUserAsync(
            UserManager<ApplicationUser> userManager,
            string applicationUserId)
        {
            if (string.IsNullOrWhiteSpace(applicationUserId))
            {
                throw new InvalidOperationException("A volunteer must be selected.");
            }

            var volunteer = await userManager.FindByIdAsync(applicationUserId);

            if (volunteer is null)
            {
                throw new InvalidOperationException("The selected volunteer could not be found.");
            }

            if (!volunteer.IsActive)
            {
                throw new InvalidOperationException("The selected volunteer is inactive.");
            }

            var isWorkshopUser = await userManager.IsInRoleAsync(volunteer, "WorkshopUser");

            if (!isWorkshopUser)
            {
                throw new InvalidOperationException("The selected volunteer is not a valid workshop user.");
            }
        }

        // Validates the recorded volunteer hours before create or update is allowed to continue.
        private static decimal ValidateHoursSpent(decimal? hoursSpent)
        {
            if (!hoursSpent.HasValue || hoursSpent.Value <= 0)
            {
                throw new InvalidOperationException("Hours spent must be greater than zero.");
            }

            return hoursSpent.Value;
        }

        // Returns the inclusive start of the selected month and the exclusive start of the next month.
        private static (DateTime StartDate, DateTime EndDate) GetMonthRange(int year, int month)
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