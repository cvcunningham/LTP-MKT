using api.Models;

namespace api.Repositories;

// The employee list. Read only: employees.csv is edited by hand for now.
public interface IEmployeeRepository
{
    Task<List<Employee>> GetAllAsync(); // sorted by campus, then name
}
