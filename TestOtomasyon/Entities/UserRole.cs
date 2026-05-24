namespace TestOtomasyon.Entities
{
    public class UserRole
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public DateTime CreatedOn { get; set; }
        public int Status { get; set; }
    }
}
