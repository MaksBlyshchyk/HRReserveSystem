using System.Diagnostics;
using HRReserveSystem.Data;
using HRReserveSystem.Models;
using HRReserveSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRReserveSystem.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var applicationStatusCounts = await _context.Applications
            .AsNoTracking()
            .GroupBy(application => application.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Status, item => item.Count);

        var vacancyStatusCounts = await _context.Vacancies
            .AsNoTracking()
            .Where(vacancy => !vacancy.IsArchived)
            .GroupBy(vacancy => vacancy.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Status, item => item.Count);

        var softSkillScores = await _context.SoftSkillAssessments
            .AsNoTracking()
            .Select(assessment => (assessment.Communication + assessment.Teamwork + assessment.Responsibility + assessment.StressResistance + assessment.Leadership) / 5.0)
            .ToListAsync();

        var dashboard = new DashboardViewModel
        {
            CandidateCount = await _context.Candidates.CountAsync(candidate => !candidate.IsDeleted),
            VacancyCount = await _context.Vacancies.CountAsync(vacancy => !vacancy.IsArchived),
            ApplicationCount = await _context.Applications.CountAsync(),
            InterviewCount = await _context.Interviews.CountAsync(),
            RecruiterCount = await _context.Recruiters.CountAsync(),
            AcceptedCandidateCount = await _context.Applications
                .Where(application => application.Status == "Hired")
                .Select(application => application.CandidateId)
                .Distinct()
                .CountAsync(),
            RejectedCandidateCount = await _context.Applications
                .Where(application => application.Status == "Rejected")
                .Select(application => application.CandidateId)
                .Distinct()
                .CountAsync(),
            AverageSoftSkillScore = softSkillScores.Count == 0 ? 0 : Math.Round(softSkillScores.Average(), 1),
            RecentCandidates = await _context.Candidates
                .AsNoTracking()
                .Where(candidate => !candidate.IsDeleted)
                .OrderByDescending(candidate => candidate.CreatedAt)
                .Take(5)
                .ToListAsync(),
            RecentVacancies = await _context.Vacancies
                .AsNoTracking()
                .Where(vacancy => !vacancy.IsArchived)
                .OrderByDescending(vacancy => vacancy.CreatedAt)
                .Take(5)
                .ToListAsync(),
            UpcomingInterviews = await _context.Interviews
                .AsNoTracking()
                .Include(interview => interview.Application)
                    .ThenInclude(application => application!.Candidate)
                .Include(interview => interview.Application)
                    .ThenInclude(application => application!.Vacancy)
                .Include(interview => interview.Recruiter)
                .Where(interview => interview.InterviewDate >= DateTime.Now)
                .OrderBy(interview => interview.InterviewDate)
                .Take(5)
                .ToListAsync(),
            ApplicationStatusCounts = applicationStatusCounts,
            VacancyStatusCounts = vacancyStatusCounts
        };

        return View(dashboard);
    }

    [AllowAnonymous]
    public IActionResult About()
    {
        return View();
    }

    [AllowAnonymous]
    public IActionResult CourseMaterials()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
