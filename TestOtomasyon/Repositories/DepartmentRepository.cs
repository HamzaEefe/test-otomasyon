using Dapper;
using TestOtomasyon.Entities;
using TestOtomasyon.Helpers;
using TestOtomasyon.Repositories.Interfaces;

namespace TestOtomasyon.Repositories
{
    public class DepartmentRepository : IDepartmentRepository
    {
        private readonly IDbConnectionFactory _dbFactory;

        public DepartmentRepository(IDbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<Department?> GetByIdAsync(Guid id)
        {
            using var connection = _dbFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<Department>(
                "SELECT * FROM Department WHERE id = @Id AND status = 1",
                new { Id = id });
        }

        public async Task<IEnumerable<Department>> GetAllAsync()
        {
            using var connection = _dbFactory.CreateConnection();
            return await connection.QueryAsync<Department>(
                "SELECT * FROM Department WHERE status = 1 ORDER BY name");
        }

        public async Task<Guid> CreateAsync(Department department)
        {
            using var connection = _dbFactory.CreateConnection();
            department.Id = Guid.NewGuid();
            department.CreatedOn = DateTime.Now;
            department.Status = 1;

            const string sql = @"
                INSERT INTO Department (id, organizationId, name, createdOn, status)
                VALUES (@Id, @OrganizationId, @Name, @CreatedOn, @Status)";
            await connection.ExecuteAsync(sql, department);
            return department.Id;
        }

        public async Task<bool> UpdateAsync(Department department)
        {
            using var connection = _dbFactory.CreateConnection();
            var rows = await connection.ExecuteAsync(
                "UPDATE Department SET name = @Name WHERE id = @Id", department);
            return rows > 0;
        }

        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            using var connection = _dbFactory.CreateConnection();
            var rows = await connection.ExecuteAsync(
                "UPDATE Department SET status = 0 WHERE id = @Id", new { Id = id });
            return rows > 0;
        }
        public async Task<int> CountAllAsync()
        {
            using var connection = _dbFactory.CreateConnection();
            const string sql = "SELECT COUNT(*) FROM Department WHERE status = 1";
            return await connection.ExecuteScalarAsync<int>(sql);
        }
    }
}