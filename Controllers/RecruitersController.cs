using HRReserveSystem.Data;
using HRReserveSystem.Models;
using HRReserveSystem.Services;
using HRReserveSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRReserveSystem.Controllers;

[Authorize(Roles = "Admin")]
public class RecruitersController(
    ApplicationDbContext context,
    IdentityRecruiterSyncService identitySync,
    IPasswordHasher<Recruiter> passwordHasher) : Controller
{
    public async Task<IActionResult> Index()
    {
        return View(await context.Recruiters
            .AsNoTracking()
            .OrderBy(recruiter => recruiter.FullName)
            .ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var recruiter = await context.Recruiters
            .AsNoTracking()
            .Include(item => item.Interviews)
            .Include(item => item.Feedbacks)
            .FirstOrDefaultAsync(item => item.Id == id);

        return recruiter is null ? NotFound() : View(recruiter);
    }

    public IActionResult Create()
    {
        return View(new RecruiterFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RecruiterFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(RecruiterFormViewModel.Password), "Вкажіть пароль.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var recruiter = new Recruiter
        {
            FullName = model.FullName,
            Email = model.Email,
            Login = model.Login,
            Role = model.Role,
            CreatedAt = DateTime.UtcNow
        };
        recruiter.PasswordHash = passwordHasher.HashPassword(recruiter, model.Password!);

        context.Recruiters.Add(recruiter);
        await context.SaveChangesAsync();

        var identityErrors = await identitySync.SyncRecruiterAsync(recruiter);
        if (identityErrors.Count > 0)
        {
            foreach (var error in identityErrors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var recruiter = await context.Recruiters.FindAsync(id);
        return recruiter is null ? NotFound() : View(ToFormModel(recruiter));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RecruiterFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var recruiter = await context.Recruiters.FindAsync(id);
            if (recruiter is null)
            {
                return NotFound();
            }

            recruiter.FullName = model.FullName;
            recruiter.Email = model.Email;
            recruiter.Login = model.Login;
            recruiter.Role = model.Role;

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                recruiter.PasswordHash = passwordHasher.HashPassword(recruiter, model.Password);
            }

            await context.SaveChangesAsync();

            var identityErrors = await identitySync.SyncRecruiterAsync(recruiter);
            if (identityErrors.Count > 0)
            {
                foreach (var error in identityErrors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }

                return View(model);
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await RecruiterExists(model.Id))
            {
                return NotFound();
            }

            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var recruiter = await context.Recruiters
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        return recruiter is null ? NotFound() : View(recruiter);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var recruiter = await context.Recruiters.FindAsync(id);

        if (recruiter is not null)
        {
            context.Recruiters.Remove(recruiter);
            await context.SaveChangesAsync();
            await identitySync.DeleteRecruiterAsync(id);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> RecruiterExists(int id)
    {
        return await context.Recruiters.AnyAsync(item => item.Id == id);
    }

    private static RecruiterFormViewModel ToFormModel(Recruiter recruiter)
    {
        return new RecruiterFormViewModel
        {
            Id = recruiter.Id,
            FullName = recruiter.FullName,
            Email = recruiter.Email,
            Login = recruiter.Login,
            Role = recruiter.Role,
            CreatedAt = recruiter.CreatedAt
        };
    }
}
