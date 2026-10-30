using api.Models;
using MySqlConnector;
using static api.Repositories.MySqlDatabase;

namespace api.Repositories;

// The employee list (`employees` table) and each employee's page Yes/No choices
// (`employee_page_access`). Does the same as CsvEmployeeRepository (see IEmployeeRepository).
public class MySqlEmployeeRepository : IEmployeeRepository
{
    private readonly MySqlDatabase _db;

    public MySqlEmployeeRepository(MySqlDatabase db) => _db = db;

    public async Task<List<Employee>> GetAllAsync()
    {
        await using var connection = await _db.OpenAsync();

        // Employee id -> (page id -> Yes/No).
        var pageAccess = new Dictionary<int, Dictionary<string, bool>>();
        await using (var reader = await Command(connection, "SELECT employee_id, page, allowed FROM employee_page_access")
            .ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var employeeId = reader.GetInt32("employee_id");
                if (!pageAccess.TryGetValue(employeeId, out var pages))
                    pageAccess[employeeId] = pages = new Dictionary<string, bool>();
                pages[reader.GetString("page")] = reader.GetBoolean("allowed");
            }
        }

        var employees = new List<Employee>();
        await using (var reader = await Command(connection, "SELECT * FROM employees").ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var id = reader.GetInt32("id");
                var semester = Text(reader, "semester");
                // No level saved (rows from before levels): it comes from the semester, as in the CSV version.
                var level = OptionalInt(reader, "access_level") is int saved and > 0 ? saved : Employee.LevelForSemester(semester);
                employees.Add(new Employee
                {
                    Id = id,
                    Name = Text(reader, "name"),
                    Email = Text(reader, "email"),
                    Phone = Text(reader, "phone"),
                    Campus = Text(reader, "campus"),
                    Semester = semester,
                    AccessLevel = level,
                    Role = Employee.RoleFor(level),
                    PasswordHash = Text(reader, "password_hash"),
                    PageAccess = pageAccess.GetValueOrDefault(id) ?? new Dictionary<string, bool>()
                });
            }
        }

        return employees.OrderBy(e => e.Campus).ThenBy(e => e.Name).ToList(); // sorted the same way as before
    }

    public async Task AddAsync(Employee employee)
    {
        await using var connection = await _db.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await Command(connection, """
            INSERT INTO employees (id, name, email, phone, campus, semester, access_level, role, password_hash)
            VALUES (@Id, @Name, @Email, @Phone, @Campus, @Semester, @AccessLevel, @Role, @PasswordHash)
            """, new
            {
                employee.Id, employee.Name, employee.Email, employee.Phone, employee.Campus, employee.Semester,
                AccessLevel = SavedLevel(employee), employee.Role, employee.PasswordHash
            }, transaction).ExecuteNonQueryAsync();
        await WritePageAccessAsync(connection, transaction, employee.Id, employee.PageAccess);
        await transaction.CommitAsync();
    }

    // The password isn't touched (it changes only through SetPasswordHashAsync). If the id changes,
    // their page rows move with it (ON UPDATE CASCADE) and are then replaced.
    public async Task<bool> UpdateAsync(int oldId, Employee changes)
    {
        await using var connection = await _db.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var found = await Command(connection, """
            UPDATE employees
            SET id = @Id, name = @Name, email = @Email, phone = @Phone, campus = @Campus, semester = @Semester,
                access_level = @AccessLevel, role = @Role
            WHERE id = @oldId
            """, new
            {
                changes.Id, changes.Name, changes.Email, changes.Phone, changes.Campus, changes.Semester,
                AccessLevel = SavedLevel(changes), changes.Role, oldId
            }, transaction).ExecuteNonQueryAsync() > 0;
        if (!found)
            return false;

        await WritePageAccessAsync(connection, transaction, changes.Id, changes.PageAccess);
        await transaction.CommitAsync();
        return true;
    }

    public async Task<bool> SetPasswordHashAsync(int id, string passwordHash)
    {
        await using var connection = await _db.OpenAsync();
        return await Command(connection, "UPDATE employees SET password_hash = @passwordHash WHERE id = @id",
            new { passwordHash, id }).ExecuteNonQueryAsync() > 0;
    }

    // Their page rows go too (ON DELETE CASCADE). Their ideas and tracker jobs keep the id.
    public async Task<bool> DeleteAsync(int id)
    {
        await using var connection = await _db.OpenAsync();
        return await Command(connection, "DELETE FROM employees WHERE id = @id", new { id }).ExecuteNonQueryAsync() > 0;
    }

    // 0 = not set; saved as NULL, like the empty AccessLevel in the CSV file.
    private static int? SavedLevel(Employee e) => e.AccessLevel > 0 ? e.AccessLevel : null;

    // Replaces the employee's page rows with `pageAccess`.
    private static async Task WritePageAccessAsync(MySqlConnection connection, MySqlTransaction transaction,
        int employeeId, IReadOnlyDictionary<string, bool> pageAccess)
    {
        await Command(connection, "DELETE FROM employee_page_access WHERE employee_id = @employeeId",
            new { employeeId }, transaction).ExecuteNonQueryAsync();
        foreach (var (page, allowed) in pageAccess)
            await Command(connection,
                "INSERT INTO employee_page_access (employee_id, page, allowed) VALUES (@employeeId, @page, @allowed)",
                new { employeeId, page, allowed }, transaction).ExecuteNonQueryAsync();
    }
}
