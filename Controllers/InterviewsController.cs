using System.Security.Claims;
using System.Text;
using HRReserveSystem.Data;
using HRReserveSystem.Models;
using HRReserveSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HRReserveSystem.Controllers;

[Authorize(Roles = "Admin,Recruiter,Interviewer")]
public class InterviewsController(ApplicationDbContext context, IEmailNotificationService emailNotificationService) : Controller
{
    public async Task<IActionResult> Index(string? interviewType, string? result)
    {
        IQueryable<Interview> interviews = context.Interviews
            .AsNoTracking()
            .Include(interview => interview.Application)
                .ThenInclude(application => application!.Candidate)
            .Include(interview => interview.Application)
                .ThenInclude(application => application!.Vacancy)
            .Include(interview => interview.Recruiter);

        if (!string.IsNullOrWhiteSpace(interviewType))
        {
            interviews = interviews.Where(interview => interview.InterviewType == interviewType);
        }

        if (!string.IsNullOrWhiteSpace(result))
        {
            interviews = interviews.Where(interview => interview.Result == result);
        }

        ViewData["InterviewTypeFilter"] = interviewType;
        ViewData["ResultFilter"] = result;
        ViewData["InterviewTypes"] = new SelectList(HrOptions.InterviewTypes, interviewType);
        ViewData["Results"] = new SelectList(HrOptions.InterviewResults, result);

        return View(await interviews
            .OrderByDescending(interview => interview.InterviewDate)
            .ToListAsync());
    }

    public async Task<IActionResult> Calendar()
    {
        var interviews = await context.Interviews
            .AsNoTracking()
            .Include(interview => interview.Application)
                .ThenInclude(application => application!.Candidate)
            .Include(interview => interview.Application)
                .ThenInclude(application => application!.Vacancy)
            .Include(interview => interview.Recruiter)
            .OrderBy(interview => interview.InterviewDate)
            .ToListAsync();

        return View(interviews);
    }

    public async Task<IActionResult> ExportIcs(int? id = null)
    {
        IQueryable<Interview> interviews = context.Interviews
            .AsNoTracking()
            .Include(interview => interview.Application)
                .ThenInclude(application => application!.Candidate)
            .Include(interview => interview.Application)
                .ThenInclude(application => application!.Vacancy)
            .Include(interview => interview.Recruiter);

        if (id.HasValue)
        {
            interviews = interviews.Where(interview => interview.Id == id.Value);
        }

        var items = await interviews
            .OrderBy(interview => interview.InterviewDate)
            .ToListAsync();

        if (id.HasValue && items.Count == 0)
        {
            return NotFound();
        }

        var fileName = id.HasValue ? $"interview-{id.Value}.ics" : "interviews-calendar.ics";
        return File(Encoding.UTF8.GetBytes(BuildIcsCalendar(items)), "text/calendar; charset=utf-8", fileName);
    }

    public async Task<IActionResult> GoogleCalendar(int id)
    {
        var interview = await context.Interviews
            .AsNoTracking()
            .Include(item => item.Application)
                .ThenInclude(application => application!.Candidate)
            .Include(item => item.Application)
                .ThenInclude(application => application!.Vacancy)
            .Include(item => item.Recruiter)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (interview is null)
        {
            return NotFound();
        }

        var start = DateTime.SpecifyKind(interview.InterviewDate, DateTimeKind.Local).ToUniversalTime();
        var end = start.AddHours(1);
        var url = "https://calendar.google.com/calendar/render?action=TEMPLATE"
            + $"&text={Uri.EscapeDataString(BuildCalendarSummary(interview))}"
            + $"&dates={FormatIcsDate(start)}/{FormatIcsDate(end)}"
            + $"&details={Uri.EscapeDataString(BuildCalendarDescription(interview))}"
            + $"&location={Uri.EscapeDataString("HRReserveSystem")}";

        return Redirect(url);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var interview = await context.Interviews
            .AsNoTracking()
            .Include(item => item.Application)
                .ThenInclude(application => application!.Candidate)
            .Include(item => item.Application)
                .ThenInclude(application => application!.Vacancy)
            .Include(item => item.Recruiter)
            .Include(item => item.Feedbacks)
                .ThenInclude(feedback => feedback.Recruiter)
            .FirstOrDefaultAsync(item => item.Id == id);

        return interview is null ? NotFound() : View(interview);
    }

    [Authorize(Roles = "Admin,Recruiter")]
    public async Task<IActionResult> Create()
    {
        await PopulateApplicationsSelectList();
        await PopulateRecruitersSelectList(await GetCurrentRecruiterId());
        return View(new Interview { InterviewDate = DateTime.Now, RecruiterId = await GetCurrentRecruiterId() });
    }

    [Authorize(Roles = "Admin,Recruiter")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ApplicationId,RecruiterId,InterviewDate,InterviewType,Result,Notes")] Interview interview)
    {
        if (!ModelState.IsValid)
        {
            await PopulateApplicationsSelectList(interview.ApplicationId);
            await PopulateRecruitersSelectList(interview.RecruiterId);
            return View(interview);
        }

        context.Add(interview);
        await context.SaveChangesAsync();
        await emailNotificationService.SendInterviewScheduledAsync(interview.Id);

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Recruiter")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var interview = await context.Interviews.FindAsync(id);
        if (interview is null)
        {
            return NotFound();
        }

        await PopulateApplicationsSelectList(interview.ApplicationId);
        await PopulateRecruitersSelectList(interview.RecruiterId);
        return View(interview);
    }

    [Authorize(Roles = "Admin,Recruiter")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,ApplicationId,RecruiterId,InterviewDate,InterviewType,Result,Notes")] Interview interview)
    {
        if (id != interview.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateApplicationsSelectList(interview.ApplicationId);
            await PopulateRecruitersSelectList(interview.RecruiterId);
            return View(interview);
        }

        try
        {
            context.Update(interview);
            await context.SaveChangesAsync();
            await emailNotificationService.SendInterviewUpdatedAsync(interview.Id);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await InterviewExists(interview.Id))
            {
                return NotFound();
            }

            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Recruiter")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var interview = await context.Interviews
            .AsNoTracking()
            .Include(item => item.Application)
                .ThenInclude(application => application!.Candidate)
            .Include(item => item.Application)
                .ThenInclude(application => application!.Vacancy)
            .Include(item => item.Recruiter)
            .FirstOrDefaultAsync(item => item.Id == id);

        return interview is null ? NotFound() : View(interview);
    }

    [Authorize(Roles = "Admin,Recruiter")]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var interview = await context.Interviews.FindAsync(id);

        if (interview is not null)
        {
            context.Interviews.Remove(interview);
            await context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateApplicationsSelectList(int? selectedApplicationId = null)
    {
        var applications = await context.Applications
            .AsNoTracking()
            .Include(application => application.Candidate)
            .Include(application => application.Vacancy)
            .Where(application => application.Candidate != null && !application.Candidate.IsDeleted)
            .Where(application => application.Vacancy != null && !application.Vacancy.IsArchived)
            .OrderByDescending(application => application.AppliedAt)
            .ToListAsync();

        var items = applications.Select(application => new
        {
            application.Id,
            Label = $"{application.Candidate?.FullName} - {application.Vacancy?.Title}"
        });

        ViewData["ApplicationId"] = new SelectList(items, "Id", "Label", selectedApplicationId);
    }

    private async Task PopulateRecruitersSelectList(int? selectedRecruiterId = null)
    {
        var recruiters = await context.Recruiters
            .AsNoTracking()
            .OrderBy(recruiter => recruiter.FullName)
            .ToListAsync();

        ViewData["RecruiterId"] = new SelectList(recruiters, "Id", "FullName", selectedRecruiterId);
    }

    private async Task<int?> GetCurrentRecruiterId()
    {
        var idValue = User.FindFirstValue(IdentityRecruiterSyncService.RecruiterIdClaim)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idValue, out var id) && await context.Recruiters.AnyAsync(recruiter => recruiter.Id == id)
            ? id
            : null;
    }

    private async Task<bool> InterviewExists(int id)
    {
        return await context.Interviews.AnyAsync(item => item.Id == id);
    }

    private static string BuildCalendarSummary(Interview interview)
    {
        var candidate = interview.Application?.Candidate?.FullName ?? "Кандидат";
        var vacancy = interview.Application?.Vacancy?.Title ?? "Вакансія";
        return $"{interview.InterviewType}: {candidate} - {vacancy}";
    }

    private static string BuildCalendarDescription(Interview interview)
    {
        var recruiter = interview.Recruiter?.FullName ?? "Не призначено";
        var notes = string.IsNullOrWhiteSpace(interview.Notes) ? "Немає" : interview.Notes;

        return $"Результат: {interview.Result}\nРекрутер: {recruiter}\nНотатки: {notes}";
    }

    private static string BuildIcsCalendar(IEnumerable<Interview> interviews)
    {
        var builder = new StringBuilder();
        builder.AppendLine("BEGIN:VCALENDAR");
        builder.AppendLine("VERSION:2.0");
        builder.AppendLine("PRODID:-//HRReserveSystem//Interview Calendar//UK");
        builder.AppendLine("CALSCALE:GREGORIAN");
        builder.AppendLine("METHOD:PUBLISH");

        foreach (var interview in interviews)
        {
            var start = DateTime.SpecifyKind(interview.InterviewDate, DateTimeKind.Local).ToUniversalTime();
            var end = start.AddHours(1);
            var summary = BuildCalendarSummary(interview);
            var description = BuildCalendarDescription(interview);

            builder.AppendLine("BEGIN:VEVENT");
            builder.AppendLine($"UID:hrreserve-interview-{interview.Id}@hrreserve.local");
            builder.AppendLine($"DTSTAMP:{FormatIcsDate(DateTime.UtcNow)}");
            builder.AppendLine($"DTSTART:{FormatIcsDate(start)}");
            builder.AppendLine($"DTEND:{FormatIcsDate(end)}");
            builder.AppendLine($"SUMMARY:{EscapeIcsText(summary)}");
            builder.AppendLine($"DESCRIPTION:{EscapeIcsText(description)}");
            builder.AppendLine("LOCATION:HRReserveSystem");
            builder.AppendLine("END:VEVENT");
        }

        builder.AppendLine("END:VCALENDAR");
        return builder.ToString();
    }

    private static string FormatIcsDate(DateTime value)
    {
        return value.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'");
    }

    private static string EscapeIcsText(string? value)
    {
        return (value ?? string.Empty)
            .Replace("\\", "\\\\")
            .Replace(";", "\\;")
            .Replace(",", "\\,")
            .Replace("\r\n", "\\n")
            .Replace("\n", "\\n");
    }
}
