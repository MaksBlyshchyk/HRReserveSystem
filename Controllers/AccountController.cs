using HRReserveSystem.Services;
using HRReserveSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HRReserveSystem.Controllers;

public class AccountController(
    DemoUserService demoUserService,
    IdentityRecruiterSyncService identitySync,
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager) : Controller
{
    [AllowAnonymous]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["DemoUsers"] = await demoUserService.GetDemoUsersAsync();
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        ViewData["DemoUsers"] = await demoUserService.GetDemoUsersAsync();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var recruiter = await demoUserService.ValidateUserAsync(model.Login, model.Password);
        if (recruiter is null)
        {
            ModelState.AddModelError(string.Empty, "Невірний логін або пароль.");
            return View(model);
        }

        var user = await userManager.FindByIdAsync(recruiter.Id.ToString());
        if (user is null)
        {
            var identityErrors = await identitySync.SyncRecruiterAsync(recruiter);
            if (identityErrors.Count > 0)
            {
                ModelState.AddModelError(string.Empty, "Користувача не вдалося синхронізувати з Identity.");
                return View(model);
            }

            user = await userManager.FindByIdAsync(recruiter.Id.ToString());
            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Користувача не знайдено в Identity.");
                return View(model);
            }
        }

        await signInManager.SignInAsync(user, model.RememberMe);

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
