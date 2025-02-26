using EventHubASP.Core;
using EventHubASP.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

public class AccountController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly UserService _userService;

    public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, RoleManager<IdentityRole<Guid>> roleManager, UserService userService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _userService = userService;
    }

    public IActionResult Register()
    {
        return View();
    }

    public async Task<IActionResult> ClearCookies()
    {
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

        foreach (var cookie in Request.Cookies.Keys)
        {
            Response.Cookies.Delete(cookie);
        }

        return RedirectToAction("ContactUs", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _userService.RegisterUserAsync(model.Username, model.Email, model.Password);
            if (result)
            {
                var user = await _userManager.FindByNameAsync(model.Username);
                await _userManager.AddToRoleAsync(user, "User");
                await _signInManager.SignInAsync(user, isPersistent: false);
                HttpContext.Session.SetString("ShowCookieNotice", "true");
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Registration failed. Username or email might already be taken.");
        }
        return View(model);
    }

    [HttpPost]
    public IActionResult EnableCookies()
    {

        HttpContext.Response.Cookies.Append("CookieAccepted", "true", new CookieOptions
        {
            Path = "/",
            HttpOnly = true,
            IsEssential = true,
            Expires = DateTimeOffset.UtcNow.AddYears(1)
        });

        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> CheckUserExistence(string emailOrUsername)
    {
        var user = await _userManager.FindByNameAsync(emailOrUsername) ?? await _userManager.FindByEmailAsync(emailOrUsername);
        return Json(new { exists = user != null });
    }


    [HttpGet]
    public async Task<IActionResult> CheckUsername(string username)
    {
        var user = await _userManager.FindByNameAsync(username);
        return Json(new { isAvailable = user == null });
    }

    [HttpGet]
    public async Task<IActionResult> CheckEmail(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return Json(new { isAvailable = user == null });
    }

    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userService.ValidateUserAsync(model.EmailOrUsername.ToLower(), model.Password);

            if (user != null)
            {
                await _signInManager.SignInAsync(user, isPersistent: true);
                var roles = await _userManager.GetRolesAsync(user);
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ModelState.AddModelError("", "Invalid login attempt. Wrong password or non existing account.");
            }
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}