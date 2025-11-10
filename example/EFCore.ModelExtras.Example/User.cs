namespace EFCore.ModelExtras.Example;

public class User
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class EmailAuditLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? OldEmail { get; set; }
    public required string NewEmail { get; set; }
    public DateTime ChangedAt { get; set; }
}
