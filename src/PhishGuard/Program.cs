using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PhishGuard.Data;
using PhishGuard.Middleware;
using PhishGuard.Models;
using PhishGuard.Services;
using PhishGuard.Services.Scoring;
using PhishGuard.Services.Training;

// Load .env file if it exists
DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Database — prefer env var, fall back to appsettings
var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Database=phishguard;User=root;Password=;";
builder.Services.AddDbContext<PhishGuardContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Authentication — supports both Cookie (legacy) and Clerk JWT
var clerkAuthority = Environment.GetEnvironmentVariable("CLERK_AUTHORITY")
    ?? builder.Configuration["Clerk:Authority"];
var useClerk = !string.IsNullOrEmpty(clerkAuthority);

if (useClerk)
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = clerkAuthority;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = true,
                ValidIssuer = clerkAuthority,
                NameClaimType = "name"
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var sessionToken = context.Request.Cookies["__session"];
                    if (!string.IsNullOrEmpty(sessionToken))
                        context.Token = sessionToken;
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    if (!context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.HandleResponse();
                        context.Response.Redirect("/Account/Login");
                    }
                    return Task.CompletedTask;
                }
            };
        });
}
else
{
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
        });
}

// HTTP clients
builder.Services.AddHttpClient("SafeBrowsing", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("PhishNET/1.0");
});

builder.Services.AddHttpClient("OpenAI", client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});

// Services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UrlSafetyService>();
builder.Services.AddScoped<TrainingService>();
builder.Services.AddScoped<LinkAnalysisService>();
builder.Services.AddScoped<AiPhishScoreService>();
builder.Services.AddScoped<PhishingGeneratorService>();
builder.Services.AddScoped<OpenAiEmailGeneratorService>();
builder.Services.AddScoped<IScoringDimension, ThreatFeedScorer>();
builder.Services.AddScoped<IScoringDimension, LanguageAnalysisScorer>();
builder.Services.AddScoped<IScoringDimension, DomainReputationScorer>();
builder.Services.AddScoped<IScoringDimension, LinkAnalysisScorer>();
builder.Services.AddScoped<ScoringEngine>();

// MVC + JSON API support
builder.Services.AddControllersWithViews()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

var app = builder.Build();

// Seed default admin account
using (var scope = app.Services.CreateScope())
{
    var auth = scope.ServiceProvider.GetRequiredService<AuthService>();
    await auth.SeedAdminAsync();

    var db = scope.ServiceProvider.GetRequiredService<PhishGuardContext>();
    var admin = await db.Employees.FirstAsync(e => e.Role == PhishGuard.Models.EmployeeRole.Admin);
    var training = scope.ServiceProvider.GetRequiredService<TrainingService>();
    await training.SeedCampaignsAsync(admin.EmployeeId);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRouting();

app.UseAuthentication();
if (useClerk) app.UseMiddleware<ClerkUserMiddleware>();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
