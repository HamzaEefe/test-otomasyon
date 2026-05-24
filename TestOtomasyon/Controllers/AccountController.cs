using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Security.Claims;
using TestOtomasyon.Helpers;
using TestOtomasyon.Models;
using TestOtomasyon.Repositories.Interfaces;
using TestOtomasyon.Resources.Languages;

namespace TestOtomasyon.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly IStringLocalizer<Lang> _localizer;

        public AccountController(IUserRepository userRepository, IStringLocalizer<Lang> localizer)
        {
            _userRepository = userRepository;
            _localizer = localizer;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Şifre doğrulama
            var isValid = await _userRepository.ValidatePasswordAsync(model.UserName, model.Password);
            if (!isValid)
            {
                ModelState.AddModelError("", _localizer["Login.InvalidCredentials"]);
                return View(model);
            }

            // Kullanıcı bilgilerini çek
            var user = await _userRepository.GetByUserNameAsync(model.UserName);
            if (user == null)
            {
                ModelState.AddModelError("", _localizer["Login.UserNotFound"]);
                return View(model);
            }

            // Yetkileri çek
            var authorities = await _userRepository.GetAuthoritiesAsync(user.Id);
            var roles = await _userRepository.GetRolesAsync(user.Id);

            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim("UserName", user.UserName),
                new Claim("DepartmentId", user.DepartmentId?.ToString() ?? ""),
                new Claim("DepartmentName", user.DepartmentName ?? "")
            };

            
            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role.Name));

            
            foreach (var auth in authorities)
                claims.Add(new Claim("Authority", auth));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userIdStr = User.GetUserId();
            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction(nameof(Login));

            var userId = Guid.Parse(userIdStr);
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return NotFound();

            var roles = await _userRepository.GetRolesAsync(userId);
            var authorities = await _userRepository.GetAuthoritiesAsync(userId);

            var model = new Models.ProfileViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.Name,
                UserName = user.UserName,
                DepartmentName = user.DepartmentName,
                ParentName = user.ParentName,
                CreatedOn = user.CreatedOn,
                MobilePhone = user.MobilePhone,
                AccountingCode = user.AccountingCode,
                Email = user.Email,
                Roles = roles.Select(r => r.Name).ToList(),
                Authorities = authorities.ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(Models.ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                
                var fullModel = await BuildProfileViewModel(model.Id);
                if (fullModel != null)
                {
                    fullModel.MobilePhone = model.MobilePhone;
                    fullModel.AccountingCode = model.AccountingCode;
                    fullModel.Email = model.Email;
                    return View(fullModel);
                }
                return View(model);
            }

            var user = await _userRepository.GetByIdAsync(model.Id);
            if (user == null)
                return NotFound();

            // Sadece kendi profilini güncelleyebilir
            var currentUserId = User.GetUserId();
            if (currentUserId != model.Id.ToString())
            {
                TempData["Error"] = _localizer["Msg.OnlyOwnProfile"].Value;
                return RedirectToAction(nameof(Profile));
            }

            user.MobilePhone = model.MobilePhone;
            user.AccountingCode = model.AccountingCode;
            user.Email = model.Email;
            await _userRepository.UpdateAsync(user);

            TempData["Success"] = _localizer["Msg.ProfileUpdated"].Value;
            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new Models.ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(Models.ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userIdStr = User.GetUserId();
            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction(nameof(Login));

            var userId = Guid.Parse(userIdStr);
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return NotFound();

            
            var isValid = await _userRepository.ValidatePasswordAsync(user.UserName, model.CurrentPassword);
            if (!isValid)
            {
                ModelState.AddModelError(nameof(model.CurrentPassword), _localizer["Msg.CurrentPasswordWrong"]);
                return View(model);
            }

            // Yeni şifreyi kaydet
            await _userRepository.ResetPasswordAsync(userId, model.NewPassword);

            TempData["Success"] = _localizer["Msg.PasswordChanged"].Value;
            return RedirectToAction(nameof(Profile));
        }

        private async Task<Models.ProfileViewModel?> BuildProfileViewModel(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return null;

            var roles = await _userRepository.GetRolesAsync(userId);
            var authorities = await _userRepository.GetAuthoritiesAsync(userId);

            return new Models.ProfileViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.Name,
                UserName = user.UserName,
                DepartmentName = user.DepartmentName,
                ParentName = user.ParentName,
                CreatedOn = user.CreatedOn,
                MobilePhone = user.MobilePhone,
                Email = user.Email,
                AccountingCode = user.AccountingCode,
                Roles = roles.Select(r => r.Name).ToList(),
                Authorities = authorities.ToList()
            };
        }
    }
}
