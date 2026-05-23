using HRReserveSystem.Data;
using HRReserveSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace HRReserveSystem.Controllers;

[Authorize(Roles = "Admin,Recruiter")]
public class CandidatesController(ApplicationDbContext context, IWebHostEnvironment environment) : Controller
{
    private const long MaxResumeFileSize = 5 * 1024 * 1024;
    private static readonly string[] AllowedResumeExtensions = [".pdf", ".doc", ".docx"];

    public async Task<IActionResult> Index(string? search, int? minExperience)
    {
        var candidates = ApplyFilters(context.Candidates.AsNoTracking(), search, minExperience);

        ViewData["CurrentFilter"] = search;
        ViewData["MinExperience"] = minExperience;

        return View(await candidates
            .OrderBy(candidate => candidate.FullName)
            .ToListAsync());
    }

    public async Task<IActionResult> ExportCsv(string? search, int? minExperience)
    {
        var candidates = await ApplyFilters(context.Candidates.AsNoTracking(), search, minExperience)
            .OrderBy(candidate => candidate.FullName)
            .ToListAsync();

        var csv = new StringBuilder();
        AppendCsvRow(csv, "Id", "FullName", "Email", "Phone", "Skills", "ExperienceYears", "ResumeFilePath", "ResumeSummary", "CreatedAt");

        foreach (var candidate in candidates)
        {
            AppendCsvRow(
                csv,
                candidate.Id,
                candidate.FullName,
                candidate.Email,
                candidate.Phone,
                candidate.Skills,
                candidate.ExperienceYears,
                candidate.ResumeFilePath,
                candidate.ResumeSummary,
                candidate.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
        }

        var preamble = Encoding.UTF8.GetPreamble();
        var content = Encoding.UTF8.GetBytes(csv.ToString());
        var fileBytes = new byte[preamble.Length + content.Length];
        Buffer.BlockCopy(preamble, 0, fileBytes, 0, preamble.Length);
        Buffer.BlockCopy(content, 0, fileBytes, preamble.Length, content.Length);

        var fileName = $"candidates-{DateTime.Today:yyyy-MM-dd}.csv";
        return File(fileBytes, "text/csv; charset=utf-8", fileName);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var candidate = await context.Candidates
            .AsNoTracking()
            .Include(item => item.Applications)
                .ThenInclude(application => application.Vacancy)
            .Include(item => item.SoftSkillAssessments)
            .FirstOrDefaultAsync(item => item.Id == id);

        return candidate is null ? NotFound() : View(candidate);
    }

    public async Task<IActionResult> DownloadResume(int id)
    {
        var candidate = await context.Candidates
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (candidate is null || string.IsNullOrWhiteSpace(candidate.ResumeFilePath))
        {
            return NotFound();
        }

        if (!candidate.ResumeFilePath.StartsWith("/uploads/resumes/", StringComparison.OrdinalIgnoreCase))
        {
            return Redirect(candidate.ResumeFilePath);
        }

        var fileName = Path.GetFileName(candidate.ResumeFilePath);
        var physicalPath = Path.Combine(environment.WebRootPath, "uploads", "resumes", fileName);
        if (!System.IO.File.Exists(physicalPath))
        {
            return NotFound();
        }

        return PhysicalFile(physicalPath, GetResumeContentType(fileName), fileName);
    }

    public IActionResult Create()
    {
        return View(new Candidate());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("FullName,Email,Phone,Skills,ResumeFilePath,ResumeSummary,ExperienceYears")] Candidate candidate, IFormFile? resumeFile)
    {
        ValidateResumeFile(resumeFile);

        if (!ModelState.IsValid)
        {
            return View(candidate);
        }

        var uploadedResumePath = await SaveResumeFileAsync(resumeFile);
        if (!string.IsNullOrWhiteSpace(uploadedResumePath))
        {
            candidate.ResumeFilePath = uploadedResumePath;
        }

        candidate.CreatedAt = DateTime.UtcNow;
        context.Add(candidate);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadResume(int id, IFormFile? resumeFile)
    {
        var candidate = await context.Candidates.FindAsync(id);
        if (candidate is null)
        {
            return NotFound();
        }

        ValidateResumeFile(resumeFile, requireFile: true);
        if (!ModelState.IsValid)
        {
            var resumeError = ModelState.TryGetValue(nameof(Candidate.ResumeFilePath), out var entry)
                ? entry.Errors.FirstOrDefault()?.ErrorMessage
                : null;
            TempData["ResumeError"] = resumeError ?? "Файл резюме не вибрано.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var oldResumePath = candidate.ResumeFilePath;
        var uploadedResumePath = await SaveResumeFileAsync(resumeFile);
        if (string.IsNullOrWhiteSpace(uploadedResumePath))
        {
            TempData["ResumeError"] = "Файл резюме не вибрано.";
            return RedirectToAction(nameof(Details), new { id });
        }

        DeleteStoredResume(oldResumePath);
        candidate.ResumeFilePath = uploadedResumePath;
        await context.SaveChangesAsync();

        TempData["ResumeMessage"] = "Резюме кандидата оновлено.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var candidate = await context.Candidates.FindAsync(id);
        return candidate is null ? NotFound() : View(candidate);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,Email,Phone,Skills,ResumeFilePath,ResumeSummary,ExperienceYears,CreatedAt")] Candidate candidate, IFormFile? resumeFile, bool removeResume = false)
    {
        if (id != candidate.Id)
        {
            return NotFound();
        }

        ValidateResumeFile(resumeFile);

        if (!ModelState.IsValid)
        {
            return View(candidate);
        }

        try
        {
            var oldResumePath = await context.Candidates
                .AsNoTracking()
                .Where(item => item.Id == id)
                .Select(item => item.ResumeFilePath)
                .FirstOrDefaultAsync();

            if (removeResume)
            {
                DeleteStoredResume(oldResumePath);
                candidate.ResumeFilePath = null;
            }

            var uploadedResumePath = await SaveResumeFileAsync(resumeFile);
            if (!string.IsNullOrWhiteSpace(uploadedResumePath))
            {
                DeleteStoredResume(oldResumePath);
                candidate.ResumeFilePath = uploadedResumePath;
            }

            context.Update(candidate);
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await CandidateExists(candidate.Id))
            {
                return NotFound();
            }

            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var candidate = await context.Candidates
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        return candidate is null ? NotFound() : View(candidate);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var candidate = await context.Candidates.FindAsync(id);

        if (candidate is not null)
        {
            context.Candidates.Remove(candidate);
            await context.SaveChangesAsync();
            DeleteStoredResume(candidate.ResumeFilePath);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> CandidateExists(int id)
    {
        return await context.Candidates.AnyAsync(item => item.Id == id);
    }

    private static IQueryable<Candidate> ApplyFilters(IQueryable<Candidate> candidates, string? search, int? minExperience)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            candidates = candidates.Where(candidate =>
                candidate.FullName.Contains(search) ||
                candidate.Email.Contains(search) ||
                candidate.Skills.Contains(search));
        }

        if (minExperience.HasValue)
        {
            candidates = candidates.Where(candidate => candidate.ExperienceYears >= minExperience.Value);
        }

        return candidates;
    }

    private static void AppendCsvRow(StringBuilder builder, params object?[] values)
    {
        builder.AppendLine(string.Join(",", values.Select(EscapeCsv)));
    }

    private static string EscapeCsv(object? value)
    {
        var text = value?.ToString() ?? string.Empty;

        return text.Contains('"') || text.Contains(',') || text.Contains('\r') || text.Contains('\n')
            ? $"\"{text.Replace("\"", "\"\"")}\""
            : text;
    }

    private void ValidateResumeFile(IFormFile? resumeFile, bool requireFile = false)
    {
        if (resumeFile is null || resumeFile.Length == 0)
        {
            if (requireFile)
            {
                ModelState.AddModelError(nameof(Candidate.ResumeFilePath), "Файл резюме не вибрано.");
            }

            return;
        }

        var extension = Path.GetExtension(resumeFile.FileName).ToLowerInvariant();
        if (!AllowedResumeExtensions.Contains(extension))
        {
            ModelState.AddModelError(nameof(Candidate.ResumeFilePath), "Дозволені формати резюме: PDF, DOC, DOCX.");
        }

        if (resumeFile.Length > MaxResumeFileSize)
        {
            ModelState.AddModelError(nameof(Candidate.ResumeFilePath), "Файл резюме має бути не більшим за 5 MB.");
        }
    }

    private async Task<string?> SaveResumeFileAsync(IFormFile? resumeFile)
    {
        if (resumeFile is null || resumeFile.Length == 0)
        {
            return null;
        }

        var extension = Path.GetExtension(resumeFile.FileName).ToLowerInvariant();
        var uploadsPath = Path.Combine(environment.WebRootPath, "uploads", "resumes");
        Directory.CreateDirectory(uploadsPath);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(uploadsPath, fileName);

        await using var stream = System.IO.File.Create(physicalPath);
        await resumeFile.CopyToAsync(stream);

        return $"/uploads/resumes/{fileName}";
    }

    private static string GetResumeContentType(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };
    }

    private void DeleteStoredResume(string? resumePath)
    {
        if (string.IsNullOrWhiteSpace(resumePath) || !resumePath.StartsWith("/uploads/resumes/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var fileName = Path.GetFileName(resumePath);
        var physicalPath = Path.Combine(environment.WebRootPath, "uploads", "resumes", fileName);
        if (System.IO.File.Exists(physicalPath))
        {
            System.IO.File.Delete(physicalPath);
        }
    }
}
