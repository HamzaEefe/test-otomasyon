using TestOtomasyon.Entities;

namespace TestOtomasyon.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByUserNameAsync(string userName);
        Task<IEnumerable<User>> GetAllAsync();
        Task<Guid> CreateAsync(User user);
        Task<bool> UpdateAsync(User user);
        Task<bool> SoftDeleteAsync(Guid id);
        Task<IEnumerable<string>> GetAuthoritiesAsync(Guid userId);
        Task<IEnumerable<Role>> GetRolesAsync(Guid userId);
        Task AssignRolesAsync(Guid userId, IEnumerable<Guid> roleIds);
        Task<bool> ValidatePasswordAsync(string userName, string password);
        Task<bool> ResetPasswordAsync(Guid userId, string newPassword);
        Task<bool> UserNameExistsAsync(string userName, Guid? excludeUserId = null);

        
        Task<IEnumerable<User>> GetSubordinatesAsync(Guid parentId);
        Task<IEnumerable<User>> GetAllSubordinatesRecursiveAsync(Guid parentId);
        Task<IEnumerable<User>> GetTopLevelUsersAsync();
        Task<int> CountAllAsync();
    }
}