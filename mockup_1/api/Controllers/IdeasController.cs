using System.Text.Json;
using api.Auth;
using api.Models;
using api.Repositories;
using api.Storage;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/ideas")]
public class IdeasController : ControllerBase
{
    private static readonly string[] AllowedLinkTypes = { IdeaLink.Photo, IdeaLink.Video };

    // Uploads: up to 500 MB per file, and up to 2 GB for the whole submission.
    private const long MaxFileBytes = 500L * 1024 * 1024;
    private const long MaxSubmissionBytes = 2L * 1024 * 1024 * 1024;

    // File types that can be uploaded, and whether each counts as a Photo or a Video.
    private static readonly Dictionary<string, string> UploadTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = IdeaLink.Photo, [".jpeg"] = IdeaLink.Photo, [".png"] = IdeaLink.Photo,
        [".heic"] = IdeaLink.Photo, [".gif"] = IdeaLink.Photo,
        [".mp4"] = IdeaLink.Video, [".mov"] = IdeaLink.Video, [".m4v"] = IdeaLink.Video
    };

    private readonly IIdeaRepository _repo;
    private readonly IEmployeeRepository _employees;
    private readonly IFileStorage _storage;
    private readonly ILogger<IdeasController> _logger;

    public IdeasController(
        IIdeaRepository repo, IEmployeeRepository employees, IFileStorage storage, ILogger<IdeasController> logger)
    {
        _repo = repo;
        _employees = employees;
        _storage = storage;
        _logger = logger;
    }

    // GET /api/ideas   (the Idea Bank: waiting for review + rejected.
    // Approved ideas are on the Production Tracker instead.)
    [HttpGet]
    [RequirePermission(Permissions.ViewIdeas)]
    public async Task<ActionResult<List<IdeaDto>>> GetAll()
    {
        // Rejected ideas are deleted automatically once they're old enough. Checking here,
        // whenever the Idea Bank is loaded, is simple and often enough for 20-30 ideas a month.
        await _repo.DeleteRejectedBeforeAsync(DateTime.UtcNow.AddDays(-Idea.RejectedDaysKept));

        var ideas = await _repo.GetAllAsync();
        var employees = await EmployeesById();
        return ideas
            .Where(i => i.ReviewStatus == Idea.Pending || i.ReviewStatus == Idea.Rejected)
            .Select(i => DtoMapping.ToDto(i, employees))
            .ToList();
    }

    // POST /api/ideas   (multipart form: "idea" = the idea as JSON, "files" = uploaded files)
    // Everything is checked first, then the files go to OneDrive, then the idea is saved.
    // If any step fails, nothing is saved, so the user can just try again.
    // The idea is recorded as submitted by whoever is logged in.
    [HttpPost]
    [RequirePermission(Permissions.SubmitIdeas)]
    [RequestSizeLimit(MaxSubmissionBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxSubmissionBytes)]
    public async Task<ActionResult<IdeaDto>> Create(
        [FromForm(Name = "idea")] string ideaJson,
        [FromForm(Name = "files")] List<IFormFile> files,
        CancellationToken cancellationToken)
    {
        IdeaRequest? request;
        try { request = JsonSerializer.Deserialize<IdeaRequest>(ideaJson, JsonSerializerOptions.Web); }
        catch (JsonException) { request = null; }
        if (request == null)
            return BadRequest(new { errors = new[] { "The form didn't arrive correctly. Please try again." } });

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Title))
            errors.Add("Please give your idea a title.");

        if (!Idea.Campuses.Contains(request.Campus))
            errors.Add("Please choose a campus: " + string.Join(", ", Idea.Campuses) + ".");

        if (!Idea.ContentCategories.Contains(request.ContentCategory))
            errors.Add("Please choose a content category: " + string.Join(", ", Idea.ContentCategories) + ".");

        for (var i = 0; i < request.Links.Count; i++)
        {
            var link = request.Links[i];
            if (!AllowedLinkTypes.Contains(link.Type))
                errors.Add($"Link {i + 1}: choose Photo or Video.");
            if (!IsHttpsUrl(link.Url))
                errors.Add($"Link {i + 1}: must be a full web address starting with https://");
        }

        if (files.Count != request.Uploads.Count)
            errors.Add("Some of the files didn't arrive with the form. Please try again.");
        if (files.Count > 0 && !_storage.IsConfigured)
            errors.Add("Uploading files isn't set up yet (the OneDrive connection hasn't been configured). " +
                       "You can paste a OneDrive link instead.");

        for (var i = 0; i < files.Count; i++)
        {
            var file = files[i];
            if (!UploadTypes.ContainsKey(Path.GetExtension(file.FileName)))
                errors.Add($"File {i + 1} (\"{file.FileName}\"): only photos and videos can be uploaded " +
                           "(.jpg, .jpeg, .png, .heic, .gif, .mp4, .mov, .m4v).");
            if (file.Length == 0)
                errors.Add($"File {i + 1} (\"{file.FileName}\") is empty.");
            if (file.Length > MaxFileBytes)
                errors.Add($"File {i + 1} (\"{file.FileName}\") is bigger than 500 MB.");
        }

        if (errors.Count > 0)
            return BadRequest(new { errors });

        var idea = new Idea
        {
            SubmittedById = HttpContext.CurrentEmployee().Id,
            Title = request.Title.Trim(),
            Campus = request.Campus ?? "",
            ContentType = request.ContentType?.Trim() ?? "",
            ContentCategory = request.ContentCategory ?? "",
            Concept = request.Concept?.Trim() ?? "",
            Caption = request.Caption?.Trim() ?? "",
            CreatedAt = DateTime.UtcNow
        };
        var links = request.Links.Select(l => new IdeaLink
        {
            Type = l.Type,
            Label = l.Label?.Trim() ?? "",
            Url = l.Url.Trim()
        }).ToList();

        var uploaded = new List<StoredFile>();
        try
        {
            for (var i = 0; i < files.Count; i++)
            {
                await using var stream = files[i].OpenReadStream();
                var stored = await _storage.UploadAsync(stream, files[i].Length, files[i].FileName, cancellationToken);
                uploaded.Add(stored);
                links.Add(new IdeaLink
                {
                    Type = UploadTypes[Path.GetExtension(files[i].FileName)],
                    Label = request.Uploads[i].Label?.Trim() ?? "",
                    Url = stored.WebUrl,
                    DriveItemId = stored.Id
                });
            }

            var saved = await _repo.AddAsync(idea, links);
            return Ok(DtoMapping.ToDto(saved, await EmployeesById()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Saving idea \"{Title}\" failed", idea.Title);
            await RemoveUploads(uploaded);
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                errors = new[] { "A file couldn't be sent to OneDrive. Please try again. " +
                                 "If it keeps happening, tell the person who looks after this site." }
            });
        }
    }

    // GET /api/ideas/files/7   (plays/shows an uploaded file by sending the browser to
    // a short-lived direct address from OneDrive)
    // Not behind the login: <img>/<video> tags can't send the login token. Uploads are turned off;
    // if they're turned back on, this needs its own check (e.g. a short-lived token in the address).
    [HttpGet("files/{linkId:int}")]
    public async Task<IActionResult> GetFile(int linkId, CancellationToken cancellationToken)
    {
        var link = await _repo.GetLinkAsync(linkId);
        if (link == null || !link.IsUpload)
            return NotFound();

        try
        {
            return Redirect(await _storage.GetContentUrlAsync(link.DriveItemId, cancellationToken));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Couldn't get the file for link {LinkId} from OneDrive", linkId);
            return StatusCode(StatusCodes.Status502BadGateway);
        }
    }

    // POST /api/ideas/5/approve   (moves it to the Production Tracker)
    [HttpPost("{id}/approve")]
    [RequirePermission(Permissions.ReviewIdeas)]
    public async Task<IActionResult> Approve(int id) => await Decided(id, await _repo.ApproveAsync(id));

    // POST /api/ideas/5/reject   (no reason needed; deleted automatically after 30 days)
    [HttpPost("{id}/reject")]
    [RequirePermission(Permissions.ReviewIdeas)]
    public async Task<IActionResult> Reject(int id) => await Decided(id, await _repo.RejectAsync(id));

    // DELETE /api/ideas/5   (permanent: removes the idea and its links; works for any status).
    // Uploaded files stay in the OneDrive folder.
    [HttpDelete("{id}")]
    [RequirePermission(Permissions.ReviewIdeas)]
    public async Task<IActionResult> Delete(int id) =>
        await _repo.DeleteAsync(id) ? NoContent() : NotFound();

    private async Task<IActionResult> Decided(int id, bool changed)
    {
        if (changed)
            return NoContent();

        var idea = await _repo.GetByIdAsync(id);
        if (idea == null)
            return NotFound();
        return Conflict(new { errors = new[] { $"This idea was already {idea.ReviewStatus.ToLower()}. Decisions can't be changed." } });
    }

    // Cleans up files from a submission that failed partway, so they don't pile up in OneDrive.
    private async Task RemoveUploads(List<StoredFile> uploaded)
    {
        foreach (var file in uploaded)
        {
            try { await _storage.DeleteAsync(file.Id, CancellationToken.None); }
            catch (Exception ex) { _logger.LogWarning(ex, "Couldn't remove leftover OneDrive file {FileId}", file.Id); }
        }
    }

    private async Task<Dictionary<int, Employee>> EmployeesById() =>
        (await _employees.GetAllAsync()).ToDictionary(e => e.Id);

    private static bool IsHttpsUrl(string url) =>
        Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
}
