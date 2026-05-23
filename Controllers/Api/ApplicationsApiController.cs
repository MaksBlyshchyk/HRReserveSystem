using HRReserveSystem.Data;
using HRReserveSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRReserveSystem.Controllers.Api;

[ApiController]
[Authorize(Roles = "Admin,Recruiter")]
[Route("api/applications")]
public class ApplicationsApiController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApplicationDto>>> GetAll()
    {
        var applications = await context.Applications
            .AsNoTracking()
            .Include(application => application.Candidate)
            .Include(application => application.Vacancy)
            .OrderByDescending(application => application.AppliedAt)
            .Select(application => ToDto(application))
            .ToListAsync();

        return Ok(applications);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApplicationDto>> GetById(int id)
    {
        var application = await context.Applications
            .AsNoTracking()
            .Include(item => item.Candidate)
            .Include(item => item.Vacancy)
            .FirstOrDefaultAsync(item => item.Id == id);

        return application is null ? NotFound() : Ok(ToDto(application));
    }

    [HttpPost]
    public async Task<ActionResult<ApplicationDto>> Create(ApplicationCreateDto dto)
    {
        var candidateExists = await context.Candidates.AnyAsync(candidate => candidate.Id == dto.CandidateId && !candidate.IsDeleted);
        var vacancyExists = await context.Vacancies.AnyAsync(vacancy => vacancy.Id == dto.VacancyId && !vacancy.IsArchived);
        if (!candidateExists || !vacancyExists)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid application reference",
                Detail = "Кандидат або вакансія не знайдені чи архівні.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (await context.Applications.AnyAsync(item => item.CandidateId == dto.CandidateId && item.VacancyId == dto.VacancyId))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Duplicate application",
                Detail = "Заявка для цього кандидата на цю вакансію вже існує.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var application = new Application
        {
            CandidateId = dto.CandidateId,
            VacancyId = dto.VacancyId,
            Status = dto.Status,
            RecruiterComment = dto.RecruiterComment,
            AppliedAt = DateTime.UtcNow
        };

        context.Applications.Add(application);
        await context.SaveChangesAsync();

        await context.Entry(application).Reference(item => item.Candidate).LoadAsync();
        await context.Entry(application).Reference(item => item.Vacancy).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = application.Id }, ToDto(application));
    }

    private static ApplicationDto ToDto(Application application)
    {
        return new ApplicationDto(
            application.Id,
            application.CandidateId,
            application.Candidate?.FullName,
            application.VacancyId,
            application.Vacancy?.Title,
            application.Status,
            application.AppliedAt,
            application.RecruiterComment);
    }
}
