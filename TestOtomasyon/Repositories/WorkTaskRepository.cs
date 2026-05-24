using Dapper;
using TestOtomasyon.Entities;
using TestOtomasyon.Helpers;
using TestOtomasyon.Repositories.Interfaces;

namespace TestOtomasyon.Repositories
{
    public class WorkTaskRepository : IWorkTaskRepository
    {
        private readonly IDbConnectionFactory _dbFactory;

        public WorkTaskRepository(IDbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        
        private const string BaseSelect = @"
            SELECT t.*,
                   ua.name AS AssignerName,
                   ub.name AS AssigneeName
            FROM WorkTask t
            INNER JOIN [User] ua ON t.assignerId = ua.id
            INNER JOIN [User] ub ON t.assigneeId = ub.id";

        public async Task<WorkTask?> GetByIdAsync(Guid id)
        {
            using var connection = _dbFactory.CreateConnection();
            var sql = BaseSelect + " WHERE t.id = @Id AND t.status = 1";
            return await connection.QuerySingleOrDefaultAsync<WorkTask>(sql, new { Id = id });
        }

        public async Task<IEnumerable<WorkTask>> GetAllAsync()
        {
            using var connection = _dbFactory.CreateConnection();
            var sql = BaseSelect + " WHERE t.status = 1 ORDER BY t.createdOn DESC";
            return await connection.QueryAsync<WorkTask>(sql);
        }

        public async Task<IEnumerable<WorkTask>> GetByAssigneeAsync(Guid assigneeId)
        {
            using var connection = _dbFactory.CreateConnection();
            var sql = BaseSelect + " WHERE t.assigneeId = @AssigneeId AND t.status = 1 ORDER BY t.createdOn DESC";
            return await connection.QueryAsync<WorkTask>(sql, new { AssigneeId = assigneeId });
        }

        public async Task<IEnumerable<WorkTask>> GetByAssignerAsync(Guid assignerId)
        {
            using var connection = _dbFactory.CreateConnection();
            var sql = BaseSelect + " WHERE t.assignerId = @AssignerId AND t.status = 1 ORDER BY t.createdOn DESC";
            return await connection.QueryAsync<WorkTask>(sql, new { AssignerId = assignerId });
        }

        public async Task<Guid> CreateAsync(WorkTask task)
        {
            using var connection = _dbFactory.CreateConnection();
            task.Id = Guid.NewGuid();
            task.CreatedOn = DateTime.Now;
            task.Status = 1;
            task.TaskStatus = 0;

            const string sql = @"
                INSERT INTO WorkTask (id, organizationId, title, description, assignerId, assigneeId, taskStatus, createdOn, status)
                VALUES (@Id, @OrganizationId, @Title, @Description, @AssignerId, @AssigneeId, @TaskStatus, @CreatedOn, @Status)";
            await connection.ExecuteAsync(sql, task);
            return task.Id;
        }

        public async Task<bool> UpdateAsync(WorkTask task)
        {
            using var connection = _dbFactory.CreateConnection();
            const string sql = @"
                UPDATE WorkTask
                SET title = @Title,
                    description = @Description,
                    assigneeId = @AssigneeId
                WHERE id = @Id";
            var rows = await connection.ExecuteAsync(sql, task);
            return rows > 0;
        }

        public async Task<bool> UpdateStatusAsync(Guid id, int newStatus)
        {
            using var connection = _dbFactory.CreateConnection();

            string sql;
            if (newStatus == TaskStatusHelper.Tamamlandi)
            {
               
                sql = "UPDATE WorkTask SET taskStatus = @TaskStatus, completedOn = @CompletedOn WHERE id = @Id";
                var rows = await connection.ExecuteAsync(sql, new
                {
                    Id = id,
                    TaskStatus = newStatus,
                    CompletedOn = DateTime.Now
                });
                return rows > 0;
            }
            else
            {
                sql = "UPDATE WorkTask SET taskStatus = @TaskStatus WHERE id = @Id";
                var rows = await connection.ExecuteAsync(sql, new { Id = id, TaskStatus = newStatus });
                return rows > 0;
            }
        }
        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            using var connection = _dbFactory.CreateConnection();
            var rows = await connection.ExecuteAsync(
                "UPDATE WorkTask SET status = 0 WHERE id = @Id", new { Id = id });
            return rows > 0;
        }
        public async Task<int> CountByAssigneeAndStatusAsync(Guid assigneeId, int taskStatus)
        {
            using var connection = _dbFactory.CreateConnection();
            const string sql = @"
        SELECT COUNT(*) FROM WorkTask 
        WHERE assigneeId = @AssigneeId 
          AND taskStatus = @TaskStatus 
          AND status = 1";
            return await connection.ExecuteScalarAsync<int>(sql, new { AssigneeId = assigneeId, TaskStatus = taskStatus });
        }

        public async Task<int> CountByAssignerAsync(Guid assignerId)
        {
            using var connection = _dbFactory.CreateConnection();
            const string sql = @"
        SELECT COUNT(*) FROM WorkTask 
        WHERE assignerId = @AssignerId 
          AND status = 1";
            return await connection.ExecuteScalarAsync<int>(sql, new { AssignerId = assignerId });
        }

        public async Task<int> CountAllAsync()
        {
            using var connection = _dbFactory.CreateConnection();
            const string sql = "SELECT COUNT(*) FROM WorkTask WHERE status = 1";
            return await connection.ExecuteScalarAsync<int>(sql);
        }

        public async Task<Dictionary<int, int>> GetTaskStatusBreakdownAsync(Guid? assigneeId = null)
        {
            using var connection = _dbFactory.CreateConnection();
            string sql;
            object? param = null;

            if (assigneeId.HasValue)
            {
                sql = @"
            SELECT taskStatus, COUNT(*) as Cnt 
            FROM WorkTask 
            WHERE assigneeId = @AssigneeId AND status = 1
            GROUP BY taskStatus";
                param = new { AssigneeId = assigneeId.Value };
            }
            else
            {
                sql = @"
            SELECT taskStatus, COUNT(*) as Cnt 
            FROM WorkTask 
            WHERE status = 1
            GROUP BY taskStatus";
            }

            var rows = await connection.QueryAsync<(int taskStatus, int Cnt)>(sql, param);
            return rows.ToDictionary(r => r.taskStatus, r => r.Cnt);
        }
        public async Task<Guid> ProposeAsync(WorkTask task)
        {
            
            using var connection = _dbFactory.CreateConnection();
            task.Id = Guid.NewGuid();
            task.CreatedOn = DateTime.Now;
            task.Status = 1;
            task.TaskStatus = TaskStatusHelper.OnayBekliyor;

            const string sql = @"
        INSERT INTO WorkTask 
            (id, organizationId, title, description, assignerId, assigneeId, taskStatus, createdOn, status)
        VALUES 
            (@Id, @OrganizationId, @Title, @Description, @AssignerId, @AssigneeId, @TaskStatus, @CreatedOn, @Status)";
            await connection.ExecuteAsync(sql, task);
            return task.Id;
        }

        public async Task<bool> ApproveAsync(Guid taskId, Guid approverId)
        {
            using var connection = _dbFactory.CreateConnection();
            const string sql = @"
        UPDATE WorkTask 
        SET taskStatus = @TaskStatus,
            approvedById = @ApproverId,
            approvedOn = @Now
        WHERE id = @Id";
            var rows = await connection.ExecuteAsync(sql, new
            {
                Id = taskId,
                TaskStatus = TaskStatusHelper.Atandi,
                ApproverId = approverId,
                Now = DateTime.Now
            });
            return rows > 0;
        }

        public async Task<bool> RejectAsync(Guid taskId, Guid approverId, string reason)
        {
            using var connection = _dbFactory.CreateConnection();
            const string sql = @"
        UPDATE WorkTask 
        SET taskStatus = @TaskStatus,
            approvedById = @ApproverId,
            approvedOn = @Now,
            rejectReason = @Reason
        WHERE id = @Id";
            var rows = await connection.ExecuteAsync(sql, new
            {
                Id = taskId,
                TaskStatus = TaskStatusHelper.Reddedildi,
                ApproverId = approverId,
                Now = DateTime.Now,
                Reason = reason
            });
            return rows > 0;
        }

        public async Task<IEnumerable<WorkTask>> GetForKanbanAsync(Guid? scopeUserId = null, bool includeAll = false)
        {
            using var connection = _dbFactory.CreateConnection();

            string whereClause;
            object? param = null;

            if (includeAll)
            {
                whereClause = "WHERE t.status = 1";
            }
            else if (scopeUserId.HasValue)
            {
                whereClause = @"
            WHERE t.status = 1
              AND (
                t.assignerId = @UserId 
                OR t.assigneeId = @UserId
                OR t.assigneeId IN (
                    WITH Subs AS (
                        SELECT id FROM [User] WHERE parentId = @UserId AND status = 1
                        UNION ALL
                        SELECT u.id FROM [User] u
                        INNER JOIN Subs s ON u.parentId = s.id
                        WHERE u.status = 1
                    )
                    SELECT id FROM Subs
                )
                OR t.assignerId IN (
                    WITH Subs2 AS (
                        SELECT id FROM [User] WHERE parentId = @UserId AND status = 1
                        UNION ALL
                        SELECT u.id FROM [User] u
                        INNER JOIN Subs2 s ON u.parentId = s.id
                        WHERE u.status = 1
                    )
                    SELECT id FROM Subs2
                )
              )";
                param = new { UserId = scopeUserId.Value };
            }
            else
            {
                whereClause = "WHERE t.status = 1";
            }

            var sql = @"
        SELECT t.*,
               ua.name AS AssignerName,
               ub.name AS AssigneeName,
               ub.userName AS AssigneeUserName,
               ub.mobilePhone AS AssigneePhone,
               ub.email AS AssigneeEmail,
               d.name AS AssigneeDepartment,
               uc.name AS ApprovedByName,
               ISNULL(STUFF((
                   SELECT ', ' + r.name
                   FROM UserRole ur
                   INNER JOIN [Role] r ON ur.roleId = r.id AND r.status = 1
                   WHERE ur.userId = ub.id AND ur.status = 1
                   FOR XML PATH('')
               ), 1, 2, ''), '') AS AssigneeRole
        FROM WorkTask t
        INNER JOIN [User] ua ON t.assignerId = ua.id
        INNER JOIN [User] ub ON t.assigneeId = ub.id
        LEFT JOIN Department d ON ub.departmentId = d.id
        LEFT JOIN [User] uc ON t.approvedById = uc.id
        " + whereClause + @"
        ORDER BY t.createdOn DESC";

            return await connection.QueryAsync<WorkTask>(sql, param);
        }

        public async Task<IEnumerable<WorkTask>> GetPendingApprovalsAsync(Guid approverId)
        {
            using var connection = _dbFactory.CreateConnection();
            const string sql = @"
        SELECT t.*,
               ua.name AS AssignerName,
               ub.name AS AssigneeName,
               ub.userName AS AssigneeUserName,
               ub.mobilePhone AS AssigneePhone,
               ub.email AS AssigneeEmail,
               d.name AS AssigneeDepartment
        FROM WorkTask t
        INNER JOIN [User] ua ON t.assignerId = ua.id
        INNER JOIN [User] ub ON t.assigneeId = ub.id
        LEFT JOIN Department d ON ub.departmentId = d.id
        WHERE t.taskStatus = 4 
          AND t.status = 1
          AND ua.parentId = @ApproverId
        ORDER BY t.createdOn DESC";
            return await connection.QueryAsync<WorkTask>(sql, new { ApproverId = approverId });
        }
    }
}