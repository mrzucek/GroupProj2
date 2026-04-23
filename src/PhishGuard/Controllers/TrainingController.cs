using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhishGuard.Data;
using PhishGuard.Models;
using PhishGuard.Models.ViewModels;
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
    /// Login-gate quiz: employee must identify which email is phishing before accessing dashboard.
    /// </summary>
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> QuizGate()
    {
        var employeeId = GetEmployeeId();

        var sim = await _db.SimulationEmails
            .Include(s => s.Campaign)
            .FirstOrDefaultAsync(s => s.TargetEmployeeId == employeeId
                                   && s.RequiresLoginQuiz
                                   && s.Result == Models.SimulationResult.Pending);

        if (sim == null) return RedirectToAction("Index", "Dashboard");

        // Pick a legitimate inbox email to show alongside the phishing one
        var legitEmail = await _db.Emails
            .Where(e => e.RecipientId == employeeId && e.Classification != EmailClassification.Blocked)
            .OrderBy(e => Guid.NewGuid())
            .FirstOrDefaultAsync();

        // Randomise which slot holds the phishing email
        var rng = new Random();
        var phishSlot = rng.Next(2) == 0 ? "left" : "right";
        TempData["PhishingSlot"] = phishSlot;
        TempData["PendingSimId"] = sim.SimulationId;

        var bodyPreview = legitEmail?.BodyPreview ?? "No preview available.";
        if (bodyPreview.Length > 300) bodyPreview = bodyPreview[..300] + "...";

        var campaignBody = sim.Campaign.TemplateBody;
        if (campaignBody.Length > 300) campaignBody = campaignBody[..300] + "...";

        var vm = new QuizGateViewModel
        {
            SimulationId = sim.SimulationId,
            PhishingSlot = phishSlot,
            PhishSenderDisplay = sim.Campaign.TemplateSender,
            PhishSenderAddress = sim.Campaign.TemplateSender,
            PhishSubject = sim.Campaign.TemplateSubject,
            PhishBodyPreview = campaignBody,
            LegitSenderDisplay = legitEmail?.SenderDisplayName ?? legitEmail?.SenderAddress ?? "Unknown Sender",
            LegitSenderAddress = legitEmail?.SenderAddress ?? "unknown@example.com",
            LegitSubject = legitEmail?.Subject ?? "(no subject)",
            LegitBodyPreview = bodyPreview
        };

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> QuizGate(int simulationEmailId, string selectedSlot)
    {
        var employeeId = GetEmployeeId();

        var sim = await _db.SimulationEmails
            .Include(s => s.Campaign)
            .FirstOrDefaultAsync(s => s.SimulationId == simulationEmailId && s.TargetEmployeeId == employeeId);

        if (sim == null) return RedirectToAction("Index", "Dashboard");

        var correctSlot = TempData["PhishingSlot"]?.ToString();
        var training = await _db.EmployeeTrainings.FirstOrDefaultAsync(t => t.EmployeeId == employeeId);

        bool isCorrect = selectedSlot == correctSlot;
        int pointsAwarded = 0;

        if (isCorrect)
        {
            sim.Result = Models.SimulationResult.Reported;
            sim.ReportedAt = DateTime.UtcNow;

            if (training != null)
            {
                training.TotalCorrectlyReported++;
                training.CurrentStreak++;
                if (training.CurrentStreak > training.BestStreak)
                    training.BestStreak = training.CurrentStreak;

                pointsAwarded = sim.Campaign.Difficulty switch
                {
                    Difficulty.Easy => 5,
                    Difficulty.Medium => 10,
                    Difficulty.Hard => 20,
                    Difficulty.Expert => 40,
                    _ => 10
                };
                training.ScorePoints += pointsAwarded;

                if (training.CurrentStreak > 0 && training.CurrentStreak % 5 == 0
                    && training.CurrentDifficulty < Difficulty.Expert)
                {
                    training.CurrentDifficulty++;
                    training.DifficultyUpdatedAt = DateTime.UtcNow;
                }
            }
        }
        else
        {
            sim.Result = Models.SimulationResult.Clicked;
            sim.ClickedLinkAt = DateTime.UtcNow;

            if (training != null)
            {
                training.TotalClicked++;
                training.CurrentStreak = 0;

                if (training.TotalClicked % 3 == 0 && training.CurrentDifficulty > Difficulty.Easy)
                {
                    training.CurrentDifficulty--;
                    training.DifficultyUpdatedAt = DateTime.UtcNow;
                }
            }
        }

        await _db.SaveChangesAsync();

        TempData["QuizIsCorrect"] = isCorrect;
        TempData["QuizPoints"] = pointsAwarded;
        TempData["QuizPhishSenderDisplay"] = sim.Campaign.TemplateSender;
        TempData["QuizPhishSenderAddress"] = sim.Campaign.TemplateSender;
        TempData["QuizPhishSubject"] = sim.Campaign.TemplateSubject;
        TempData["QuizPhishingIndicators"] = sim.Campaign.PhishingIndicators;

        return RedirectToAction("QuizResult");
    }

    [Authorize(Roles = "Employee")]
    public IActionResult QuizResult()
    {
        var vm = new QuizResultViewModel
        {
            IsCorrect = TempData["QuizIsCorrect"] as bool? ?? false,
            PointsAwarded = TempData["QuizPoints"] as int? ?? 0,
            PhishSenderDisplay = TempData["QuizPhishSenderDisplay"]?.ToString() ?? "",
            PhishSenderAddress = TempData["QuizPhishSenderAddress"]?.ToString() ?? "",
            PhishSubject = TempData["QuizPhishSubject"]?.ToString() ?? "",
            PhishingIndicators = TempData["QuizPhishingIndicators"]?.ToString() ?? "",
            StreakReset = !(TempData["QuizIsCorrect"] as bool? ?? false)
        };

        return View(vm);
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
