using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhishGuard.Data;
using PhishGuard.Services;
using System.Security.Claims;

namespace PhishGuard.Controllers;

[Authorize]
public class DemoController : Controller
{
    private readonly PhishGuardContext _db;
    private readonly OpenAiEmailGeneratorService _emailGenerator;

    public DemoController(PhishGuardContext db, OpenAiEmailGeneratorService emailGenerator)
    {
        _db = db;
        _emailGenerator = emailGenerator;
    }

    [HttpPost]
    public async Task<IActionResult> SeedTestEmails()
    {
        var employeeId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var employee = await _db.Employees.FindAsync(employeeId);
        if (employee == null) return RedirectToAction("Index", "Dashboard");

        await _emailGenerator.SeedEmailsForUserAsync(employeeId, employee.DisplayName, employee.Email);

        TempData["Success"] = "Seeded 10 AI-generated emails and ran them through the scoring engine.";
        return RedirectToAction("Index", "Dashboard");
    }
}
