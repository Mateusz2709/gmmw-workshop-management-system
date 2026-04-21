using GMMW.Web.Components;
using GMMW.Web.Components.Account;
using GMMW.Web.Data;
using GMMW.Web.Data.Seed;
using GMMW.Web.Services.Implementations;
using GMMW.Web.Services.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GMMW.Web.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Registers the Blazor component model and enables interactive server rendering.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Registers authentication-state services used by Blazor and the Identity account flow.
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

// Registers the application service layer for dependency injection.
builder.Services.AddScoped<IMotoristService, MotoristService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IRepairService, RepairService>();
builder.Services.AddScoped<IRepairPartService, RepairPartService>();
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IVolunteerWorkService, VolunteerWorkService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IUserAdminService, UserAdminService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddSignalR();

// Configures cookie-based authentication for ASP.NET Identity.
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
    .AddIdentityCookies();

// Configures EF Core to use the main SQL Server connection string.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configures ASP.NET Identity for internal users, roles, and EF Core persistence.
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    // Users can sign in without account-confirmation flow in this assignment project.
    options.SignIn.RequireConfirmedAccount = false;
    options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
})
    .AddRoles<IdentityRole>() // Enables role support such as SuperUser and WorkshopUser.
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// Sets the main cookie redirect paths for login and access-denied handling.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/access-denied";
});

// Keeps Identity account features satisfied without implementing real email sending.
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

// Configures environment-specific error handling and production security behaviour.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Re-executes unknown routes through the app's not-found page.
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseHttpsRedirection();
app.UseAntiforgery();

// Maps static assets and the main Blazor component app.
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Maps the additional Identity account endpoints used by the /Account area.
app.MapAdditionalIdentityEndpoints();

app.MapHub<ClassUpdatesHub>("/hubs/class-updates");

// Seeds the default roles and administrator account on startup.
await DbSeeder.SeedRolesAndAdminAsync(app.Services);

app.Run();
