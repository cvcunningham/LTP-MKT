namespace api.Models;

// What the submit form sends, as JSON in the "idea" part of a multipart form.
// Uploaded files would come in the "files" part, in the same order as Uploads
// (the upload button is hidden on the form for now, so Uploads is normally empty).
public class IdeaRequest
{
    public string? Name { get; set; }
    public string Title { get; set; } = "";
    public string? Campus { get; set; }
    public string? ContentType { get; set; }
    public string? ContentCategory { get; set; }
    public string? Concept { get; set; }
    public string? WhyItCouldWork { get; set; }
    public string? Caption { get; set; }
    public List<string> Platforms { get; set; } = new();
    public string? Hashtags { get; set; }
    public List<LinkRequest> Links { get; set; } = new();
    public List<UploadRequest> Uploads { get; set; } = new();
}

public class UploadRequest
{
    public string? Label { get; set; }
}

public class LinkRequest
{
    public string Type { get; set; } = "";
    public string? Label { get; set; }
    public string Url { get; set; } = "";
}

// What the Production Tracker's Save button sends (JSON).
public class ProductionUpdateRequest
{
    public string? Filmer { get; set; }
    public string? Editor { get; set; }
    public string? Reviewer { get; set; }
    public string? Poster { get; set; }
    public DateOnly? TargetPostDate { get; set; } // "2026-11-01", or null for no date
    public string Status { get; set; } = "";
}

// What GET /api/ideas sends back (JSON).
// IsUpload = the file was uploaded through the app, so the page can play/show it.
public record LinkDto(int Id, string Type, string Label, string Url, bool IsUpload);

public record IdeaDto(
    int Id,
    string Name,
    string Title,
    string Campus,
    string ContentType,
    string ContentCategory,
    string Concept,
    string WhyItCouldWork,
    string Caption,
    List<string> Platforms,
    string Hashtags,
    DateTime CreatedAt,
    string ReviewStatus,
    DateTime? ReviewedAt,
    DateTime? DeletesOn, // for Rejected ideas: when the app deletes it automatically
    List<LinkDto> Links);

// What GET /api/production sends back (JSON).
public record ProductionDto(
    int Id,
    string Filmer,
    string Editor,
    string Reviewer,
    string Poster,
    DateOnly? TargetPostDate,
    string Status,
    DateTime UpdatedAt,
    IdeaDto Idea);
