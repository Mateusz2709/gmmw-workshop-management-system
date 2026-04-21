using GMMW.Web.Models.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore; // This imports the part of ASP.NET Identity that works with Entity Framework Core.
using Microsoft.EntityFrameworkCore; // This imports the main EF Core namespace - Entity Framework Core is Microsoft’s ORM framework for .NET.

namespace GMMW.Web.Data
{
    // This creates a class called ApplicationDbContext. This is the main bridge between: C# code, Ef Core, the database and Identity (the database control centre of the application)
    // (DbContextOptions<ApplicationDbContext> options) - this class receives configuration options when it is created.
    // : IdentityDbContext<ApplicationUser>(options) - It is a special EF Core context class provided by ASP.NET Identity.
    //      It already knows how to manage the database tables needed for Identity.
    //      <ApplicationUser> means use my custom ApplicationUser class as the user model.
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        // DbSet<T> is an EF Core type that represents a set of entities of one kind. For example : the doorway from C# into the Motorists table.
        public DbSet<ClassAttendance> ClassAttendances { get; set; } = default!;
        public DbSet<Motorist> Motorists { get; set; } = default!; // Initialized by EF Core at runtime; default! is used to silence nullable warnings.
        public DbSet<Repair> Repairs { get; set; } = default!;
        public DbSet<RepairPart> RepairParts { get; set; } = default!;
        public DbSet<RepairVolunteerAssignment> RepairVolunteerAssignments { get; set; } = default!;
        public DbSet<Vehicle> Vehicles { get; set; } = default!;
        public DbSet<WorkshopClass> WorkshopClasses { get; set; } = default!;

        // This method is called by EF Core when building the database model.
        // It is used to configure entities, relationships, keys, and other database rules.
        // This relationship could be inferred by EF Core conventions, but it is configured explicitly to make the design clearer and easier to control later.
        protected override void OnModelCreating(ModelBuilder builder)
        {
            // run the model configuration that already exists in the parent class before applying my custom rules.
            base.OnModelCreating(builder);

            // now I want to define extra database rules for the Vehicle class.
            builder.Entity<Vehicle>()
                .HasOne(v => v.Motorist) // // Each vehicle is linked to one motorist (its owner).
                .WithMany(m => m.Vehicles) // from the Motorist side, one motorist can be related to many vehicles.
                .HasForeignKey(v => v.MotoristId) // // Use MotoristId as the foreign key that links each vehicle to its owner.
                .OnDelete(DeleteBehavior.Restrict); //  Prevents deleting a motorist while related vehicle records still exist.

            builder.Entity<Repair>() // Tells EF Core we are configuring the Repair entity.
                .HasOne(r => r.Vehicle) // Each Repair has one related Vehicle.
                .WithMany(v => v.Repairs) // One Vehicle can have many Repair records.
                .HasForeignKey(v => v.VehicleId) // The foreign key on the Repair table is VehicleId
                .OnDelete(DeleteBehavior.Restrict); // Prevents deleting a vehicle while related repair records still exist.

            // Tell EF Core how to store the TotalCost decimal value in SQL Server.
            // We do this because money/cost values should have a clear precision and scale.
            builder.Entity<Repair>()
                .Property(r => r.TotalCost) // Select the TotalCost property from the Repair entity.
                .HasPrecision(10, 2); // Store it with up to 10 digits in total, including 2 after the decimal point.


            builder.Entity<RepairPart>() // Tells EF Core we are configuring the RepairPart entity.
                .HasOne(rp => rp.Repair) // Each RepairPart belongs to one Repair.
                .WithMany(r => r.RepairParts) // One Repair can have many linked part records.
                .HasForeignKey(rp => rp.RepairId) // The foreign key is RepairId.
                .OnDelete(DeleteBehavior.Cascade); // If a repair record is deleted, its child RepairPart records should also be deleted automatically.

            // Tell EF Core how to store the UnitCost decimal value in SQL Server.
            // This is useful because part prices also need a safe and predictable decimal format.
            builder.Entity<RepairPart>()
                .Property(rp => rp.UnitCost) // Select the UnitCost property from the RepairPart entity.
                .HasPrecision(10, 2); // Allow up to 10 digits in total, with 2 digits after the decimal point.

            // WorkshopClass -> ApplicationUser
            builder.Entity<WorkshopClass>()
                .HasOne(w => w.DeliveredByUser) // each class has one delivering user
                .WithMany(u => u.WorkshopClasses) // one user can deliver many classes
                .HasForeignKey(w => w.DeliveredByUserId) // the FK is DeliveredByUserId
                .OnDelete(DeleteBehavior.Restrict); // cannot delete that user while linked classes still exist

            // ClassAttendance -> WorkshopClass
            builder.Entity<ClassAttendance>()
                .HasOne(ca => ca.WorkshopClass) // each attendance record belongs to one class
                .WithMany(w => w.ClassAttendances) // one class can have many attendance records
                .HasForeignKey(ca => ca.WorkshopClassId)
                // I’m using Cascade here because ClassAttendance is a child/linking record and should not really exist on its own without its parent class
                .OnDelete(DeleteBehavior.Cascade); // if a class is deleted, its attendance rows are deleted too

            // ClassAttendance -> Motorist
            builder.Entity<ClassAttendance>()
                .HasOne(ca => ca.Motorist) // each attendance record belongs to one motorist
                .WithMany(m => m.ClassAttendances) // each attendance record belongs to one motorist
                .HasForeignKey(ca => ca.MotoristId)
                // I used Restrict here because attendance history is meaningful, so it should not disappear just because someone deletes a motorist record.
                .OnDelete(DeleteBehavior.Restrict); // cannot delete a motorist while attendance records still exist

            // RepairVolunteerAssignment -> Repair
            builder.Entity<RepairVolunteerAssignment>()
                .HasOne(rva => rva.Repair) // Each assignment record belongs to one repair.
                .WithMany(r => r.RepairVolunteerAssignments) // One repair can have many volunteer assignment records.
                .HasForeignKey(rva => rva.RepairId)
                // I used Cascade here Because RepairVolunteerAssignment is a dependent linking record. It does not really make sense on its own without its parent repair
                .OnDelete(DeleteBehavior.Cascade);

            // Tell EF Core how to store the HoursSpent decimal value in SQL Server.
            // We use decimal here because repair work time may include values like 1.5 or 2.75 hours.
            builder.Entity<RepairVolunteerAssignment>()
                .Property(rva => rva.HoursSpent) // Select the HoursSpent property from the RepairVolunteerAssignment entity.
                .HasPrecision(10, 2); // Store it with up to 10 digits in total and 2 digits after the decimal point.

            // RepairVolunteerAssignment -> ApplicationUser
            builder.Entity<RepairVolunteerAssignment>()
                .HasOne(rva => rva.ApplicationUser) // each assignment belongs to one internal user
                .WithMany(u => u.RepairVolunteerAssignments) // one internal user can have many assignment records
                .HasForeignKey(rva => rva.ApplicationUserId)
                // I used Restrict here because volunteer time/history is meaningful, and I do not want to lose those records accidentally by deleting a user account.
                .OnDelete(DeleteBehavior.Restrict);







        }
    }
}
