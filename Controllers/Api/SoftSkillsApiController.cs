using HRReserveSystem.Data;
using HRReserveSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRReserveSystem.Controllers.Api;

[ApiController]
[Authorize(Roles = "Admin,Interviewer")]
[Route("api/soft-skills")]
public class SoftSkillsApiController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SoftSkillDto>>> GetAll()
    {
        var assessments = await context.SoftSkillAssessments
            .AsNoTracking()
            .Include(assessment => assessment.Candidate)
            .OrderBy(assessment => assessment.Candidate!.FullName)
            .Select(assessment => ToDto(assessment))
            .ToListAsync();

        return Ok(assessments);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SoftSkillDto>> GetById(int id)
    {
        var assessment = await context.SoftSkillAssessments
            .AsNoTracking()
            .Include(item => item.Candidate)
            .FirstOrDefaultAsync(item => item.Id == id);

        return assessment is null ? NotFound() : Ok(ToDto(assessment));
    }

    [HttpPost]
    public async Task<ActionResult<SoftSkillDto>> Create(SoftSkillCreateDto dto)
    {
        if (!await context.Candidates.AnyAsync(candidate => candidate.Id == dto.CandidateId && !candidate.IsDeleted))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid candidate",
                Detail = "Кандидата для оцінки soft skills не знайдено.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var assessment = new SoftSkillAssessment
        {
            CandidateId = dto.CandidateId,
            Communication = dto.Communication,
            Teamwork = dto.Teamwork,
            Responsibility = dto.Responsibility,
            StressResistance = dto.StressResistance,
            Leadership = dto.Leadership,
            OverallComment = dto.OverallComment
        };

        context.SoftSkillAssessments.Add(assessment);
        await context.SaveChangesAsync();

        await context.Entry(assessment).Reference(item => item.Candidate).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = assessment.Id }, ToDto(assessment));
    }

    private static SoftSkillDto ToDto(SoftSkillAssessment assessment)
    {
        return new SoftSkillDto(
            assessment.Id,
            assessment.CandidateId,
            assessment.Candidate?.FullName,
            assessment.Communication,
            assessment.Teamwork,
            assessment.Responsibility,
            assessment.StressResistance,
            assessment.Leadership,
            assessment.AverageScore,
            assessment.OverallComment);
    }
}
