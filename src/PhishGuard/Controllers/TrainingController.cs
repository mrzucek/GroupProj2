using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhishGuard.Data;
using PhishGuard.Models;
using PhishGuard.Services;
using System.Security.Claims;

namespace PhishGuard.Controllers;

[Authorize]
public class TrainingController : Controller
{
    private readonly PhishGuardContext _db;
    private readonly TrainingService _training;

    public TrainingController(PhishGuardContext db, TrainingService training)
    {
        _db = db;
        _training = training;
    }

    private int GetEmployeeId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Training hub — shows pending simulations, history, and stats.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var employeeId = GetEmployeeId();

        var training = await _db.EmployeeTrainings
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId);

        var pending = await _training.GetPendingSimulationsAsync(employeeId);
        var history = await _training.GetHistoryAsync(employeeId);

        ViewBag.Training = training;
        ViewBag.Pending = pending;
        ViewBag.History = history;

        return View();
    }

    /// <summary>
    /// View a simulated phishing email (like an inbox would show it).
    /// </summary>
    public async Task<IActionResult> ViewSimulation(int id)
    {
        var employeeId = GetEmployeeId();

        var sim = await _db.SimulationEmails
            .Include(s => s.Campaign)
            .FirstOrDefaultAsync(s => s.SimulationId == id && s.TargetEmployeeId == employeeId);

        if (sim == null) return NotFound();

        // Mark as opened
        if (sim.OpenedAt == null)
        {
            sim.OpenedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return View(sim);
    }

    /// <summary>
    /// Employee clicked the link in a simulated phishing email — gotcha moment.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SimulationClicked(int id)
    {
        var employeeId = GetEmployeeId();
        var sim = await _training.RecordClickAsync(id, employeeId);

        if (sim == null) return NotFound();

        return RedirectToAction("SimulationResult", new { id });
    }

    /// <summary>
    /// Employee reported a simulated phishing email — correct action.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SimulationReported(int id)
    {
        var employeeId = GetEmployeeId();
        var sim = await _training.RecordReportAsync(id, employeeId);

        if (sim == null) return NotFound();

        return RedirectToAction("SimulationResult", new { id });
    }

    /// <summary>
    /// Show the result/feedback after interacting with a simulation.
    /// </summary>
    public async Task<IActionResult> SimulationResult(int id)
    {
        var employeeId = GetEmployeeId();

        var sim = await _db.SimulationEmails
            .Include(s => s.Campaign)
            .FirstOrDefaultAsync(s => s.SimulationId == id && s.TargetEmployeeId == employeeId);

        if (sim == null) return NotFound();

        var training = await _db.EmployeeTrainings
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId);

        ViewBag.Training = training;
        return View(sim);
    }

    /// <summary>
    /// Leaderboard page.
    /// </summary>
    public async Task<IActionResult> Leaderboard()
    {
        var leaderboard = await _training.GetLeaderboardAsync();
        var employeeId = GetEmployeeId();
        ViewBag.CurrentEmployeeId = employeeId;
        return View(leaderboard);
    }

    /// <summary>
    /// Admin/demo action: send a simulation to the current user.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SendSimulation()
    {
        var employeeId = GetEmployeeId();
        var sim = await _training.SendSimulationAsync(employeeId);

        if (sim == null)
        {
            TempData["Success"] = "No campaigns available. Seed campaigns first.";
        }
        else
        {
            TempData["Success"] = "A simulated phishing email has been sent to your training inbox!";
        }

        return RedirectToAction("Index");
    }
}
