using TestOtomasyon.Entities;

namespace TestOtomasyon.Repositories.Interfaces
{
    public interface IMessageRepository
    {
        Task<Guid> SendMessageAsync(Message message);
        Task<Message?> GetByIdAsync(Guid messageId);
        Task<IEnumerable<Message>> GetInboxAsync(Guid userId);
        Task<IEnumerable<Message>> GetSentAsync(Guid userId);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task<int> GetSentCountAsync(Guid userId);
        Task MarkAsReadAsync(Guid messageId);
        Task<bool> DeleteAsync(Guid messageId, Guid userId);
    }
}