using System.Threading.RateLimiting;
using System.Net;
using LibrarySystem.Application.Catalog;
using LibrarySystem.Application.Security;
using LibrarySystem.Application.Notifications;
using LibrarySystem.Infrastructure.Catalog;
using LibrarySystem.Infrastructure.Data;
using LibrarySystem.Infrastructure.Identity;
using LibrarySystem.Infrastructure.Security;
using LibrarySystem.Services;
using LibrarySystem.Services.Notifications;
using LibrarySystem.Services.Operations;
using LibrarySystem.Services.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment() &&
    (string.IsNullOrWhiteSpace(builder.Configuration["AllowedHosts"]) ||
     builder.Configuration["AllowedHosts"] == "*"))
{
    throw new InvalidOperationException(
        "AllowedHosts must contain the explicit production host name or names outside Development.");
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

var reverseProxyEnabled = builder.Configuration.GetValue<bool>("ReverseProxy:Enabled");
if (reverseProxyEnabled)
{
    var knownProxyValues = builder.Configuration
        .GetSection("ReverseProxy:KnownProxies")
        .Get<string[]>() ?? [];
    if (knownProxyValues.Length == 0)
    {
        throw new InvalidOperationException(
            "ReverseProxy:KnownProxies must contain at least one trusted proxy when reverse-proxy handling is enabled.");
    }

    var knownProxies = knownProxyValues.Select(value =>
        IPAddress.TryParse(value, out var address)
            ? address
            : throw new InvalidOperationException($"ReverseProxy:KnownProxies contains an invalid IP address: {value}"))
        .ToArray();
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;
        options.RequireHeaderSymmetry = true;
        options.KnownProxies.Clear();
        foreach (var knownProxy in knownProxies)
        {
            options.KnownProxies.Add(knownProxy);
        }
    });
}

var dataProtection = builder.Services.AddDataProtection()
    .SetApplicationName("LibrarySystem");
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
}
else if (!builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException(
        "DataProtection:KeysPath must point to persistent, access-controlled storage outside development.");
}

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
builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationUserClaimsPrincipalFactory>();
builder.Services.AddScoped<IUserValidator<ApplicationUser>, SchoolEmailUserValidator>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = false;
});
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.Zero);
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
    options.TokenLifespan = TimeSpan.FromMinutes(
        builder.Configuration.GetValue<int?>("Identity:Links:TokenLifetimeMinutes") ?? 60));

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build())
    .AddPolicy(PolicyNames.ManageUsers,
        policy => policy.RequireRole(RoleNames.Administrator, RoleNames.Librarian))
    .AddPolicy(PolicyNames.ManageCatalog,
        policy => policy.RequireRole(RoleNames.Administrator, RoleNames.Librarian))
    .AddPolicy(PolicyNames.ManageCirculation,
        policy => policy.RequireRole(RoleNames.Administrator, RoleNames.Librarian))
    .AddPolicy(PolicyNames.ViewOperationalReports,
        policy => policy.RequireRole(RoleNames.Administrator, RoleNames.Librarian))
    .AddPolicy(PolicyNames.ViewAuditLog,
        policy => policy.RequireRole(RoleNames.Administrator))
    .AddPolicy(PolicyNames.ViewOwnAccount,
        policy => policy.RequireAuthenticatedUser())
    .AddPolicy(PolicyNames.ReserveBooks,
        policy => policy.RequireRole(RoleNames.Student, RoleNames.Teacher));

builder.Services.AddRazorPages();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var path = context.Request.Path;
        if (!HttpMethods.IsPost(context.Request.Method) ||
            (!path.StartsWithSegments("/Identity/Account/Login") &&
            !path.StartsWithSegments("/Identity/Account/ForgotPassword") &&
            !path.StartsWithSegments("/Identity/Account/ResetPassword") &&
            !path.StartsWithSegments("/Identity/Account/ConfirmEmail") &&
            !path.StartsWithSegments("/Identity/Account/ResendEmailConfirmation")))
        {
            return RateLimitPartition.GetNoLimiter("non-authentication");
        }

        var clientAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var clientKey = $"{clientAddress}:{path.Value?.ToLowerInvariant()}";
        return RateLimitPartition.GetFixedWindowLimiter(clientKey, _ => new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = 5,
            QueueLimit = 0,
            Window = TimeSpan.FromMinutes(1)
        });
    });
});
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database", tags: ["ready"]);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddSingleton<IBookCoverStorage, LocalBookCoverStorage>();
builder.Services.AddLibraryServices();
builder.Services.AddOptions<DataRetentionOptions>()
    .Bind(builder.Configuration.GetSection(DataRetentionOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<NotificationDeliveryOptions>()
    .Bind(builder.Configuration.GetSection(NotificationDeliveryOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<IdentityLinkOptions>()
    .Bind(builder.Configuration.GetSection(IdentityLinkOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<INotificationSender, DevelopmentNotificationSender>();
}
else
{
    builder.Services.AddOptions<SmtpOptions>()
        .Bind(builder.Configuration.GetSection(SmtpOptions.SectionName))
        .ValidateDataAnnotations()
        .Validate(options =>
            !options.Host.Contains("example", StringComparison.OrdinalIgnoreCase) &&
            !options.UserName.Contains("SET_IN_SECRET_STORE", StringComparison.OrdinalIgnoreCase) &&
            !options.Password.Contains("SET_IN_SECRET_STORE", StringComparison.OrdinalIgnoreCase) &&
            !options.FromAddress.Contains("example", StringComparison.OrdinalIgnoreCase),
            "Production SMTP settings must be supplied by the deployment secret/configuration store; template placeholders are not valid.")
        .ValidateOnStart();
    builder.Services.AddSingleton<INotificationSender, SmtpNotificationSender>();
}

builder.Logging.Configure(options =>
    options.ActivityTrackingOptions = ActivityTrackingOptions.TraceId |
        ActivityTrackingOptions.SpanId |
        ActivityTrackingOptions.ParentId);
if (!builder.Environment.IsDevelopment())
{
    builder.Logging.AddJsonConsole();
}

var app = builder.Build();

if (reverseProxyEnabled)
{
    app.UseForwardedHeaders();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.Use(async (context, next) =>
{
    var suppliedCorrelationId = context.Request.Headers["X-Correlation-ID"].ToString();
    var correlationId = Guid.TryParse(suppliedCorrelationId, out var parsedCorrelationId)
        ? parsedCorrelationId.ToString("D")
        : Guid.NewGuid().ToString("D");
    context.TraceIdentifier = correlationId;
    context.Response.Headers["X-Correlation-ID"] = correlationId;

    using (app.Logger.BeginScope(new Dictionary<string, object>
    {
        ["CorrelationId"] = correlationId
    }))
    {
        await next(context);
    }
});

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
    context.Response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'none'; " +
        "form-action 'self'; img-src 'self' data:; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "style-src 'self' https://fonts.googleapis.com; " +
        "script-src 'self'";
    await next(context);
});

app.UseRateLimiter();

app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        context.Response.Headers.CacheControl = "no-store, no-cache";
        context.Response.Headers.Pragma = "no-cache";
    }
    await next(context);
});
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true &&
        context.User.HasClaim(CustomClaimTypes.MustChangePassword, bool.TrueString) &&
        !context.Request.Path.StartsWithSegments("/Identity/Account/ChangePassword") &&
        !context.Request.Path.StartsWithSegments("/Identity/Account/Logout"))
    {
        var returnUrl = context.Request.PathBase + context.Request.Path + context.Request.QueryString;
        context.Response.Redirect(QueryString.Create("returnUrl", returnUrl).ToUriComponent()
            .Insert(0, "/Identity/Account/ChangePassword"));
        return;
    }
    await next(context);
});
app.UseAuthorization();

app.MapStaticAssets().AllowAnonymous();
app.MapRazorPages()
    .WithStaticAssets();
app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false
    })
    .AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("ready")
    })
    .AllowAnonymous();

await app.Services.SeedIdentityAsync(app.Configuration, app.Environment);
await app.Services.SeedLibraryDataAsync();

app.Run();

public partial class Program;
