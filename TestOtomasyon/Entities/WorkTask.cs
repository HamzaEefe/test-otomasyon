namespace TestOtomasyon.Entities;

public class WorkTask
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid AssignerId { get; set; }
    public Guid AssigneeId { get; set; }
    public int TaskStatus { get; set; }
    public int Status { get; set; }
    public Guid ApprovedById { get; set; }
    public DateTime? ApprovedOn { get; set; }
    public string? RejectReason { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? CompletedOn { get; set; }
    public string? AssignerName { get; set; }
    public string? AssigneeName { get; set; }
    public string? ApprovedByName { get; set; }
    public string? AssigneeUserName { get; set; }
    public string? AssigneeRole { get; set; }
    public string? AssigneeDepartment { get; set; }
    public string? AssigneePhone { get; set; }
    public string? AssigneeEmail { get; set; }
}
