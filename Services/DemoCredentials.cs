namespace HRReserveSystem.Services;

public sealed record DemoCredential(string FullName, string Email, string Login, string Password, string Role);

public static class DemoCredentials
{
    public static readonly IReadOnlyList<DemoCredential> Users =
    [
        new("Адміністратор системи", "admin@hrreserve.local", "admin", "admin123", "Admin"),
        new("Оксана Рекрутер", "recruiter@hrreserve.local", "recruiter", "recruiter123", "Recruiter"),
        new("Ігор Інтерв'юер", "interviewer@hrreserve.local", "interviewer", "interviewer123", "Interviewer")
    ];

    public static string? GetPassword(string login)
    {
        return Users.FirstOrDefault(user => user.Login.Equals(login, StringComparison.OrdinalIgnoreCase))?.Password;
    }
}
