using GMMW.Web.Services.Interfaces;
using GMMW.Web.Data;
using GMMW.Web.Models.Domain;
using GMMW.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace GMMW.Web.Services.Implementations
{
    // Handles motorist search, retrieval, creation, update, and delete rules.
    public class MotoristService : IMotoristService
    {
        private readonly ApplicationDbContext _context;

        public MotoristService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Returns one page of motorists matching the search term, ordered for stable pagination.
        public async Task<List<Motorist>> SearchMotoristsAsync(string? searchTerm, int pageNumber, int pageSize)
        {
            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            if (pageSize < 1)
            {
                pageSize = 1;
            }

            return await BuildMotoristSearchQuery(searchTerm)
                .Include(motorist => motorist.Vehicles)
                .OrderBy(motorist => motorist.LastName)
                .ThenBy(motorist => motorist.FirstName)
                .ThenBy(motorist => motorist.MotoristId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        // Builds the shared motorist search query used by both the paged results method and the count method.
        private IQueryable<Motorist> BuildMotoristSearchQuery(string? searchTerm)
        {
            var query = _context.Motorists
                .AsNoTracking()
                .AsQueryable();

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return query;
            }

            var trimmedSearchTerm = searchTerm.Trim();
            var upperSearchTerm = trimmedSearchTerm.ToUpper();
            var phoneSearchTerm = trimmedSearchTerm.Replace(" ", string.Empty);

            return query.Where(motorist =>
                motorist.FirstName.ToUpper().Contains(upperSearchTerm) ||
                motorist.LastName.ToUpper().Contains(upperSearchTerm) ||
                ((motorist.FirstName + " " + motorist.LastName).ToUpper().Contains(upperSearchTerm)) ||
                motorist.Email.ToUpper().Contains(upperSearchTerm) ||
                motorist.PhoneNumber.Replace(" ", string.Empty).Contains(phoneSearchTerm));
        }

        // Returns how many motorists match the current search term for pagination.
        public async Task<int> GetMotoristSearchCountAsync(string? searchTerm)
        {
            return await BuildMotoristSearchQuery(searchTerm).CountAsync();
        }

        // Loads one motorist by ID together with linked vehicles for the details or edit flow.
        public async Task<Motorist?> GetMotoristByIdAsync(int motoristId)
        {
            return await _context.Motorists
                .AsNoTracking()
                .Include(motorist => motorist.Vehicles)
                .FirstOrDefaultAsync(motorist => motorist.MotoristId == motoristId);
        }

        // Creates a new motorist record after checking that the email address is not already in use.
        public async Task<int> CreateMotoristAsync(MotoristCreateViewModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            var firstName = NormalizeText(model.FirstName);
            var lastName = NormalizeText(model.LastName);
            var email = NormalizeText(model.Email);
            var phoneNumber = NormalizeText(model.PhoneNumber);
            var address = NormalizeText(model.Address);
            var normalizedEmail = NormalizeEmailForComparison(email);

            var emailExists = await _context.Motorists
                .AnyAsync(motorist => motorist.Email.Trim().ToUpper() == normalizedEmail);

            if (emailExists)
            {
                throw new InvalidOperationException("A motorist with this email address already exists.");
            }

            var motorist = new Motorist
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PhoneNumber = phoneNumber,
                Address = address
            };

            _context.Motorists.Add(motorist);
            await _context.SaveChangesAsync();

            return motorist.MotoristId;
        }

        // Updates an existing motorist record after checking that the edited email address stays unique.
        public async Task UpdateMotoristAsync(MotoristEditViewModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            var motorist = await _context.Motorists
                .FirstOrDefaultAsync(existingMotorist => existingMotorist.MotoristId == model.MotoristId);

            if (motorist is null)
            {
                throw new InvalidOperationException("The selected motorist could not be found.");
            }

            var firstName = NormalizeText(model.FirstName);
            var lastName = NormalizeText(model.LastName);
            var email = NormalizeText(model.Email);
            var phoneNumber = NormalizeText(model.PhoneNumber);
            var address = NormalizeText(model.Address);
            var normalizedEmail = NormalizeEmailForComparison(email);

            var emailExists = await _context.Motorists
                .AnyAsync(existingMotorist =>
                    existingMotorist.MotoristId != model.MotoristId &&
                    existingMotorist.Email.Trim().ToUpper() == normalizedEmail);

            if (emailExists)
            {
                throw new InvalidOperationException("A motorist with this email address already exists.");
            }

            motorist.FirstName = firstName;
            motorist.LastName = lastName;
            motorist.Email = email;
            motorist.PhoneNumber = phoneNumber;
            motorist.Address = address;

            await _context.SaveChangesAsync();
        }

        // Deletes a motorist only when no linked vehicle or protected related records still exist.
        public async Task DeleteMotoristAsync(int motoristId)
        {
            var motorist = await _context.Motorists
                .Include(existingMotorist => existingMotorist.Vehicles)
                .FirstOrDefaultAsync(existingMotorist => existingMotorist.MotoristId == motoristId);

            if (motorist is null)
            {
                throw new InvalidOperationException("The selected motorist could not be found.");
            }

            if (motorist.Vehicles.Any())
            {
                throw new InvalidOperationException("This motorist cannot be deleted because vehicles are still linked. Please deal with the related vehicles first.");
            }

            var hasLinkedAttendance = await _context.ClassAttendances
                .AnyAsync(attendance => attendance.MotoristId == motoristId);

            if (hasLinkedAttendance)
            {
                throw new InvalidOperationException("This motorist cannot be deleted because related records still exist in the system. Review the linked records before trying again.");
            }

            _context.Motorists.Remove(motorist);
            await _context.SaveChangesAsync();
        }

        // Trims a text value before it is saved so basic user input stays consistent.
        private static string NormalizeText(string? value)
        {
            return value?.Trim() ?? string.Empty;
        }

        // Builds a trimmed upper-case email value for duplicate comparison checks.
        private static string NormalizeEmailForComparison(string? email)
        {
            return email?.Trim().ToUpper() ?? string.Empty;
        }
    }
}
