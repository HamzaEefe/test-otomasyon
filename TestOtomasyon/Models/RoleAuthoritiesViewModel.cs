namespace TestOtomasyon.Models
{
    public class RoleAuthoritiesViewModel
    {
        public Guid RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public List<AuthorityCheckItem> Authorities { get; set; } = new();
    }

    public class AuthorityCheckItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsAssigned { get; set; }
    }
}