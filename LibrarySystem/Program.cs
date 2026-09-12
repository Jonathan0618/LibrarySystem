using LibrarySystem.Application.Catalog;
using LibrarySystem.Application.Members;
using LibrarySystem.Application.Security;
using LibrarySystem.Infrastructure.Catalog;
using LibrarySystem.Infrastructure.Data;
using LibrarySystem.Infrastructure.Identity;
using LibrarySystem.Infrastructure.Members;
using LibrarySystem.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.SlidingExpiration = true;
});
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.Zero);

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build())
    .AddPolicy(PolicyNames.ManageUsers,
        policy => policy.RequireRole(RoleNames.Administrator))
    .AddPolicy(PolicyNames.ManageCatalog,
        policy => policy.RequireRole(RoleNames.Administrator, RoleNames.Librarian))
    .AddPolicy(PolicyNames.ManageCirculation,
        policy => policy.RequireRole(RoleNames.Administrator, RoleNames.Librarian))
    .AddPolicy(PolicyNames.ViewOperationalReports,
        policy => policy.RequireRole(RoleNames.Administrator, RoleNames.Librarian))
    .AddPolicy(PolicyNames.ViewOwnAccount,
        policy => policy.RequireAuthenticatedUser())
    .AddPolicy(PolicyNames.ReserveBooks,
        policy => policy.RequireRole(RoleNames.Student, RoleNames.Teacher));

builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddSingleton<IBookCoverStorage, LocalBookCoverStorage>();
builder.Services.AddScoped<ICatalogReferenceService, CatalogReferenceService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<IMemberService, MemberService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
    .WithStaticAssets();

await app.Services.SeedIdentityAsync(app.Configuration);

app.Run();

public partial class Program;
