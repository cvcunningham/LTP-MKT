using api.Models;

namespace api.Repositories;

public class CsvEmployeeRepository : IEmployeeRepository
{
    // CsvFile.EnsureSchema upgrades older files to these headers (matching columns by name).
    private const string EmployeesHeader = "Id,Name,Email,Phone,Campus,Semester,AccessLevel,Role,PasswordHash";
    // One row per page a Lead set differently from the person's level (Allowed = Yes or No).
    private const string PageAccessHeader = "Id,EmployeeId,Page,Allowed";

    private readonly string _path;
    private readonly string _pageAccessPath;
    private readonly SemaphoreSlim _lock = new(1, 1); // one read/write at a time

    public CsvEmployeeRepository(IWebHostEnvironment env)
    {
        _path = Path.Combine(env.ContentRootPath, "Data", "employees.csv");
        _pageAccessPath = Path.Combine(env.ContentRootPath, "Data", "employee_page_access.csv");
        CsvFile.EnsureSchema(_path, EmployeesHeader);
        CsvFile.EnsureSchema(_pageAccessPath, PageAccessHeader);
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
        try
        {
            CsvFile.AppendRow(_path, ToRow(employee));
            WritePageAccess(employee.Id, employee.Id, employee.PageAccess);
        }
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
            WritePageAccess(oldId, changes.Id, changes.PageAccess);
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
            WritePageAccess(id, id, new Dictionary<string, bool>());
            return true;
        }
        finally { _lock.Release(); }
    }

    private static string[] ToRow(Employee e) => new[]
    {
        e.Id.ToString(), e.Name, e.Email, e.Phone, e.Campus, e.Semester,
        e.AccessLevel > 0 ? e.AccessLevel.ToString() : "", e.Role, e.PasswordHash
    };

    // Replaces the page rows of the employee that had oldId with `pageAccess`, saved under newId.
    // Other employees' rows keep their Ids; new rows get the next free Id.
    private void WritePageAccess(int oldId, int newId, IReadOnlyDictionary<string, bool> pageAccess)
    {
        var rows = CsvFile.ReadRows(_pageAccessPath)
            .Where(r => r.Length >= 4 && int.TryParse(r[0], out _) && r[1].Trim() != oldId.ToString())
            .ToList();
        var nextId = rows.Select(r => int.Parse(r[0])).DefaultIfEmpty(0).Max() + 1;
        foreach (var (page, allowed) in pageAccess)
            rows.Add(new[] { (nextId++).ToString(), newId.ToString(), page, allowed ? "Yes" : "No" });
        CsvFile.WriteAll(_pageAccessPath, PageAccessHeader, rows);
    }

    // Employee id -> (page id -> Yes/No). Rows with an unknown Allowed value are skipped.
    private Dictionary<int, Dictionary<string, bool>> ReadPageAccess()
    {
        var byEmployee = new Dictionary<int, Dictionary<string, bool>>();
        foreach (var r in CsvFile.ReadRows(_pageAccessPath))
        {
            if (r.Length < 4 || !int.TryParse(r[1], out var employeeId))
                continue;
            var allowed = r[3].Trim();
            if (allowed != "Yes" && allowed != "No")
                continue;
            if (!byEmployee.TryGetValue(employeeId, out var pages))
                byEmployee[employeeId] = pages = new Dictionary<string, bool>();
            pages[r[2].Trim()] = allowed == "Yes";
        }
        return byEmployee;
    }

    // Read fresh every time, so edits made by hand to the CSV still show up without restarting the API.
    // Rows without a whole-number Id are skipped (e.g. a half-typed row), and missing trailing
    // fields are treated as empty. An empty AccessLevel (rows from before levels) comes from the semester.
    private List<Employee> ReadEmployees()
    {
        var columns = EmployeesHeader.Split(',').Length;
        var pageAccess = ReadPageAccess();
        return CsvFile.ReadRows(_path)
            .Select(r => r.Concat(Enumerable.Repeat("", Math.Max(0, columns - r.Length))).ToArray())
            .Where(r => int.TryParse(r[0], out _))
            .Select(r =>
            {
                var id = int.Parse(r[0]);
                var semester = r[5].Trim();
                var level = int.TryParse(r[6], out var saved) ? saved : Employee.LevelForSemester(semester);
                return new Employee
                {
                    Id = id,
                    Name = r[1].Trim(),
                    Email = r[2].Trim(),
                    Phone = r[3].Trim(),
                    Campus = r[4].Trim(),
                    Semester = semester,
                    AccessLevel = level,
                    Role = Employee.RoleFor(level),
                    PasswordHash = r[8].Trim(),
                    PageAccess = pageAccess.GetValueOrDefault(id) ?? new Dictionary<string, bool>()
                };
            })
            .ToList();
    }
}
