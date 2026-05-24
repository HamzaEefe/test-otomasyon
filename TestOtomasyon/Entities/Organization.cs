namespace TestOtomasyon.Entities
{
    public class Organization
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public int Status { get; set; }
    }
}
