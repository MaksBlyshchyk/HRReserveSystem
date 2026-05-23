using System.ComponentModel.DataAnnotations;

namespace HRReserveSystem.Models;

public class InterviewFeedback : IValidatableObject
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Оберіть співбесіду.")]
    [Display(Name = "Співбесіда")]
    public int InterviewId { get; set; }

    [Display(Name = "Автор відгуку")]
    public int? RecruiterId { get; set; }

    [Required(ErrorMessage = "Вкажіть коментар інтерв'юера.")]
    [StringLength(2000)]
    [Display(Name = "Коментар")]
    public string Comment { get; set; } = string.Empty;

    [Range(1, 10, ErrorMessage = "Оцінка має бути від 1 до 10.")]
    [Display(Name = "Оцінка")]
    public int Score { get; set; } = 5;

    [Required(ErrorMessage = "Оберіть рекомендацію.")]
    [StringLength(120)]
    [Display(Name = "Рекомендація")]
    public string Recommendation { get; set; } = string.Empty;

    [Display(Name = "Створено")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Interview? Interview { get; set; }

    public Recruiter? Recruiter { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!HrOptions.FeedbackRecommendations.Contains(Recommendation))
        {
            yield return new ValidationResult("Оберіть коректну рекомендацію.", [nameof(Recommendation)]);
        }
    }
}
