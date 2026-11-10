namespace AutoPro.Domain.Entities;

public class StaffUser
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Enums.UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
}
