namespace TestOtomasyon.Entities
{
    public class Message
    {
        public Guid Id { get; set; }
        public Guid SenderId { get; set; }
        public Guid RecipientId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public int Status { get; set; } = 1;


        public string? SenderName { get; set; }
        public string? RecipientName { get; set; }
    }
}