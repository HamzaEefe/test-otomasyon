namespace TestOtomasyon.Models
{
    public class DashboardViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public List<string> Roles { get; set; } = new();
        public int AuthorityCount { get; set; }

        
        public int MyTaskAssigned { get; set; }      
        public int MyTaskInProgress { get; set; }   
        public int MyTaskCompleted { get; set; }    
        public int MyTaskCancelled { get; set; }   
        public int MyTaskTotal => MyTaskAssigned + MyTaskInProgress + MyTaskCompleted + MyTaskCancelled;

       
        public int AssignedByMeTotal { get; set; }
        public int TeamSize { get; set; }
        public int TeamActiveTasks { get; set; }

       
        public int TotalUsers { get; set; }
        public int TotalDepartments { get; set; }
        public int TotalTasks { get; set; }
        public Dictionary<int, int> SystemTaskBreakdown { get; set; } = new();

        
        public bool ShowPersonelPanel { get; set; }
        public bool ShowSahaAmiriPanel { get; set; }
        public bool ShowAdminPanel { get; set; }
    }
}