namespace TestOtomasyon.Helpers
{
    public static class AuthorityNameHelper
    {
        private static readonly Dictionary<string, string> CategoryKeys = new()
        {
            { "User-Manage", "AuthCat.Management" },
            { "Role-Manage", "AuthCat.Management" },
            { "Department-Manage", "AuthCat.Management" },
            { "Authority-Manage", "AuthCat.Management" },
            { "Task-Create", "AuthCat.TaskOps" },
            { "Task-Assign", "AuthCat.TaskOps" },
            { "Task-View", "AuthCat.TaskOps" },
            { "Task-Update", "AuthCat.TaskOps" },
            { "Task-ViewAll", "AuthCat.TaskOps" },
            { "Task-Propose", "AuthCat.TaskOps" },
            { "Task-Approve", "AuthCat.TaskOps" },
            { "Task-SystemView", "AuthCat.TaskOps" },
            { "Dashboard-View", "AuthCat.General" },
            { "Organization-View", "AuthCat.General" },
        };

        public static string GetDisplayName(string authorityCode)
        {
            if (string.IsNullOrEmpty(authorityCode))
                return string.Empty;

            return LocalizationAccessor.T("Authority." + authorityCode);
        }

        public static string GetCategory(string authorityCode)
        {
            if (string.IsNullOrEmpty(authorityCode))
                return LocalizationAccessor.T("Common.Other");

            return CategoryKeys.TryGetValue(authorityCode, out var catKey)
                ? LocalizationAccessor.T(catKey)
                : LocalizationAccessor.T("Common.Other");
        }
    }
}
