using System.ComponentModel.DataAnnotations;

namespace HRReserveSystem.Models;

public class Interview : IValidatableObject
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Оберіть заявку.")]
    [Display(Name = "Заявка")]
    public int ApplicationId { get; set; }

    [Display(Name = "Рекрутер")]
    public int? RecruiterId { get; set; }

    [Display(Name = "Дата співбесіди")]
    public DateTime InterviewDate { get; set; } = DateTime.Now;

    [Required(ErrorMessage = "Оберіть тип співбесіди.")]
    [StringLength(80)]
    [Display(Name = "Тип співбесіди")]
    public string InterviewType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Оберіть результат співбесіди.")]
    [StringLength(80)]
    [Display(Name = "Результат")]
    public string Result { get; set; } = "Pending";

    [StringLength(2000)]
    [Display(Name = "Нотатки")]
    public string? Notes { get; set; }

    public Application? Application { get; set; }

    public Recruiter? Recruiter { get; set; }

    public ICollection<InterviewFeedback> Feedbacks { get; set; } = new List<InterviewFeedback>();

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
