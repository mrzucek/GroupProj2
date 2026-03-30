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

    public AccountController(AuthService auth)
    {
        _auth = auth;
    }

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
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
    public IActionResult Register() => View();

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
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
