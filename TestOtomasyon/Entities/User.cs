namespace TestOtomasyon.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid? DepartmentId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? MobilePhone { get; set; }
        public string? AccountingCode { get; set; }
        public int UserType { get; set; } = 1;
        public Guid? ParentId { get; set; }

        public DateTime CreatedOn { get; set; }
        public int Status { get; set; }

        
        public string? DepartmentName { get; set; }
        public string? ParentName { get; set; }
    }
}