using TestOtomasyon.Entities;

namespace TestOtomasyon.Repositories.Interfaces
{
    public interface IRoleRepository
    {
        Task<Role?> GetByIdAsync(Guid id);
        Task<IEnumerable<Role>> GetAllAsync();
        Task<Guid> CreateAsync(Role role);
        Task<bool> UpdateAsync(Role role);
        Task<bool> SoftDeleteAsync(Guid id);
        Task<IEnumerable<Authority>> GetAuthoritiesAsync(Guid roleId);
        Task AssignAuthoritiesAsync(Guid roleId, IEnumerable<Guid> authorityIds);
    }
}