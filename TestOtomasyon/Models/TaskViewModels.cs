using System.ComponentModel.DataAnnotations;
using TestOtomasyon.Entities;

namespace TestOtomasyon.Models
{
    public class KanbanColumn
    {
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public List<WorkTask> Tasks { get; set; } = new();
    }

    public class SystemDistributionViewModel
    {
        public KanbanColumn PendingApproval { get; set; } = new() { Title = "Kanban.ColPendingApproval", Icon = "⏳", Color = "#a855f7" };
        public KanbanColumn Assigned { get; set; } = new() { Title = "Kanban.ColAssigned", Icon = "📌", Color = "#f59e0b" };
        public KanbanColumn InProgress { get; set; } = new() { Title = "Kanban.ColInProgress", Icon = "🔄", Color = "#06b6d4" };
        public KanbanColumn Completed { get; set; } = new() { Title = "Kanban.ColCompleted", Icon = "✅", Color = "#10b981" };
        public KanbanColumn History { get; set; } = new() { Title = "Kanban.ColHistory", Icon = "📜", Color = "#94a3b8" };

        public string ScopeLabel { get; set; } = string.Empty;
        public int TotalTasks { get; set; }
    }


    public class TaskCreateViewModel
    {
        [Required(ErrorMessage = "Validation.TitleRequired")]
        [Display(Name = "Field.TaskTitle")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Field.TaskDescription")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Validation.AssigneeRequired")]
        [Display(Name = "Field.Assignee")]
        public Guid AssigneeId { get; set; }
    }

    public class TaskEditViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Validation.TitleRequired")]
        [Display(Name = "Field.TaskTitle")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Field.TaskDescription")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Field.Assignee")]
        public Guid AssigneeId { get; set; }
    }

    public class TaskProposeViewModel
    {
        [Required(ErrorMessage = "Validation.ProposeTitleRequired")]
        [Display(Name = "Field.ProposeTitle")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Validation.ProposeDescRequired")]
        [Display(Name = "Field.ProposeDescription")]
        public string Description { get; set; } = string.Empty;
    }
}