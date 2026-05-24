using Dapper;
using TestOtomasyon.Entities;
using TestOtomasyon.Helpers;
using TestOtomasyon.Repositories.Interfaces;

namespace TestOtomasyon.Repositories
{
    public class AuthorityRepository : IAuthorityRepository
    {
        private readonly IDbConnectionFactory _dbFactory;

        public AuthorityRepository(IDbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<IEnumerable<Authority>> GetAllAsync()
        {
            using var connection = _dbFactory.CreateConnection();
            return await connection.QueryAsync<Authority>(
                "SELECT * FROM Authority WHERE status = 1 ORDER BY name");
        }

        public async Task<Authority?> GetByIdAsync(Guid id)
        {
            using var connection = _dbFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<Authority>(
                "SELECT * FROM Authority WHERE id = @Id AND status = 1",
                new { Id = id });
        }
    }
}