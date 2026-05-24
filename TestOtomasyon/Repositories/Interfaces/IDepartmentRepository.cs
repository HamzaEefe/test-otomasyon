using TestOtomasyon.Entities;

namespace TestOtomasyon.Repositories.Interfaces
{
    public interface IDepartmentRepository
    {
        Task<Department?> GetByIdAsync(Guid id);
        Task<IEnumerable<Department>> GetAllAsync();
        Task<Guid> CreateAsync(Department department);
        Task<bool> UpdateAsync(Department department);
        Task<bool> SoftDeleteAsync(Guid id);
        Task<int> CountAllAsync();
    }
}