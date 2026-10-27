using api.Models;

namespace api.Repositories;

public class CsvEmployeeRepository : IEmployeeRepository
{
    // CsvFile.EnsureSchema upgrades older files to these headers (matching columns by name).
    private const string EmployeesHeader = "Id,Name,Email,Phone,Campus,Semester,Role,PasswordHash";

    private readonly string _path;
    private readonly SemaphoreSlim _lock = new(1, 1); // one read/write at a time

    public CsvEmployeeRepository(IWebHostEnvironment env)
    {
        _path = Path.Combine(env.ContentRootPath, "Data", "employees.csv");
        CsvFile.EnsureSchema(_path, EmployeesHeader);
    }

    public async Task<List<Employee>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try { return ReadEmployees().OrderBy(e => e.Campus).ThenBy(e => e.Name).ToList(); }
        finally { _lock.Release(); }
    }

    public async Task AddAsync(Employee employee)
    {
        await _lock.WaitAsync();
        try { CsvFile.AppendRow(_path, ToRow(employee)); }
        finally { _lock.Release(); }
    }

    public async Task<bool> UpdateAsync(int oldId, Employee changes)
    {
        await _lock.WaitAsync();
        try
        {
            var employees = ReadEmployees();
            var index = employees.FindIndex(e => e.Id == oldId);
            if (index < 0)
                return false;
            changes.PasswordHash = employees[index].PasswordHash; // passwords change only through SetPasswordHashAsync
            employees[index] = changes;
            CsvFile.WriteAll(_path, EmployeesHeader, employees.Select(ToRow));
            return true;
        }
        finally { _lock.Release(); }
    }

    public async Task<bool> SetPasswordHashAsync(int id, string passwordHash)
    {
        await _lock.WaitAsync();
        try
        {
            var employees = ReadEmployees();
            var employee = employees.FirstOrDefault(e => e.Id == id);
            if (employee == null)
                return false;
            employee.PasswordHash = passwordHash;
            CsvFile.WriteAll(_path, EmployeesHeader, employees.Select(ToRow));
            return true;
        }
        finally { _lock.Release(); }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await _lock.WaitAsync();
        try
        {
            var employees = ReadEmployees();
            if (employees.RemoveAll(e => e.Id == id) == 0)
                return false;
            CsvFile.WriteAll(_path, EmployeesHeader, employees.Select(ToRow));
            return true;
        }
        finally { _lock.Release(); }
    }

    private static string[] ToRow(Employee e) => new[]
    {
        e.Id.ToString(), e.Name, e.Email, e.Phone, e.Campus, e.Semester, e.Role, e.PasswordHash
    };

    // Read fresh every time, so edits made by hand to the CSV still show up without restarting the API.
    // Rows without a whole-number Id are skipped (e.g. a half-typed row), and missing trailing
    // fields are treated as empty.
    private List<Employee> ReadEmployees()
    {
        var columns = EmployeesHeader.Split(',').Length;
        return CsvFile.ReadRows(_path)
            .Select(r => r.Concat(Enumerable.Repeat("", Math.Max(0, columns - r.Length))).ToArray())
            .Where(r => int.TryParse(r[0], out _))
            .Select(r => new Employee
            {
                Id = int.Parse(r[0]),
                Name = r[1].Trim(),
                Email = r[2].Trim(),
                Phone = r[3].Trim(),
                Campus = r[4].Trim(),
                Semester = r[5].Trim(),
                Role = Employee.RoleFor(r[5].Trim()),
                PasswordHash = r[7].Trim()
            })
            .ToList();
    }
}
