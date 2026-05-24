using Dapper;
using TestOtomasyon.Entities;
using TestOtomasyon.Helpers;
using TestOtomasyon.Repositories.Interfaces;

namespace TestOtomasyon.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnectionFactory _dbFactory;

        public UserRepository(IDbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        // Ortak SELECT (tek bir kullanıcı)
        private const string SelectUserBase = @"
            SELECT u.*, 
                   d.name AS DepartmentName,
                   p.name AS ParentName
            FROM [User] u
            LEFT JOIN Department d ON u.departmentId = d.id
            LEFT JOIN [User] p ON u.parentId = p.id";

        public async Task<User?> GetByIdAsync(Guid id)
        {
            using var connection = _dbFactory.CreateConnection();
            var sql = SelectUserBase + " WHERE u.id = @Id AND u.status = 1";
            return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Id = id });
        }

        public async Task<User?> GetByUserNameAsync(string userName)
        {
            using var connection = _dbFactory.CreateConnection();
            var sql = SelectUserBase + " WHERE u.userName = @UserName AND u.status = 1";
            return await connection.QuerySingleOrDefaultAsync<User>(sql, new { UserName = userName });
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            using var connection = _dbFactory.CreateConnection();
            var sql = SelectUserBase + " WHERE u.status = 1 ORDER BY u.name";
            return await connection.QueryAsync<User>(sql);
        }

        public async Task<Guid> CreateAsync(User user)
        {
            using var connection = _dbFactory.CreateConnection();
            user.Id = Guid.NewGuid();
            user.CreatedOn = DateTime.Now;
            user.Status = 1;
            user.Name = $"{user.FirstName} {user.LastName}";
            if (user.UserType == 0) user.UserType = 1;

            const string sql = @"
        INSERT INTO [User] 
            (id, organizationId, departmentId, firstName, lastName, name, userName, password, email,
             mobilePhone, accountingCode, userType, parentId, createdOn, status)
        VALUES 
            (@Id, @OrganizationId, @DepartmentId, @FirstName, @LastName, @Name, @UserName, @Password, @Email,
             @MobilePhone, @AccountingCode, @UserType, @ParentId, @CreatedOn, @Status)";
            await connection.ExecuteAsync(sql, user);
            return user.Id;
        }

        public async Task<bool> UpdateAsync(User user)
        {
            using var connection = _dbFactory.CreateConnection();
            user.Name = $"{user.FirstName} {user.LastName}";

            const string sql = @"
        UPDATE [User]
        SET firstName = @FirstName,
            lastName = @LastName,
            name = @Name,
            departmentId = @DepartmentId,
            mobilePhone = @MobilePhone,
            email = @Email,
            accountingCode = @AccountingCode,
            parentId = @ParentId
        WHERE id = @Id";
            var rows = await connection.ExecuteAsync(sql, user);
            return rows > 0;
        }

        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            using var connection = _dbFactory.CreateConnection();
            const string sql = @"
                UPDATE [User] 
                SET status = 0,
                    userName = userName + '_deleted_' + CONVERT(NVARCHAR(20), GETDATE(), 112) + '_' + LEFT(CAST(NEWID() AS NVARCHAR(36)), 8)
                WHERE id = @Id";
            var rows = await connection.ExecuteAsync(sql, new { Id = id });
            return rows > 0;
        }

        public async Task<IEnumerable<string>> GetAuthoritiesAsync(Guid userId)
        {
            using var connection = _dbFactory.CreateConnection();
            const string sql = @"
                SELECT DISTINCT a.name
                FROM [User] u
                INNER JOIN UserRole ur ON u.id = ur.userId AND ur.status = 1
                INNER JOIN [Role] r ON ur.roleId = r.id AND r.status = 1
                INNER JOIN RoleAuthority ra ON r.id = ra.roleId AND ra.status = 1
                INNER JOIN Authority a ON ra.authorityId = a.id AND a.status = 1
                WHERE u.id = @UserId AND u.status = 1";
            return await connection.QueryAsync<string>(sql, new { UserId = userId });
        }

        public async Task<IEnumerable<Role>> GetRolesAsync(Guid userId)
        {
            using var connection = _dbFactory.CreateConnection();
            const string sql = @"
                SELECT r.*
                FROM [Role] r
                INNER JOIN UserRole ur ON r.id = ur.roleId AND ur.status = 1
                WHERE ur.userId = @UserId AND r.status = 1";
            return await connection.QueryAsync<Role>(sql, new { UserId = userId });
        }

        public async Task AssignRolesAsync(Guid userId, IEnumerable<Guid> roleIds)
        {
            using var connection = _dbFactory.CreateConnection();
            await connection.ExecuteAsync(
                "UPDATE UserRole SET status = 0 WHERE userId = @UserId",
                new { UserId = userId });

            foreach (var roleId in roleIds)
            {
                await connection.ExecuteAsync(@"
                    INSERT INTO UserRole (id, userId, roleId, createdOn, status)
                    VALUES (@Id, @UserId, @RoleId, @CreatedOn, 1)",
                    new
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        RoleId = roleId,
                        CreatedOn = DateTime.Now
                    });
            }
        }

        public async Task<bool> ValidatePasswordAsync(string userName, string password)
        {
            var user = await GetByUserNameAsync(userName);
            if (user == null) return false;
            return BCrypt.Net.BCrypt.Verify(password, user.Password);
        }

        public async Task<bool> ResetPasswordAsync(Guid userId, string newPassword)
        {
            using var connection = _dbFactory.CreateConnection();
            var hash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            const string sql = "UPDATE [User] SET password = @Password WHERE id = @Id";
            var rows = await connection.ExecuteAsync(sql, new { Id = userId, Password = hash });
            return rows > 0;
        }

        public async Task<bool> UserNameExistsAsync(string userName, Guid? excludeUserId = null)
        {
            using var connection = _dbFactory.CreateConnection();
            string sql;
            object parameters;

           
            if (excludeUserId.HasValue)
            {
                sql = "SELECT COUNT(*) FROM [User] WHERE userName = @UserName AND id != @ExcludeId";
                parameters = new { UserName = userName, ExcludeId = excludeUserId.Value };
            }
            else
            {
                sql = "SELECT COUNT(*) FROM [User] WHERE userName = @UserName";
                parameters = new { UserName = userName };
            }

            var count = await connection.ExecuteScalarAsync<int>(sql, parameters);
            return count > 0;
        }

        // ========== HİYERARŞİ METOTLARI ==========

        public async Task<IEnumerable<User>> GetSubordinatesAsync(Guid parentId)
        {
            using var connection = _dbFactory.CreateConnection();
            var sql = SelectUserBase + " WHERE u.parentId = @ParentId AND u.status = 1 ORDER BY u.name";
            return await connection.QueryAsync<User>(sql, new { ParentId = parentId });
        }

        public async Task<IEnumerable<User>> GetAllSubordinatesRecursiveAsync(Guid parentId)
        {
            using var connection = _dbFactory.CreateConnection();
           
            const string sql = @"
        WITH SubordinatesCTE AS (
            -- İlk seviye: parentId'si verilen ID olanlar
            SELECT id, organizationId, departmentId, firstName, lastName, name, userName, password,
                   mobilePhone, accountingCode, userType, parentId, createdOn, status, 1 AS Level
            FROM [User]
            WHERE parentId = @ParentId AND status = 1

            UNION ALL

            -- Recursive: önceki seviyenin altındakiler
            SELECT u.id, u.organizationId, u.departmentId, u.firstName, u.lastName, u.name, u.userName, u.password,
                   u.mobilePhone, u.accountingCode, u.userType, u.parentId, u.createdOn, u.status, sc.Level + 1
            FROM [User] u
            INNER JOIN SubordinatesCTE sc ON u.parentId = sc.id
            WHERE u.status = 1
        )
        SELECT sc.*, 
               d.name AS DepartmentName,
               p.name AS ParentName
        FROM SubordinatesCTE sc
        LEFT JOIN Department d ON sc.departmentId = d.id
        LEFT JOIN [User] p ON sc.parentId = p.id
        ORDER BY sc.Level, sc.name";
            return await connection.QueryAsync<User>(sql, new { ParentId = parentId });
        }

        public async Task<IEnumerable<User>> GetTopLevelUsersAsync()
        {
            using var connection = _dbFactory.CreateConnection();
            var sql = SelectUserBase + " WHERE u.parentId IS NULL AND u.status = 1 ORDER BY u.name";
            return await connection.QueryAsync<User>(sql);
        }
        public async Task<int> CountAllAsync()
        {
            using var connection = _dbFactory.CreateConnection();
            const string sql = "SELECT COUNT(*) FROM [User] WHERE status = 1";
            return await connection.ExecuteScalarAsync<int>(sql);
        }
    }
}