using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PhishGuard.Data;
using PhishGuard.Models;
using PhishGuard.Services;
using PhishGuard.Services.Scoring;

var builder = WebApplication.CreateBuilder(args);

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Database=phishguard;User=root;Password=;";
builder.Services.AddDbContext<PhishGuardContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

// HTTP client for URL safety checks
builder.Services.AddHttpClient("SafeBrowsing", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("PhishGuard/1.0");
});

// Services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UrlSafetyService>();
builder.Services.AddScoped<TrainingService>();
builder.Services.AddScoped<IScoringDimension, ThreatFeedScorer>();
builder.Services.AddScoped<IScoringDimension, LanguageAnalysisScorer>();
builder.Services.AddScoped<IScoringDimension, DomainReputationScorer>();
builder.Services.AddScoped<ScoringEngine>();

// MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Seed default admin account
using (var scope = app.Services.CreateScope())
{
    var auth = scope.ServiceProvider.GetRequiredService<AuthService>();
    await auth.SeedAdminAsync();

    // Seed phishing campaign templates
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
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
