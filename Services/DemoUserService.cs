using HRReserveSystem.Data;
using HRReserveSystem.Models;
using HRReserveSystem.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HRReserveSystem.Services;

public class DemoUserService(ApplicationDbContext context, IPasswordHasher<Recruiter> passwordHasher)
{
    public async Task<Recruiter?> ValidateUserAsync(string login, string password)
    {
        var normalizedLogin = login.Trim().ToLower();

        var recruiter = await context.Recruiters
            .FirstOrDefaultAsync(item =>
                item.Login.ToLower() == normalizedLogin ||
                item.Email.ToLower() == normalizedLogin);

        if (recruiter is null)
        {
            return null;
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(recruiter, recruiter.PasswordHash, password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            recruiter.PasswordHash = passwordHasher.HashPassword(recruiter, password);
            await context.SaveChangesAsync();
        }

        return recruiter;
    }

    public async Task<IReadOnlyList<DemoUserViewModel>> GetDemoUsersAsync()
    {
        var demoLogins = DemoCredentials.Users.Select(user => user.Login).ToArray();
        var recruiters = await context.Recruiters
            .AsNoTracking()
            .Where(recruiter => demoLogins.Contains(recruiter.Login))
            .OrderBy(recruiter => recruiter.Id)
            .ToListAsync();

        return recruiters
            .Select(recruiter => new DemoUserViewModel(
                recruiter.Login,
                DemoCredentials.GetPassword(recruiter.Login) ?? string.Empty,
                recruiter.Role))
            .ToList();
    }
}
