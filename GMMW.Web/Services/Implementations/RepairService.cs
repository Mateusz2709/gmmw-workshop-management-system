using Microsoft.EntityFrameworkCore;
using GMMW.Web.Data;
using GMMW.Web.Models.Domain;
using GMMW.Web.Models.ViewModels;
using GMMW.Web.Services.Interfaces;
using GMMW.Web.Models.Enums;

namespace GMMW.Web.Services.Implementations
{
    // Handles repair booking, repair updates, repair lookup, and repair search logic.
    public class RepairService : IRepairService
    {
        private readonly ApplicationDbContext _context;

        public RepairService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Creates a new repair booking for an existing vehicle and returns the new RepairId.
        public async Task<int> CreateRepairAsync(RepairCreateViewModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            var faultDescription = NormalizeRequiredText(model.FaultDescription, "Fault description");

            var vehicleExists = await _context.Vehicles
                .AsNoTracking()
                .AnyAsync(vehicle => vehicle.VehicleId == model.VehicleId);

            if (!vehicleExists)
            {
                throw new InvalidOperationException("The selected vehicle could not be found.");
            }

            var repair = new Repair
            {
                VehicleId = model.VehicleId,
                RepairDate = model.RepairDate,
                FaultDescription = faultDescription,
                WorkCarriedOut = string.Empty,
                RepairStatus = RepairStatus.Pending
            };

            _context.Repairs.Add(repair);
            await _context.SaveChangesAsync();

            return repair.RepairId;
        }

        // Loads one repair by ID together with the related vehicle, owner, and assigned volunteers.
        public async Task<Repair?> GetRepairByIdAsync(int repairId)
        {
            return await _context.Repairs
                .AsNoTracking()
                .Include(repair => repair.Vehicle)
                    .ThenInclude(vehicle => vehicle.Motorist)
                .Include(repair => repair.RepairVolunteerAssignments)
                    .ThenInclude(assignment => assignment.ApplicationUser)
                .FirstOrDefaultAsync(repair => repair.RepairId == repairId);
        }

        // Returns the full repair history for one vehicle ordered from newest to oldest.
        public async Task<List<Repair>> GetRepairsByVehicleIdAsync(int vehicleId)
        {
            return await _context.Repairs
                .AsNoTracking()
                .Include(repair => repair.Vehicle)
                    .ThenInclude(vehicle => vehicle.Motorist)
                .Where(repair => repair.VehicleId == vehicleId)
                .OrderByDescending(repair => repair.RepairDate)
                .ToListAsync();
        }

        // Updates an existing repair record while keeping completed repairs and vehicle links protected.
        public async Task UpdateRepairAsync(RepairEditViewModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            var faultDescription = NormalizeRequiredText(model.FaultDescription, "Fault description");
            var workCarriedOut = NormalizeOptionalText(model.WorkCarriedOut);

            var repair = await _context.Repairs
                .FirstOrDefaultAsync(existingRepair => existingRepair.RepairId == model.RepairId);

            if (repair is null)
            {
                throw new InvalidOperationException("The selected repair could not be found.");
            }

            if (repair.RepairStatus == RepairStatus.Completed)
            {
                throw new InvalidOperationException("This repair has already been completed and can no longer be edited.");
            }

            if (repair.VehicleId != model.VehicleId)
            {
                throw new InvalidOperationException("A repair cannot be reassigned to a different vehicle.");
            }

            repair.RepairDate = model.RepairDate;
            repair.FaultDescription = faultDescription;
            repair.WorkCarriedOut = workCarriedOut;
            repair.RepairStatus = model.RepairStatus;

            await _context.SaveChangesAsync();
        }

        // Returns one page of repair history rows for a selected vehicle.
        public async Task<List<Repair>> GetRepairsByVehicleIdPagedAsync(int vehicleId, int pageNumber, int pageSize)
        {
            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            if (pageSize < 1)
            {
                pageSize = 1;
            }

            return await _context.Repairs
                .AsNoTracking()
                .Include(repair => repair.Vehicle)
                    .ThenInclude(vehicle => vehicle.Motorist)
                .Where(repair => repair.VehicleId == vehicleId)
                .OrderByDescending(repair => repair.RepairDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        // Returns the total number of repair records linked to one vehicle.
        public async Task<int> GetRepairCountByVehicleIdAsync(int vehicleId)
        {
            return await _context.Repairs
                .AsNoTracking()
                .CountAsync(repair => repair.VehicleId == vehicleId);
        }

        // Returns one page of repairs matching the selected repair ID, registration, and owner-name filters.
        public async Task<List<Repair>> SearchRepairsAsync(
            int? repairId,
            string? registrationNumber,
            string? ownerName,
            int pageNumber,
            int pageSize)
        {
            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            if (pageSize < 1)
            {
                pageSize = 1;
            }

            return await BuildRepairSearchQuery(repairId, registrationNumber, ownerName)
                .Include(repair => repair.Vehicle)
                    .ThenInclude(vehicle => vehicle.Motorist)
                .OrderByDescending(repair => repair.RepairDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        // Returns how many repairs match the selected repair ID, registration, and owner-name filters.
        public async Task<int> GetRepairSearchCountAsync(
            int? repairId,
            string? registrationNumber,
            string? ownerName)
        {
            return await BuildRepairSearchQuery(repairId, registrationNumber, ownerName)
                .CountAsync();
        }

        // Builds the shared repair search query so paging results and counts always use the same filters.
        private IQueryable<Repair> BuildRepairSearchQuery(int? repairId, string? registrationNumber, string? ownerName)
        {
            var query = _context.Repairs
                .AsNoTracking()
                .AsQueryable();

            if (repairId.HasValue)
            {
                query = query.Where(repair => repair.RepairId == repairId.Value);
            }

            if (!string.IsNullOrWhiteSpace(registrationNumber))
            {
                var normalizedRegistration = registrationNumber.Replace(" ", string.Empty).Trim().ToUpper();

                query = query.Where(repair =>
                    repair.Vehicle.RegistrationNumber.Replace(" ", string.Empty).ToUpper() == normalizedRegistration);
            }

            if (!string.IsNullOrWhiteSpace(ownerName))
            {
                var normalizedOwnerName = ownerName.Trim().ToUpper();

                query = query.Where(repair =>
                    (repair.Vehicle.Motorist.FirstName + " " + repair.Vehicle.Motorist.LastName)
                        .ToUpper()
                        .Contains(normalizedOwnerName) ||
                    repair.Vehicle.Motorist.FirstName.ToUpper().Contains(normalizedOwnerName) ||
                    repair.Vehicle.Motorist.LastName.ToUpper().Contains(normalizedOwnerName));
            }

            return query;
        }

        // Trims required text input and blocks empty values before they are saved.
        private static string NormalizeRequiredText(string? value, string fieldName)
        {
            var trimmedValue = value?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(trimmedValue))
            {
                throw new InvalidOperationException($"{fieldName} is required.");
            }

            return trimmedValue;
        }

        // Trims optional text input before it is saved.
        private static string NormalizeOptionalText(string? value)
        {
            return value?.Trim() ?? string.Empty;
        }
    }
}