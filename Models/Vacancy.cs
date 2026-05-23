using System.ComponentModel.DataAnnotations;

namespace HRReserveSystem.Models;

public class Vacancy : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Вкажіть назву вакансії.")]
    [StringLength(160, ErrorMessage = "Назва має містити не більше 160 символів.")]
    [Display(Name = "Назва")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Опишіть вакансію.")]
    [StringLength(2000, ErrorMessage = "Опис має містити не більше 2000 символів.")]
    [Display(Name = "Опис")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть вимоги до вакансії.")]
    [StringLength(2000, ErrorMessage = "Вимоги мають містити не більше 2000 символів.")]
    [Display(Name = "Вимоги")]
    public string Requirements { get; set; } = string.Empty;

    [Range(0, 1_000_000, ErrorMessage = "Зарплата має бути від 0 до 1 000 000.")]
    [Display(Name = "Зарплата від")]
    public decimal SalaryMin { get; set; }

    [Range(0, 1_000_000, ErrorMessage = "Зарплата має бути від 0 до 1 000 000.")]
    [Display(Name = "Зарплата до")]
    public decimal SalaryMax { get; set; }

    [Required(ErrorMessage = "Оберіть статус вакансії.")]
    [StringLength(40)]
    [Display(Name = "Статус")]
    public string Status { get; set; } = "Open";

    [Display(Name = "Дата створення")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Архівна")]
    public bool IsArchived { get; set; }

    public ICollection<Application> Applications { get; set; } = new List<Application>();

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
