using api.Models;
using api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/ideas")]
public class IdeasController : ControllerBase
{
    private static readonly string[] AllowedPlatforms = { "Instagram", "TikTok" };
    private static readonly string[] AllowedLinkTypes = { IdeaLink.Photo, IdeaLink.Video };
    private static readonly string[] Views = { Idea.Pending, Idea.Approved, Idea.Denied };

    private readonly IIdeaRepository _repo;

    public IdeasController(IIdeaRepository repo)
    {
        _repo = repo;
    }

    // GET /api/ideas?view=Pending   (Pending, Approved or Denied; default Pending)
    [HttpGet]
    public async Task<ActionResult<List<IdeaDto>>> GetAll([FromQuery] string view = Idea.Pending)
    {
        if (!Views.Contains(view))
            return BadRequest(new { errors = new[] { "View must be Pending, Approved or Denied." } });

        var ideas = await _repo.GetAllAsync();
        return ideas.Where(i => i.ReviewStatus == view).Select(ToDto).ToList();
    }

    // POST /api/ideas   (JSON body)
    [HttpPost]
    public async Task<ActionResult<IdeaDto>> Create(IdeaRequest request)
    {
        var errors = new List<string>();

        if (request.Platforms.Any(p => !AllowedPlatforms.Contains(p)))
            errors.Add("Platform must be Instagram or TikTok.");

        for (var i = 0; i < request.Links.Count; i++)
        {
            var link = request.Links[i];
            if (!AllowedLinkTypes.Contains(link.Type))
                errors.Add($"Link {i + 1}: choose Photo or Video.");
            if (!IsHttpsUrl(link.Url))
                errors.Add($"Link {i + 1}: must be a full web address starting with https://");
        }

        if (errors.Count > 0)
            return BadRequest(new { errors });

        var idea = new Idea
        {
            Name = request.Name?.Trim() ?? "",
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? "",
            Caption = request.Caption?.Trim() ?? "",
            Platforms = string.Join(";", request.Platforms),
            Hashtags = request.Hashtags?.Trim() ?? "",
            CreatedAt = DateTime.UtcNow
        };
        var links = request.Links.Select(l => new IdeaLink
        {
            Type = l.Type,
            Label = l.Label?.Trim() ?? "",
            Url = l.Url.Trim()
        }).ToList();

        var saved = await _repo.AddAsync(idea, links);
        return Ok(ToDto(saved));
    }

    // POST /api/ideas/5/approve
    [HttpPost("{id}/approve")]
    public Task<IActionResult> Approve(int id) => Review(id, Idea.Approved, "");

    // POST /api/ideas/5/deny   (JSON body: { "reason": "..." })
    [HttpPost("{id}/deny")]
    public Task<IActionResult> Deny(int id, DenyRequest request) => Review(id, Idea.Denied, request.Reason.Trim());

    // DELETE /api/ideas/5   (permanent: removes the idea and its links; works for any status)
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id) =>
        await _repo.DeleteAsync(id) ? NoContent() : NotFound();

    private async Task<IActionResult> Review(int id, string status, string denialReason)
    {
        if (await _repo.SetReviewAsync(id, status, denialReason))
            return NoContent();

        var idea = await _repo.GetByIdAsync(id);
        if (idea == null)
            return NotFound();
        return Conflict(new { errors = new[] { $"This idea was already {idea.ReviewStatus.ToLower()}. Decisions can't be changed." } });
    }

    private static bool IsHttpsUrl(string url) =>
        Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;

    private static IdeaDto ToDto(Idea idea) => new(
        idea.Id,
        idea.Name,
        idea.Title,
        idea.Description,
        idea.Caption,
        idea.Platforms.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList(),
        idea.Hashtags,
        idea.CreatedAt,
        idea.ReviewStatus,
        idea.DenialReason,
        idea.ReviewedAt,
        idea.Links.Select(l => new LinkDto(l.Type, l.Label, l.Url)).ToList());
}
