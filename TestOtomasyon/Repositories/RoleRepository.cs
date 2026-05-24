using Dapper;
using TestOtomasyon.Entities;
using TestOtomasyon.Helpers;
using TestOtomasyon.Repositories.Interfaces;

namespace TestOtomasyon.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly IDbConnectionFactory _dbFactory;

        public RoleRepository(IDbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<Role?> GetByIdAsync(Guid id)
        {
            using var connection = _dbFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<Role>(
                "SELECT * FROM [Role] WHERE id = @Id AND status = 1",
                new { Id = id });
        }

        public async Task<IEnumerable<Role>> GetAllAsync()
        {
            using var connection = _dbFactory.CreateConnection();
            return await connection.QueryAsync<Role>(
                "SELECT * FROM [Role] WHERE status = 1 ORDER BY name");
        }

        public async Task<Guid> CreateAsync(Role role)
        {
            using var connection = _dbFactory.CreateConnection();
            role.Id = Guid.NewGuid();
            role.CreatedOn = DateTime.Now;
            role.Status = 1;

            const string sql = @"
                INSERT INTO [Role] (id, organizationId, name, createdOn, status)
                VALUES (@Id, @OrganizationId, @Name, @CreatedOn, @Status)";
            await connection.ExecuteAsync(sql, role);
            return role.Id;
        }

        public async Task<bool> UpdateAsync(Role role)
        {
            using var connection = _dbFactory.CreateConnection();
            const string sql = "UPDATE [Role] SET name = @Name WHERE id = @Id";
            var rows = await connection.ExecuteAsync(sql, role);
            return rows > 0;
        }

        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            using var connection = _dbFactory.CreateConnection();
            var rows = await connection.ExecuteAsync(
                "UPDATE [Role] SET status = 0 WHERE id = @Id", new { Id = id });
            return rows > 0;
        }

        public async Task<IEnumerable<Authority>> GetAuthoritiesAsync(Guid roleId)
        {
            using var connection = _dbFactory.CreateConnection();
            const string sql = @"
                SELECT a.*
                FROM Authority a
                INNER JOIN RoleAuthority ra ON a.id = ra.authorityId AND ra.status = 1
                WHERE ra.roleId = @RoleId AND a.status = 1
                ORDER BY a.name";
            return await connection.QueryAsync<Authority>(sql, new { RoleId = roleId });
        }

        public async Task AssignAuthoritiesAsync(Guid roleId, IEnumerable<Guid> authorityIds)
        {
            using var connection = _dbFactory.CreateConnection();
            // Önce bu role ait tüm yetkileri pasifle
            await connection.ExecuteAsync(
                "UPDATE RoleAuthority SET status = 0 WHERE roleId = @RoleId",
                new { RoleId = roleId });

            // Sonra seçilen yetkileri yeniden ekle
            foreach (var authorityId in authorityIds)
            {
                await connection.ExecuteAsync(@"
                    INSERT INTO RoleAuthority (id, roleId, authorityId, createdOn, status)
                    VALUES (@Id, @RoleId, @AuthorityId, @CreatedOn, 1)",
                    new
                    {
                        Id = Guid.NewGuid(),
                        RoleId = roleId,
                        AuthorityId = authorityId,
                        CreatedOn = DateTime.Now
                    });
            }
        }
    }
}