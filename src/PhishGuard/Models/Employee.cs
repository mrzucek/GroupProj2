using System.ComponentModel.DataAnnotations;

namespace PhishGuard.Models;

public class Employee
{
    public int EmployeeId { get; set; }

    [Required, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Department { get; set; }

    [Required]
    public EmployeeRole Role { get; set; } = EmployeeRole.Employee;

    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Email> Emails { get; set; } = new List<Email>();
    public EmployeeTraining? Training { get; set; }
}

public enum EmployeeRole
{
    Employee,
    Admin
}
