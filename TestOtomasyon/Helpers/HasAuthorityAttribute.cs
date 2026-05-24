using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TestOtomasyon.Helpers
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class HasAuthorityAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _authorityName;

        public HasAuthorityAttribute(string authorityName)
        {
            _authorityName = authorityName;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            // Kullanıcı login değilse → Login sayfasına
            if (user.Identity == null || !user.Identity.IsAuthenticated)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // Yetki kontrolü
            var hasAuthority = user.Claims
                .Any(c => c.Type == "Authority" && c.Value == _authorityName);

            if (!hasAuthority)
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                return;
            }
        }
    }
}