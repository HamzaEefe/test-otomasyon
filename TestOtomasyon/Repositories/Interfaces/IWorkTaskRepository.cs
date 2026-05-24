using TestOtomasyon.Entities;

namespace TestOtomasyon.Repositories.Interfaces
{
    public interface IWorkTaskRepository
    {
        Task<WorkTask?> GetByIdAsync(Guid id);
        Task<IEnumerable<WorkTask>> GetAllAsync();
        Task<IEnumerable<WorkTask>> GetByAssigneeAsync(Guid assigneeId);
        Task<IEnumerable<WorkTask>> GetByAssignerAsync(Guid assignerId);
        Task<Guid> CreateAsync(WorkTask task);
        Task<bool> UpdateAsync(WorkTask task);
        Task<bool> UpdateStatusAsync(Guid id, int newStatus);
        Task<bool> SoftDeleteAsync(Guid id);
        Task<int> CountByAssigneeAndStatusAsync(Guid assigneeId, int taskStatus);
        Task<int> CountByAssignerAsync(Guid assignerId);
        Task<int> CountAllAsync();
        Task<Dictionary<int, int>> GetTaskStatusBreakdownAsync(Guid? assigneeId = null);
        Task<Guid> ProposeAsync(WorkTask task);
        Task<bool> ApproveAsync(Guid taskId, Guid approverId);
        Task<bool> RejectAsync(Guid taskId, Guid approverId, string reason);
        Task<IEnumerable<WorkTask>> GetForKanbanAsync(Guid? scopeUserId = null, bool includeAll = false);
        Task<IEnumerable<WorkTask>> GetPendingApprovalsAsync(Guid approverId);
    }
}