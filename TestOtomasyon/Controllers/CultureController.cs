using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace TestOtomasyon.Controllers
{
    [AllowAnonymous]
    public class CultureController : Controller
    {
        [HttpGet]
        public IActionResult SetLanguage(string culture, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(culture))
                culture = "tr-TR";

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    HttpOnly = false
                });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }
    }
}
