using TestOtomasyon.Entities;

namespace TestOtomasyon.Repositories.Interfaces
{
    public interface IAuthorityRepository
    {
        Task<IEnumerable<Authority>> GetAllAsync();
        Task<Authority?> GetByIdAsync(Guid id);
    }
}