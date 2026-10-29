using api.Auth;
using api.Models;
using api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

// The Intern Directory (edit: Level 4; view only: anyone a Lead sets to Yes), and the employee list
// for the tracker's dropdowns.
[ApiController]
[Route("api/employees")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeRepository _repo;
    private readonly IIdeaRepository _ideas;
    private readonly SessionStore _sessions;

    public EmployeesController(IEmployeeRepository repo, IIdeaRepository ideas, SessionStore sessions)
    {
        _repo = repo;
        _ideas = ideas;
        _sessions = sessions;
    }

    // GET /api/employees   (sorted by campus, then name)
    [HttpGet]
    [RequirePermission(Permissions.AssignCrew, Permissions.EditTracker, Permissions.ViewDirectory, Permissions.ManageDirectory)]
    public async Task<List<EmployeeDto>> GetAll() =>
        (await _repo.GetAllAsync()).Select(DtoMapping.ToDto).ToList();

    // GET /api/employees/level-pages   (for each access level, the pages that are Yes by default,
    // e.g. { "1": ["form", "content-calendar"], ... }. The Edit pop-up resets the switches to these
    // when the level is changed.)
    [HttpGet("level-pages")]
    [RequirePermission(Permissions.ViewDirectory, Permissions.ManageDirectory)]
    public Dictionary<int, string[]> GetLevelPages() =>
        Employee.AccessLevels.ToDictionary(level => level, Permissions.DefaultPages);

    // GET /api/employees/next-id   (the id the Add form suggests: one higher than any id in use,
    // including deleted employees' ids that old ideas or tracker jobs still point to)
    [HttpGet("next-id")]
    [RequirePermission(Permissions.ManageDirectory)]
    public async Task<object> GetNextId() =>
        new { id = (await TakenIds()).DefaultIfEmpty(0).Max() + 1 };

    // POST /api/employees   (JSON body: id, name, email, phone, campus, semester, accessLevel,
    // pageAccess: { "tracker": true, ... })
    [HttpPost]
    [RequirePermission(Permissions.ManageDirectory)]
    public async Task<ActionResult<EmployeeDto>> Create(EmployeeRequest request)
    {
        var (employee, errors) = await Validate(request, null);
        if (errors.Count > 0)
            return BadRequest(new { errors });
        await _repo.AddAsync(employee);
        return Ok(DtoMapping.ToDto(employee));
    }

    // PUT /api/employees/3   (same body as POST; a different id in the body changes their id,
    // and their ideas and tracker jobs move to the new id)
    [HttpPut("{id}")]
    [RequirePermission(Permissions.ManageDirectory)]
    public async Task<IActionResult> Update(int id, EmployeeRequest request)
    {
        var (employee, errors) = await Validate(request, id);
        // So a Lead can't lock themselves out of the directory.
        if (id == HttpContext.CurrentEmployee().Id && !Permissions.Has(employee, Permissions.ManageDirectory))
            errors.Add("You can't take away your own access to edit the Intern Directory (Level 4 with Intern Directory " +
                       "set to Yes). Ask another Lead to do it.");
        if (errors.Count > 0)
            return BadRequest(new { errors });
        if (!await _repo.UpdateAsync(id, employee))
            return NotFound();
        if (employee.Id != id)
        {
            await _ideas.ReplaceEmployeeIdAsync(id, employee.Id);
            _sessions.ReplaceEmployeeId(id, employee.Id); // they stay logged in
        }
        return NoContent();
    }

    // POST /api/employees/3/reset-password   (back to the starting password, LTPMKT)
    [HttpPost("{id}/reset-password")]
    [RequirePermission(Permissions.ManageDirectory)]
    public async Task<IActionResult> ResetPassword(int id) =>
        await _repo.SetPasswordHashAsync(id, "") ? NoContent() : NotFound();

    // DELETE /api/employees/3   (permanent. Their ideas and tracker jobs keep the id and show
    // "(former employee #3)", so the id isn't suggested for anyone new.)
    [HttpDelete("{id}")]
    [RequirePermission(Permissions.ManageDirectory)]
    public async Task<IActionResult> Delete(int id)
    {
        if (id == HttpContext.CurrentEmployee().Id)
            return BadRequest(new { errors = new[] { "You can't delete yourself. Ask another Lead to do it." } });
        if (!await _repo.DeleteAsync(id))
            return NotFound();
        _sessions.RemoveEmployee(id); // logs them out
        return NoContent();
    }

    // Ids that can't be given to someone else: current employees, and deleted employees that
    // ideas or tracker jobs still point to.
    private async Task<HashSet<int>> TakenIds()
    {
        var ids = await _ideas.GetEmployeeIdsInUseAsync();
        ids.UnionWith((await _repo.GetAllAsync()).Select(e => e.Id));
        return ids;
    }

    // Checks the form. Id, name, campus, semester and access level are required. currentId is the
    // employee's id before this edit (null when adding).
    private async Task<(Employee Employee, List<string> Errors)> Validate(EmployeeRequest request, int? currentId)
    {
        var errors = new List<string>();
        if (request.Id is not int id || id < 1)
            errors.Add("Please enter an ID (a whole number, 1 or higher).");
        else if (id != currentId && (await TakenIds()).Contains(id))
            errors.Add($"ID {id} is already used (by another employee, or by old ideas or tracker jobs). Please choose a different ID.");
        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add("Please enter a name.");
        if (!Employee.Campuses.Contains(request.Campus))
            errors.Add("Please choose a campus: " + string.Join(", ", Employee.Campuses) + ".");
        if (!Employee.Semesters.Contains(request.Semester))
            errors.Add("Please choose a semester: " + string.Join(", ", Employee.Semesters) + ".");
        if (request.AccessLevel is not int level || !Employee.AccessLevels.Contains(level))
            errors.Add("Please choose an access level: " + string.Join(", ", Employee.AccessLevels) + ".");

        var employee = new Employee
        {
            Id = request.Id ?? 0,
            Name = request.Name?.Trim() ?? "",
            Email = request.Email?.Trim() ?? "",
            Phone = request.Phone?.Trim() ?? "",
            Campus = request.Campus ?? "",
            Semester = request.Semester ?? "",
            AccessLevel = request.AccessLevel ?? 0,
            Role = Employee.RoleFor(request.AccessLevel ?? 0),
            PageAccess = Permissions.Overrides(request.AccessLevel ?? 0, request.PageAccess)
        };
        return (employee, errors);
    }
}
