using System.ComponentModel.DataAnnotations;

namespace HRReserveSystem.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Вкажіть логін або email.")]
    [Display(Name = "Логін або email")]
    public string Login { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть пароль.")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Запам'ятати мене")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
