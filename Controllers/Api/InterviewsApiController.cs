using HRReserveSystem.Data;
using HRReserveSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRReserveSystem.Controllers.Api;

[ApiController]
[Authorize(Roles = "Admin,Recruiter,Interviewer")]
[Route("api/interviews")]
public class InterviewsApiController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<InterviewDto>>> GetAll()
    {
        var interviews = await context.Interviews
            .AsNoTracking()
            .Include(interview => interview.Recruiter)
            .OrderByDescending(interview => interview.InterviewDate)
            .Select(interview => ToDto(interview))
            .ToListAsync();

        return Ok(interviews);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InterviewDto>> GetById(int id)
    {
        var interview = await context.Interviews
            .AsNoTracking()
            .Include(item => item.Recruiter)
            .FirstOrDefaultAsync(item => item.Id == id);

        return interview is null ? NotFound() : Ok(ToDto(interview));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Recruiter")]
    public async Task<ActionResult<InterviewDto>> Create(InterviewCreateDto dto)
    {
        if (!await context.Applications.AnyAsync(application => application.Id == dto.ApplicationId))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid application",
                Detail = "Заявку для співбесіди не знайдено.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (dto.RecruiterId.HasValue && !await context.Recruiters.AnyAsync(recruiter => recruiter.Id == dto.RecruiterId.Value))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid recruiter",
                Detail = "Рекрутера для співбесіди не знайдено.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var interview = new Interview
        {
            ApplicationId = dto.ApplicationId,
            RecruiterId = dto.RecruiterId,
            InterviewDate = dto.InterviewDate,
            InterviewType = dto.InterviewType,
            Result = dto.Result,
            Notes = dto.Notes
        };

        context.Interviews.Add(interview);
        await context.SaveChangesAsync();

        await context.Entry(interview).Reference(item => item.Recruiter).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = interview.Id }, ToDto(interview));
    }

    private static InterviewDto ToDto(Interview interview)
    {
        return new InterviewDto(
            interview.Id,
            interview.ApplicationId,
            interview.RecruiterId,
            interview.Recruiter?.FullName,
            interview.InterviewDate,
            interview.InterviewType,
            interview.Result,
            interview.Notes);
    }
}
