using Microsoft.EntityFrameworkCore;
using PhishGuard.Data;
using PhishGuard.Models;

namespace PhishGuard.Services;

public class AuthService
{
    private readonly PhishGuardContext _db;

    public AuthService(PhishGuardContext db)
    {
        _db = db;
    }

    public async Task<Employee?> RegisterAsync(string email, string displayName, string password, string? department = null)
    {
        if (await _db.Employees.AnyAsync(e => e.Email == email))
            return null;

        var employee = new Employee
        {
            Email = email,
            DisplayName = displayName,
            Department = department,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = EmployeeRole.Employee
        };

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();

        // Create training record for gamification
        _db.EmployeeTrainings.Add(new EmployeeTraining { EmployeeId = employee.EmployeeId });
        await _db.SaveChangesAsync();

        return employee;
    }

    public async Task<Employee?> LoginAsync(string email, string password)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Email == email && e.IsActive);
        if (employee == null) return null;

        if (string.IsNullOrEmpty(employee.PasswordHash) ||
            !BCrypt.Net.BCrypt.Verify(password, employee.PasswordHash))
            return null;

        employee.LastLogin = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return employee;
    }

    public async Task<Employee?> GetByIdAsync(int id)
    {
        return await _db.Employees.FindAsync(id);
    }

    public async Task SeedAdminAsync()
    {
        if (!await _db.Employees.AnyAsync(e => e.Role == EmployeeRole.Admin))
        {
            var admin = new Employee
            {
                Email = "admin@phishguard.local",
                DisplayName = "Admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = EmployeeRole.Admin
            };
            _db.Employees.Add(admin);
            await _db.SaveChangesAsync();

            _db.EmployeeTrainings.Add(new EmployeeTraining { EmployeeId = admin.EmployeeId });
            await _db.SaveChangesAsync();
        }
    }
}
