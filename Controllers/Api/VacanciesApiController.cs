using HRReserveSystem.Data;
using HRReserveSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRReserveSystem.Controllers.Api;

[ApiController]
[Authorize(Roles = "Admin,Recruiter")]
[Route("api/vacancies")]
public class VacanciesApiController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<VacancyDto>>> GetAll()
    {
        var vacancies = await context.Vacancies
            .AsNoTracking()
            .Where(vacancy => !vacancy.IsArchived)
            .OrderByDescending(vacancy => vacancy.CreatedAt)
            .Select(vacancy => ToDto(vacancy))
            .ToListAsync();

        return Ok(vacancies);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VacancyDto>> GetById(int id)
    {
        var vacancy = await context.Vacancies
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id && !item.IsArchived);

        return vacancy is null ? NotFound() : Ok(ToDto(vacancy));
    }

    [HttpPost]
    public async Task<ActionResult<VacancyDto>> Create(VacancyCreateDto dto)
    {
        var vacancy = new Vacancy
        {
            Title = dto.Title,
            Description = dto.Description,
            Requirements = dto.Requirements,
            SalaryMin = dto.SalaryMin,
            SalaryMax = dto.SalaryMax,
            Status = dto.Status,
            CreatedAt = DateTime.UtcNow
        };

        context.Vacancies.Add(vacancy);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = vacancy.Id }, ToDto(vacancy));
    }

    private static VacancyDto ToDto(Vacancy vacancy)
    {
        return new VacancyDto(
            vacancy.Id,
            vacancy.Title,
            vacancy.Description,
            vacancy.Requirements,
            vacancy.SalaryMin,
            vacancy.SalaryMax,
            vacancy.Status,
            vacancy.CreatedAt);
    }
}
