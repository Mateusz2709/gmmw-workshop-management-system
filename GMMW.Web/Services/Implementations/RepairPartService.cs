using GMMW.Web.Data;
using GMMW.Web.Models.Domain;
using GMMW.Web.Models.Enums;
using GMMW.Web.Models.ViewModels;
using GMMW.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GMMW.Web.Services.Implementations
{
    // Handles repair part lookup, validation, create, update, delete, and parent total-cost refresh logic.
    public class RepairPartService : IRepairPartService
    {
        private readonly ApplicationDbContext _context;

        public RepairPartService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Returns all part rows linked to one repair in a stable display order.
        public async Task<List<RepairPart>> GetPartsByRepairIdAsync(int repairId)
        {
            return await _context.RepairParts
                .AsNoTracking()
                .Where(part => part.RepairId == repairId)
                .OrderBy(part => part.PartName)
                .ToListAsync();
        }

        // Returns one repair part row by ID, or null when it does not exist.
        public async Task<RepairPart?> GetRepairPartByIdAsync(int repairPartId)
        {
            return await _context.RepairParts
                .AsNoTracking()
                .FirstOrDefaultAsync(part => part.RepairPartId == repairPartId);
        }

        // Creates a new repair part row after validating the input and confirming the parent repair can still be changed.
        public async Task<int> CreateRepairPartAsync(RepairPartCreateViewModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            var partName = ValidateRepairPartInput(model.PartName, model.Quantity, model.UnitCost);
            await EnsureRepairAllowsPartChangesAsync(model.RepairId);

            var repairPart = new RepairPart
            {
                RepairId = model.RepairId,
                PartName = partName,
                PartType = model.PartType,
                Quantity = model.Quantity!.Value,
                UnitCost = model.UnitCost!.Value
            };

            _context.RepairParts.Add(repairPart);
            await _context.SaveChangesAsync();

            await UpdateRepairTotalCostAsync(model.RepairId);

            return repairPart.RepairPartId;
        }

        // Updates an existing repair part row after validating the input and confirming the parent repair can still be changed.
        public async Task UpdateRepairPartAsync(RepairPartEditViewModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            var partName = ValidateRepairPartInput(model.PartName, model.Quantity, model.UnitCost);

            var repairPart = await _context.RepairParts
                .FirstOrDefaultAsync(part => part.RepairPartId == model.RepairPartId);

            if (repairPart is null)
            {
                throw new InvalidOperationException("The selected repair part could not be found.");
            }

            if (repairPart.RepairId != model.RepairId)
            {
                throw new InvalidOperationException("A repair part cannot be reassigned to a different repair.");
            }

            await EnsureRepairAllowsPartChangesAsync(model.RepairId);

            repairPart.PartName = partName;
            repairPart.PartType = model.PartType;
            repairPart.Quantity = model.Quantity!.Value;
            repairPart.UnitCost = model.UnitCost!.Value;

            await _context.SaveChangesAsync();
            await UpdateRepairTotalCostAsync(model.RepairId);
        }

        // Deletes a repair part row when it exists and the parent repair is still editable.
        public async Task DeleteRepairPartAsync(int repairPartId)
        {
            var repairPart = await _context.RepairParts
                .FirstOrDefaultAsync(part => part.RepairPartId == repairPartId);

            if (repairPart is null)
            {
                throw new InvalidOperationException("The selected repair part could not be found.");
            }

            await EnsureRepairAllowsPartChangesAsync(repairPart.RepairId);

            var repairId = repairPart.RepairId;

            _context.RepairParts.Remove(repairPart);
            await _context.SaveChangesAsync();

            await UpdateRepairTotalCostAsync(repairId);
        }

        // Calculates the total cost of all parts linked to one repair using database-side summing logic.
        public async Task<decimal> CalculateTotalPartsCostAsync(int repairId)
        {
            return await _context.RepairParts
                .AsNoTracking()
                .Where(part => part.RepairId == repairId)
                .SumAsync(part => (decimal?)(part.Quantity * part.UnitCost)) ?? 0m;
        }

        // Validates the shared repair part input used by both create and edit operations.
        private static string ValidateRepairPartInput(string? partName, int? quantity, decimal? unitCost)
        {
            var trimmedPartName = partName?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(trimmedPartName))
            {
                throw new ArgumentException("Part name is required.");
            }

            if (!quantity.HasValue)
            {
                throw new ArgumentException("Quantity is required.");
            }

            if (quantity.Value <= 0)
            {
                throw new ArgumentException("Quantity must be greater than 0.");
            }

            if (!unitCost.HasValue)
            {
                throw new ArgumentException("Unit cost is required.");
            }

            if (unitCost.Value <= 0)
            {
                throw new ArgumentException("Unit cost must be greater than 0.");
            }

            return trimmedPartName;
        }

        // Confirms that the parent repair exists and is not already completed before allowing part changes.
        private async Task EnsureRepairAllowsPartChangesAsync(int repairId)
        {
            var repair = await _context.Repairs
                .FirstOrDefaultAsync(existingRepair => existingRepair.RepairId == repairId);

            if (repair is null)
            {
                throw new InvalidOperationException("The selected repair could not be found.");
            }

            if (repair.RepairStatus == RepairStatus.Completed)
            {
                throw new InvalidOperationException("Parts cannot be changed because this repair has already been completed.");
            }
        }

        // Recalculates the stored parent repair total from all linked repair part rows.
        private async Task UpdateRepairTotalCostAsync(int repairId)
        {
            var repair = await _context.Repairs
                .FirstOrDefaultAsync(existingRepair => existingRepair.RepairId == repairId);

            if (repair is null)
            {
                return;
            }

            repair.TotalCost = await _context.RepairParts
                .Where(part => part.RepairId == repairId)
                .SumAsync(part => (decimal?)(part.Quantity * part.UnitCost)) ?? 0m;

            await _context.SaveChangesAsync();
        }
    }
}
