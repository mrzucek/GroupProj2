using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PhishGuard.Data;
using PhishGuard.Models;

namespace PhishGuard.Middleware;

public class ClerkUserMiddleware
{
    private readonly RequestDelegate _next;

    public ClerkUserMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var clerkUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirstValue("sub");

            if (!string.IsNullOrEmpty(clerkUserId) && !int.TryParse(clerkUserId, out _))
            {
                using var scope = context.RequestServices.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PhishGuardContext>();

                var employee = await db.Employees.FirstOrDefaultAsync(e => e.ClerkUserId == clerkUserId);

                if (employee == null)
                {
                    var email = context.User.FindFirstValue(ClaimTypes.Email)
                        ?? context.User.FindFirstValue("email")
                        ?? $"{clerkUserId}@clerk.user";
                    var name = context.User.FindFirstValue(ClaimTypes.Name)
                        ?? context.User.FindFirstValue("name")
                        ?? email.Split('@')[0];

                    employee = await db.Employees.FirstOrDefaultAsync(e => e.Email == email);
                    if (employee != null)
                    {
                        employee.ClerkUserId = clerkUserId;
                        employee.LastLogin = DateTime.UtcNow;
                    }
                    else
                    {
                        employee = new Employee
                        {
                            Email = email, DisplayName = name, ClerkUserId = clerkUserId,
                            Role = EmployeeRole.Employee, LastLogin = DateTime.UtcNow
                        };
                        db.Employees.Add(employee);
                        db.EmployeeTrainings.Add(new EmployeeTraining { Employee = employee });
                    }
                    await db.SaveChangesAsync();
                }
                else
                {
                    employee.LastLogin = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, employee.EmployeeId.ToString()),
                    new(ClaimTypes.Email, employee.Email),
                    new(ClaimTypes.Name, employee.DisplayName),
                    new(ClaimTypes.Role, employee.Role.ToString())
                };
                context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Clerk"));
            }
        }
        await _next(context);
    }
}
