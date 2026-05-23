using System.ComponentModel.DataAnnotations;

namespace HRReserveSystem.ViewModels;

public class RecruiterFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Вкажіть ПІБ рекрутера.")]
    [StringLength(120, ErrorMessage = "ПІБ має містити не більше 120 символів.")]
    [Display(Name = "ПІБ")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть електронну пошту.")]
    [EmailAddress(ErrorMessage = "Введіть коректну електронну пошту.")]
    [StringLength(160, ErrorMessage = "Email має містити не більше 160 символів.")]
    [Display(Name = "Електронна пошта")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть логін.")]
    [StringLength(60, ErrorMessage = "Логін має містити не більше 60 символів.")]
    [Display(Name = "Логін")]
    public string Login { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "Пароль має містити щонайменше 6 символів.")]
    [Display(Name = "Пароль")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "Оберіть роль.")]
    [StringLength(40)]
    [Display(Name = "Роль")]
    public string Role { get; set; } = "Recruiter";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
