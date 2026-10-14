using api.Models;
using api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

// The employee list, for the name dropdowns. Read only: employees.csv is edited by hand for now.
[ApiController]
[Route("api/employees")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeRepository _repo;

    public EmployeesController(IEmployeeRepository repo)
    {
        _repo = repo;
    }

    // GET /api/employees   (sorted by campus, then name)
    [HttpGet]
    public async Task<List<EmployeeDto>> GetAll() =>
        (await _repo.GetAllAsync()).Select(DtoMapping.ToDto).ToList();
}
