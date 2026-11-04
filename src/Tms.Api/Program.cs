using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Tms.Api.Auth;
using Tms.Api.Infrastructure;
using Tms.Api.Services;
using Tms.Infrastructure.Identity;
using Tms.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------------------
// Configuration: options are bound lazily so test/production overrides always win, and the
// signing key is validated wherever it is actually used (never a silent default).
// ---------------------------------------------------------------------------------------
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
builder.Services.Configure<JwtOptions>(jwtSection);

// ---------------------------------------------------------------------------------------
// Persistence (SQL Server locally, Azure SQL in the cloud - same provider).
// The connection string is resolved when the context is created so configuration overrides
// (environment variables, Key Vault, integration tests) always apply.
// ---------------------------------------------------------------------------------------
builder.Services.AddDbContext<TmsDbContext>((serviceProvider, options) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(3));
});

// ---------------------------------------------------------------------------------------
// Identity (users + password hashing) and our own JWT bearer authentication
// ---------------------------------------------------------------------------------------
builder.Services
    .AddIdentityCore<AppUser>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<TmsDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

// Validation parameters are built from the same IOptions instance the token service signs
// with, so signing and validation can never diverge.
builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<Microsoft.Extensions.Options.IOptions<JwtOptions>>((bearer, jwtAccessor) =>
    {
        var jwt = jwtAccessor.Value;

        if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey must be configured with at least 32 characters (use Key Vault / app settings in Azure).");
        }

        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddTmsAuthorization();

// ---------------------------------------------------------------------------------------
// MVC, problem details, exception mapping
// ---------------------------------------------------------------------------------------
builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

// Application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserAdminService, UserAdminService>();
builder.Services.AddScoped<ISiteService, SiteService>();
builder.Services.AddScoped<ITowerService, TowerService>();
builder.Services.AddScoped<ITenancyService, TenancyService>();
builder.Services.AddScoped<IOperatorService, OperatorService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddSingleton<ILookupService, LookupService>();

// CORS for the Vite dev server (production: the SPA origin(s) from configuration)
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? ["http://localhost:5173"];

builder.Services.AddCors(options => options.AddPolicy("spa", policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

// Throttle credential endpoints per client IP (raised in tests through configuration)
var authPermitLimit = builder.Configuration.GetValue("RateLimiting:AuthPermitLimit", 10);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = authPermitLimit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<TmsDbContext>("sqlserver", tags: ["ready"]);

// ---------------------------------------------------------------------------------------
// Swagger / OpenAPI with JWT bearer support
// ---------------------------------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Tower Management System API",
        Version = "v1",
        Description = "TowerCo portfolio: sites, towers, tenants (MNOs), tenancies and capacity. "
                      + "Sign in with POST /api/auth/login, then use Authorize with the returned access token."
    });

    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT access token without the 'Bearer ' prefix."
    };

    options.AddSecurityDefinition("Bearer", scheme);
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// ---------------------------------------------------------------------------------------
// Pipeline
// ---------------------------------------------------------------------------------------
app.UseExceptionHandler();

if (app.Configuration.GetValue("Swagger:Enabled", true))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Tower Management System API v1");
        options.DocumentTitle = "Tower Management System API";
    });
}

app.UseCors("spa");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// When the built SPA is published into wwwroot (single App Service deployment), serve it and
// fall back to index.html so client-side routes survive a refresh.
var spaIndex = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "index.html");
if (File.Exists(spaIndex))
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.MapFallbackToFile("index.html");
}

await DbInitializer.InitializeAsync(app.Services, app.Configuration);

app.Run();

/// <summary>Exposed so the integration tests can boot the real pipeline.</summary>
public partial class Program;

