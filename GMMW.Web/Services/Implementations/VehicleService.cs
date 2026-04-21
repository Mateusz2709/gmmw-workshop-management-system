using GMMW.Web.Data;
using GMMW.Web.Models.Domain;
using GMMW.Web.Models.ViewModels;
using GMMW.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GMMW.Web.Services.Implementations
{
    // Handles vehicle lookup, creation, update, delete, and motorist-linked vehicle summaries.
    public class VehicleService : IVehicleService
    {
        private readonly ApplicationDbContext _context;

        public VehicleService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Loads one vehicle by ID together with its linked motorist details.
        public async Task<Vehicle?> GetVehicleByIdAsync(int vehicleId)
        {
            return await _context.Vehicles
                .AsNoTracking()
                .Include(vehicle => vehicle.Motorist)
                .FirstOrDefaultAsync(vehicle => vehicle.VehicleId == vehicleId);
        }

        // Creates a new vehicle record after validating the owner, registration, and core vehicle details.
        public async Task<int> CreateVehicleAsync(VehicleCreateViewModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            var registrationNumber = NormalizeRequiredRegistration(model.RegistrationNumber);
            var make = NormalizeRequiredText(model.Make, "Make");
            var vehicleModel = NormalizeRequiredText(model.Model, "Model");

            var motoristExists = await _context.Motorists
                .AsNoTracking()
                .AnyAsync(motorist => motorist.MotoristId == model.MotoristId);

            if (!motoristExists)
            {
                throw new InvalidOperationException("The selected motorist could not be found.");
            }

            var registrationExists = await _context.Vehicles
                .AsNoTracking()
                .AnyAsync(vehicle =>
                    vehicle.RegistrationNumber.Replace(" ", string.Empty).ToUpper() == registrationNumber);

            if (registrationExists)
            {
                throw new InvalidOperationException("A vehicle with this registration number already exists.");
            }

            var vehicle = new Vehicle
            {
                RegistrationNumber = registrationNumber,
                Make = make,
                Model = vehicleModel,
                Year = model.Year,
                VehicleType = model.VehicleType,
                MotoristId = model.MotoristId
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            return vehicle.VehicleId;
        }

        // Updates an existing vehicle while keeping registration uniqueness and owner validity intact.
        public async Task UpdateVehicleAsync(VehicleEditViewModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            var registrationNumber = NormalizeRequiredRegistration(model.RegistrationNumber);
            var make = NormalizeRequiredText(model.Make, "Make");
            var vehicleModel = NormalizeRequiredText(model.Model, "Model");

            var existingVehicle = await _context.Vehicles
                .FirstOrDefaultAsync(vehicle => vehicle.VehicleId == model.VehicleId);

            if (existingVehicle is null)
            {
                throw new InvalidOperationException("The selected vehicle could not be found.");
            }

            var motoristExists = await _context.Motorists
                .AsNoTracking()
                .AnyAsync(motorist => motorist.MotoristId == model.MotoristId);

            if (!motoristExists)
            {
                throw new InvalidOperationException("The selected motorist could not be found.");
            }

            var registrationExists = await _context.Vehicles
                .AsNoTracking()
                .AnyAsync(vehicle =>
                    vehicle.VehicleId != model.VehicleId &&
                    vehicle.RegistrationNumber.Replace(" ", string.Empty).ToUpper() == registrationNumber);

            if (registrationExists)
            {
                throw new InvalidOperationException("A vehicle with this registration number already exists.");
            }

            existingVehicle.RegistrationNumber = registrationNumber;
            existingVehicle.Make = make;
            existingVehicle.Model = vehicleModel;
            existingVehicle.Year = model.Year;
            existingVehicle.VehicleType = model.VehicleType;
            existingVehicle.MotoristId = model.MotoristId;

            await _context.SaveChangesAsync();
        }

        // Deletes a vehicle only when it exists and no repair records are still linked to it.
        public async Task DeleteVehicleAsync(int vehicleId)
        {
            var existingVehicle = await _context.Vehicles
                .Include(vehicle => vehicle.Repairs)
                .FirstOrDefaultAsync(vehicle => vehicle.VehicleId == vehicleId);

            if (existingVehicle is null)
            {
                throw new InvalidOperationException("The selected vehicle could not be found.");
            }

            if (existingVehicle.Repairs.Any())
            {
                throw new InvalidOperationException("This vehicle cannot be deleted because repair records are still linked to it.");
            }

            _context.Vehicles.Remove(existingVehicle);
            await _context.SaveChangesAsync();
        }

        // Loads one vehicle by registration number together with its linked motorist details.
        public async Task<Vehicle?> GetVehicleByRegistrationNumberAsync(string registrationNumber)
        {
            if (string.IsNullOrWhiteSpace(registrationNumber))
            {
                return null;
            }

            var normalizedRegistration = NormalizeRegistration(registrationNumber);

            return await _context.Vehicles
                .AsNoTracking()
                .Include(vehicle => vehicle.Motorist)
                .FirstOrDefaultAsync(vehicle =>
                    vehicle.RegistrationNumber.Replace(" ", string.Empty).ToUpper() == normalizedRegistration);
        }

        // Returns the vehicle summaries shown on the motorist details page for linked-vehicle drill-down.
        public async Task<List<MotoristLinkedVehicleViewModel>> GetVehicleSummariesByMotoristIdAsync(int motoristId)
        {
            return await _context.Vehicles
                .AsNoTracking()
                .Where(vehicle => vehicle.MotoristId == motoristId)
                .Select(vehicle => new MotoristLinkedVehicleViewModel
                {
                    VehicleId = vehicle.VehicleId,
                    RegistrationNumber = vehicle.RegistrationNumber,
                    VehicleType = vehicle.VehicleType.ToString(),
                    LatestRepairDate = vehicle.Repairs
                        .OrderByDescending(repair => repair.RepairDate)
                        .Select(repair => (DateTime?)repair.RepairDate)
                        .FirstOrDefault()
                })
                .ToListAsync();
        }

        // Trims and validates required non-registration text input before saving.
        private static string NormalizeRequiredText(string? value, string fieldName)
        {
            var trimmedValue = value?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(trimmedValue))
            {
                throw new InvalidOperationException($"{fieldName} is required.");
            }

            return trimmedValue;
        }

        // Trims, de-spaces, and uppercases a registration number so duplicate checks stay consistent.
        private static string NormalizeRegistration(string? registrationNumber)
        {
            return registrationNumber?
                .Trim()
                .Replace(" ", string.Empty)
                .ToUpper() ?? string.Empty;
        }

        // Validates that a registration exists before applying the shared normalization rules.
        private static string NormalizeRequiredRegistration(string? registrationNumber)
        {
            var normalizedRegistration = NormalizeRegistration(registrationNumber);

            if (string.IsNullOrWhiteSpace(normalizedRegistration))
            {
                throw new InvalidOperationException("Registration number is required.");
            }

            return normalizedRegistration;
        }
    }
}