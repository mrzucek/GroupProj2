using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using PhishGuard.Models.ViewModels;
using PhishGuard.Services;
using System.Security.Claims;

namespace PhishGuard.Controllers;

public class AccountController : Controller
{
    private readonly AuthService _auth;
    private readonly IConfiguration _config;

    public AccountController(AuthService auth, IConfiguration config)
    {
        _auth = auth;
        _config = config;
    }

    private bool UseClerk => !string.IsNullOrEmpty(
        Environment.GetEnvironmentVariable("CLERK_AUTHORITY") ?? _config["Clerk:Authority"]);

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        ViewBag.UseClerk = UseClerk;
        ViewBag.ClerkPublishableKey = Environment.GetEnvironmentVariable("CLERK_PUBLISHABLE_KEY")
            ?? _config["Clerk:PublishableKey"];
        ViewBag.ClerkFapiUrl = Environment.GetEnvironmentVariable("CLERK_FAPI_URL") ?? "";
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (UseClerk) return RedirectToAction("Login");

        if (!ModelState.IsValid) return View(model);

        var employee = await _auth.LoginAsync(model.Email, model.Password);
        if (employee == null)
        {
            ModelState.AddModelError("", "Invalid email or password");
            return View(model);
        }

        await SignInAsync(employee);

        if (employee.Role == Models.EmployeeRole.Admin)
            return RedirectToAction("Index", "Admin");

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        ViewBag.UseClerk = UseClerk;
        ViewBag.ClerkPublishableKey = Environment.GetEnvironmentVariable("CLERK_PUBLISHABLE_KEY")
            ?? _config["Clerk:PublishableKey"];
        ViewBag.ClerkFapiUrl = Environment.GetEnvironmentVariable("CLERK_FAPI_URL") ?? "";
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (UseClerk) return RedirectToAction("Register");

        if (!ModelState.IsValid) return View(model);

        var employee = await _auth.RegisterAsync(model.Email, model.DisplayName, model.Password, model.Department);
        if (employee == null)
        {
            ModelState.AddModelError("Email", "An account with this email already exists");
            return View(model);
        }

        await SignInAsync(employee);
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        if (UseClerk)
        {
            Response.Cookies.Delete("__session");
            return RedirectToAction("Login");
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    private async Task SignInAsync(Models.Employee employee)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, employee.EmployeeId.ToString()),
            new(ClaimTypes.Email, employee.Email),
            new(ClaimTypes.Name, employee.DisplayName),
            new(ClaimTypes.Role, employee.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }
}
