using Microsoft.Extensions.Localization;
using TestOtomasyon.Resources.Languages;

namespace TestOtomasyon.Helpers
{
    public static class LocalizationAccessor
    {
        private static IStringLocalizer<Lang>? _localizer;

        public static void Configure(IServiceProvider services)
        {
            _localizer = services.GetService(typeof(IStringLocalizer<Lang>)) as IStringLocalizer<Lang>;
        }

        public static string T(string key)
        {
            if (_localizer == null) return key;
            var value = _localizer[key];
            return value.ResourceNotFound ? key : value.Value;
        }

        public static string T(string key, params object[] args)
        {
            if (_localizer == null) return key;
            var value = _localizer[key, args];
            return value.ResourceNotFound ? key : value.Value;
        }
    }
}
