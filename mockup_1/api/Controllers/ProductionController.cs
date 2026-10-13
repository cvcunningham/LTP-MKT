using api.Models;
using api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

// The Production Tracker: approved ideas, with who films, edits, reviews and posts them.
[ApiController]
[Route("api/production")]
public class ProductionController : ControllerBase
{
    private const string InProgressView = "InProgress";
    private const string PostedView = "Posted";

    private readonly IIdeaRepository _repo;

    public ProductionController(IIdeaRepository repo)
    {
        _repo = repo;
    }

    // GET /api/production?view=InProgress   (everything not Posted yet; default)
    // GET /api/production?view=Posted
    [HttpGet]
    public async Task<ActionResult<List<ProductionDto>>> GetAll([FromQuery] string view = InProgressView)
    {
        if (view != InProgressView && view != PostedView)
            return BadRequest(new { errors = new[] { "View must be InProgress or Posted." } });

        var items = await _repo.GetProductionAsync();
        return items
            .Where(p => (p.Status == ProductionItem.Posted) == (view == PostedView))
            .Select(DtoMapping.ToDto)
            .ToList();
    }

    // GET /api/production/statuses   (the Status choices, in order, for the dropdown)
    [HttpGet("statuses")]
    public string[] GetStatuses() => ProductionItem.Statuses;

    // PUT /api/production/3   (JSON body: filmer, editor, reviewer, poster, targetPostDate, status)
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, ProductionUpdateRequest request)
    {
        if (!ProductionItem.Statuses.Contains(request.Status))
            return BadRequest(new { errors = new[] { "Please choose a status from the list." } });

        var item = new ProductionItem
        {
            Id = id,
            Filmer = request.Filmer?.Trim() ?? "",
            Editor = request.Editor?.Trim() ?? "",
            Reviewer = request.Reviewer?.Trim() ?? "",
            Poster = request.Poster?.Trim() ?? "",
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
