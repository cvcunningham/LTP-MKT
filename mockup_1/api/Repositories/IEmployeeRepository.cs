using api.Models;

namespace api.Repositories;

// The employee list, edited on the Intern Directory page.
public interface IEmployeeRepository
{
    Task<List<Employee>> GetAllAsync(); // sorted by campus, then name
    Task AddAsync(Employee employee);   // employee.Id is chosen on the page (the controller checks it's free)

    // Saves the employee that had oldId (employee.Id may be a new id). Their password is kept.
    // False if oldId doesn't exist.
    Task<bool> UpdateAsync(int oldId, Employee employee);

    // Saves a new password hash ("" = back to the starting password). False if id doesn't exist.
    Task<bool> SetPasswordHashAsync(int id, string passwordHash);

    // Permanently removes the employee. False if it doesn't exist.
    Task<bool> DeleteAsync(int id);
}
