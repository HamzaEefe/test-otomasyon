using Dapper;
using Microsoft.Data.SqlClient;
using TestOtomasyon.Entities;
using TestOtomasyon.Helpers;
using TestOtomasyon.Repositories.Interfaces;

namespace TestOtomasyon.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ILogger<MessageRepository> _logger;

        public MessageRepository(IDbConnectionFactory connectionFactory, ILogger<MessageRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<Guid> SendMessageAsync(Message message)
        {
            try
            {
                message.Id = Guid.NewGuid();
                message.SentAt = DateTime.Now;
                if (message.Status == 0) message.Status = 1;

                var sql = @"
                    INSERT INTO [Message] (id, senderId, recipientId, subject, body, sentAt, isRead, status)
                    VALUES (@Id, @SenderId, @RecipientId, @Subject, @Body, @SentAt, @IsRead, @Status)";

                using var connection = _connectionFactory.CreateConnection();
                await connection.ExecuteAsync(sql, message);

                return message.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendMessageAsync hatası: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<Message?> GetByIdAsync(Guid messageId)
        {
            try
            {
                var sql = @"
                    SELECT m.*,
                           us.name AS SenderName,
                           ur.name AS RecipientName
                    FROM [Message] m
                    INNER JOIN [User] us ON m.senderId = us.id
                    INNER JOIN [User] ur ON m.recipientId = ur.id
                    WHERE m.id = @MessageId AND m.status = 1";

                using var connection = _connectionFactory.CreateConnection();
                return await connection.QuerySingleOrDefaultAsync<Message>(sql, new { MessageId = messageId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetByIdAsync hatası");
                throw;
            }
        }

        public async Task<IEnumerable<Message>> GetInboxAsync(Guid userId)
        {
            try
            {
                var sql = @"
                    SELECT m.*, u.name AS SenderName
                    FROM [Message] m
                    INNER JOIN [User] u ON m.senderId = u.id
                    WHERE m.recipientId = @UserId AND m.status = 1
                    ORDER BY m.sentAt DESC";

                using var connection = _connectionFactory.CreateConnection();
                return await connection.QueryAsync<Message>(sql, new { UserId = userId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetInboxAsync hatası");
                throw;
            }
        }

        public async Task<IEnumerable<Message>> GetSentAsync(Guid userId)
        {
            try
            {
                var sql = @"
                    SELECT m.*, u.name AS RecipientName
                    FROM [Message] m
                    INNER JOIN [User] u ON m.recipientId = u.id
                    WHERE m.senderId = @UserId AND m.status = 1
                    ORDER BY m.sentAt DESC";

                using var connection = _connectionFactory.CreateConnection();
                return await connection.QueryAsync<Message>(sql, new { UserId = userId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetSentAsync hatası");
                throw;
            }
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            try
            {
                var sql = "SELECT COUNT(*) FROM [Message] WHERE recipientId = @UserId AND isRead = 0 AND status = 1";

                using var connection = _connectionFactory.CreateConnection();
                return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUnreadCountAsync hatası");
                throw;
            }
        }

        public async Task<int> GetSentCountAsync(Guid userId)
        {
            try
            {
                var sql = "SELECT COUNT(*) FROM [Message] WHERE senderId = @UserId AND status = 1";

                using var connection = _connectionFactory.CreateConnection();
                return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetSentCountAsync hatası");
                throw;
            }
        }

        public async Task MarkAsReadAsync(Guid messageId)
        {
            try
            {
                var sql = "UPDATE [Message] SET isRead = 1 WHERE id = @MessageId";

                using var connection = _connectionFactory.CreateConnection();
                await connection.ExecuteAsync(sql, new { MessageId = messageId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MarkAsReadAsync hatası");
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid messageId, Guid userId)
        {
            try
            {
                // Soft delete: status = 0
                var sql = @"
                    UPDATE [Message]
                    SET status = 0
                    WHERE id = @MessageId
                    AND (senderId = @UserId OR recipientId = @UserId)";

                using var connection = _connectionFactory.CreateConnection();
                var rows = await connection.ExecuteAsync(sql, new { MessageId = messageId, UserId = userId });
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteAsync hatası");
                throw;
            }
        }
    }
}
