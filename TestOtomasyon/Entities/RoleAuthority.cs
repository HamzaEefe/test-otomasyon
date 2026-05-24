namespace TestOtomasyon.Entities
{
    public class RoleAuthority
    {
        public Guid Id { get; set; }
        public Guid RoleId { get; set; }
        public Guid AuthorityId { get; set; }
        public DateTime CreatedOn { get; set; }
        public int Status { get; set; }
    }
}
