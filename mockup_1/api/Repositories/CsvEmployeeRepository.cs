using api.Models;

namespace api.Repositories;

public class CsvEmployeeRepository : IEmployeeRepository
{
    // New columns are added at the end, so CsvFile.EnsureSchema can upgrade older files.
    private const string EmployeesHeader = "Id,Name,Email,Phone,Campus,Semester,Role";

    private readonly string _path;

    public CsvEmployeeRepository(IWebHostEnvironment env)
    {
        _path = Path.Combine(env.ContentRootPath, "Data", "employees.csv");
        CsvFile.EnsureSchema(_path, EmployeesHeader);
    }

    // Read fresh every time, so edits made by hand to the CSV show up without restarting the API.
    // Rows without a whole-number Id are skipped (e.g. a half-typed row), and missing trailing
    // fields are treated as empty.
    public Task<List<Employee>> GetAllAsync()
    {
        var columns = EmployeesHeader.Split(',').Length;
        var employees = CsvFile.ReadRows(_path)
            .Select(r => r.Concat(Enumerable.Repeat("", Math.Max(0, columns - r.Length))).ToArray())
            .Where(r => int.TryParse(r[0], out _))
            .Select(r => new Employee
            {
                Id = int.Parse(r[0]),
                Name = r[1].Trim(),
                Email = r[2].Trim(),
                Phone = r[3].Trim(),
                Campus = r[4].Trim(),
                Semester = int.TryParse(r[5], out var semester) ? semester : null,
                Role = r[6].Trim() == "" ? Employee.Member : r[6].Trim()
            })
            .OrderBy(e => e.Campus)
            .ThenBy(e => e.Name)
            .ToList();
        return Task.FromResult(employees);
    }
}
