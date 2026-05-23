using HRReserveSystem.Data;
using HRReserveSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRReserveSystem.Controllers.Api;

[ApiController]
[Authorize(Roles = "Admin,Recruiter")]
[Route("api/candidates")]
public class CandidatesApiController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CandidateDto>>> GetAll()
    {
        var candidates = await context.Candidates
            .AsNoTracking()
            .Where(candidate => !candidate.IsDeleted)
            .OrderBy(candidate => candidate.FullName)
            .Select(candidate => ToDto(candidate))
            .ToListAsync();

        return Ok(candidates);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CandidateDto>> GetById(int id)
    {
        var candidate = await context.Candidates
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);

        return candidate is null ? NotFound() : Ok(ToDto(candidate));
    }

    [HttpPost]
    public async Task<ActionResult<CandidateDto>> Create(CandidateCreateDto dto)
    {
        var normalizedEmail = dto.Email.Trim().ToLower();
        if (await context.Candidates.AnyAsync(candidate => candidate.Email.ToLower() == normalizedEmail))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Duplicate candidate email",
                Detail = "Кандидат із такою електронною поштою вже існує.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var candidate = new Candidate
        {
            FullName = dto.FullName,
            Email = dto.Email,
            Phone = dto.Phone,
            Skills = dto.Skills,
            ExperienceYears = dto.ExperienceYears,
            ResumeSummary = dto.ResumeSummary,
            CreatedAt = DateTime.UtcNow
        };

        context.Candidates.Add(candidate);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = candidate.Id }, ToDto(candidate));
    }

    private static CandidateDto ToDto(Candidate candidate)
    {
        return new CandidateDto(
            candidate.Id,
            candidate.FullName,
            candidate.Email,
            candidate.Phone,
            candidate.Skills,
            candidate.ExperienceYears,
            candidate.ResumeFilePath,
            candidate.ResumeSummary,
            candidate.CreatedAt);
    }
}
