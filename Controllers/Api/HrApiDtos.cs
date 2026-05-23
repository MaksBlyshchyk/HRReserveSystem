using System.ComponentModel.DataAnnotations;
using HRReserveSystem.Models;

namespace HRReserveSystem.Controllers.Api;

public sealed record CandidateDto(
    int Id,
    string FullName,
    string Email,
    string? Phone,
    string Skills,
    int ExperienceYears,
    string? ResumeFilePath,
    string? ResumeSummary,
    DateTime CreatedAt);

public sealed class CandidateCreateDto
{
    [Required(ErrorMessage = "Вкажіть ПІБ кандидата.")]
    [StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть електронну пошту.")]
    [EmailAddress(ErrorMessage = "Введіть коректну електронну пошту.")]
    [StringLength(160)]
    public string Email { get; set; } = string.Empty;

    [StringLength(40)]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Опишіть ключові навички кандидата.")]
    [StringLength(1000)]
    public string Skills { get; set; } = string.Empty;

    [Range(0, 60, ErrorMessage = "Досвід має бути від 0 до 60 років.")]
    public int ExperienceYears { get; set; }

    [StringLength(4000)]
    public string? ResumeSummary { get; set; }
}

public sealed record VacancyDto(
    int Id,
    string Title,
    string Description,
    string Requirements,
    decimal SalaryMin,
    decimal SalaryMax,
    string Status,
    DateTime CreatedAt);

public sealed class VacancyCreateDto : IValidatableObject
{
    [Required(ErrorMessage = "Вкажіть назву вакансії.")]
    [StringLength(160)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Опишіть вакансію.")]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть вимоги до вакансії.")]
    [StringLength(2000)]
    public string Requirements { get; set; } = string.Empty;

    [Range(0, 1_000_000, ErrorMessage = "Зарплата має бути від 0 до 1 000 000.")]
    public decimal SalaryMin { get; set; }

    [Range(0, 1_000_000, ErrorMessage = "Зарплата має бути від 0 до 1 000 000.")]
    public decimal SalaryMax { get; set; }

    [Required(ErrorMessage = "Оберіть статус вакансії.")]
    public string Status { get; set; } = "Open";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!HrOptions.VacancyStatuses.Contains(Status))
        {
            yield return new ValidationResult("Оберіть коректний статус вакансії.", [nameof(Status)]);
        }

        if (SalaryMax < SalaryMin)
        {
            yield return new ValidationResult("Максимальна зарплата не може бути меншою за мінімальну.", [nameof(SalaryMax)]);
        }
    }
}

public sealed record ApplicationDto(
    int Id,
    int CandidateId,
    string? CandidateName,
    int VacancyId,
    string? VacancyTitle,
    string Status,
    DateTime AppliedAt,
    string? RecruiterComment);

public sealed class ApplicationCreateDto : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "Оберіть кандидата.")]
    public int CandidateId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Оберіть вакансію.")]
    public int VacancyId { get; set; }

    [Required(ErrorMessage = "Оберіть етап відбору.")]
    public string Status { get; set; } = "New";

    [StringLength(2000)]
    public string? RecruiterComment { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!HrOptions.ApplicationStatuses.Contains(Status))
        {
            yield return new ValidationResult("Оберіть коректний етап відбору.", [nameof(Status)]);
        }
    }
}

public sealed record InterviewDto(
    int Id,
    int ApplicationId,
    int? RecruiterId,
    string? RecruiterName,
    DateTime InterviewDate,
    string InterviewType,
    string Result,
    string? Notes);

public sealed class InterviewCreateDto : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "Оберіть заявку.")]
    public int ApplicationId { get; set; }

    public int? RecruiterId { get; set; }

    public DateTime InterviewDate { get; set; } = DateTime.Now;

    [Required(ErrorMessage = "Оберіть тип співбесіди.")]
    public string InterviewType { get; set; } = "HR";

    [Required(ErrorMessage = "Оберіть результат співбесіди.")]
    public string Result { get; set; } = "Pending";

    [StringLength(2000)]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!HrOptions.InterviewTypes.Contains(InterviewType))
        {
            yield return new ValidationResult("Оберіть коректний тип співбесіди.", [nameof(InterviewType)]);
        }

        if (!HrOptions.InterviewResults.Contains(Result))
        {
            yield return new ValidationResult("Оберіть коректний результат співбесіди.", [nameof(Result)]);
        }
    }
}

public sealed record SoftSkillDto(
    int Id,
    int CandidateId,
    string? CandidateName,
    int Communication,
    int Teamwork,
    int Responsibility,
    int StressResistance,
    int Leadership,
    double AverageScore,
    string? OverallComment);

public sealed class SoftSkillCreateDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Оберіть кандидата.")]
    public int CandidateId { get; set; }

    [Range(1, 10, ErrorMessage = "Оцінка має бути від 1 до 10.")]
    public int Communication { get; set; } = 5;

    [Range(1, 10, ErrorMessage = "Оцінка має бути від 1 до 10.")]
    public int Teamwork { get; set; } = 5;

    [Range(1, 10, ErrorMessage = "Оцінка має бути від 1 до 10.")]
    public int Responsibility { get; set; } = 5;

    [Range(1, 10, ErrorMessage = "Оцінка має бути від 1 до 10.")]
    public int StressResistance { get; set; } = 5;

    [Range(1, 10, ErrorMessage = "Оцінка має бути від 1 до 10.")]
    public int Leadership { get; set; } = 5;

    [StringLength(2000)]
    public string? OverallComment { get; set; }
}
