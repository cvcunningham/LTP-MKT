using api.Models;
using api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

// The Production Tracker (and the Content Calendar): approved ideas, with who films, edits,
// reviews and posts them.
[ApiController]
[Route("api/production")]
public class ProductionController : ControllerBase
{
    private const string InProgressView = "InProgress";
    private const string CompletedView = "Completed";
    private const string AllView = "All";

    private readonly IIdeaRepository _repo;
    private readonly IEmployeeRepository _employees;

    public ProductionController(IIdeaRepository repo, IEmployeeRepository employees)
    {
        _repo = repo;
        _employees = employees;
    }

    // GET /api/production?view=InProgress   (everything not Completed yet; default)
    // GET /api/production?view=Completed
    // GET /api/production?view=All          (the Content Calendar)
    [HttpGet]
    public async Task<ActionResult<List<ProductionDto>>> GetAll([FromQuery] string view = InProgressView)
    {
        if (view != InProgressView && view != CompletedView && view != AllView)
            return BadRequest(new { errors = new[] { "View must be InProgress, Completed or All." } });

        var items = await _repo.GetProductionAsync();
        var employees = (await _employees.GetAllAsync()).ToDictionary(e => e.Id);
        return items
            .Where(p => view == AllView || (p.Status == ProductionItem.Completed) == (view == CompletedView))
            .Select(p => DtoMapping.ToDto(p, employees))
            .ToList();
    }

    // GET /api/production/statuses   (the Status choices, in order, for the dropdown)
    [HttpGet("statuses")]
    public string[] GetStatuses() => ProductionItem.Statuses;

    // PUT /api/production/3   (JSON body: filmerId, editorId, reviewerId, posterId, targetPostDate, status)
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, ProductionUpdateRequest request)
    {
        var errors = new List<string>();
        if (!ProductionItem.Statuses.Contains(request.Status))
            errors.Add("Please choose a status from the list.");

        var employees = (await _employees.GetAllAsync()).Select(e => e.Id).ToHashSet();
        var jobs = new (string Job, int? Id)[]
        {
            ("Filmer", request.FilmerId), ("Editor", request.EditorId),
            ("Reviewer/approver", request.ReviewerId), ("Poster", request.PosterId)
        };
        foreach (var (job, employeeId) in jobs)
        {
            if (employeeId is int chosen && !employees.Contains(chosen))
                errors.Add($"{job}: that person isn't on the employee list anymore. Please refresh the page and choose again.");
        }
        if (errors.Count > 0)
            return BadRequest(new { errors });

        var item = new ProductionItem
        {
            Id = id,
            FilmerId = request.FilmerId,
            EditorId = request.EditorId,
            ReviewerId = request.ReviewerId,
            PosterId = request.PosterId,
            TargetPostDate = request.TargetPostDate,
            Status = request.Status
        };
        return await _repo.UpdateProductionAsync(item) ? NoContent() : NotFound();
    }

    // DELETE /api/production/3   (permanent: removes the item, its idea and the idea's links)
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id) =>
        await _repo.DeleteProductionAsync(id) ? NoContent() : NotFound();
}
