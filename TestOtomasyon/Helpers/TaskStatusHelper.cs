namespace TestOtomasyon.Helpers
{
    public static class TaskStatusHelper
    {
        public const int Atandi = 0;
        public const int Basladi = 1;
        public const int Tamamlandi = 2;
        public const int IptalEdildi = 3;
        public const int OnayBekliyor = 4;
        public const int Reddedildi = 5;

        public static string GetName(int status) => status switch
        {
            0 => LocalizationAccessor.T("Status.Assigned"),
            1 => LocalizationAccessor.T("Status.Started"),
            2 => LocalizationAccessor.T("Status.Completed"),
            3 => LocalizationAccessor.T("Status.Cancelled"),
            4 => LocalizationAccessor.T("Status.PendingApproval"),
            5 => LocalizationAccessor.T("Status.Rejected"),
            _ => LocalizationAccessor.T("Common.Unknown")
        };

        public static string GetBadgeClass(int status) => status switch
        {
            0 => "bg-secondary",
            1 => "bg-warning text-dark",
            2 => "bg-success",
            3 => "bg-danger",
            4 => "bg-info text-dark",
            5 => "bg-dark",
            _ => "bg-light text-dark"
        };

        public static string GetIcon(int status) => status switch
        {
            0 => "📌",
            1 => "🔄",
            2 => "✅",
            3 => "❌",
            4 => "⏳",
            5 => "🚫",
            _ => "❓"
        };

        public static string GetColumnColor(int status) => status switch
        {
            4 => "#a855f7",
            0 => "#f59e0b",
            1 => "#06b6d4",
            2 => "#10b981",
            _ => "#94a3b8"
        };
    }
}
