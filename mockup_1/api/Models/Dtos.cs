using System.ComponentModel.DataAnnotations;

namespace api.Models;

// What the submit form sends, as JSON in the "idea" part of a multipart form.
// The uploaded files come in the "files" part, in the same order as Uploads.
public class IdeaRequest
{
    public string? Name { get; set; }
    public string Title { get; set; } = "";

    public string? Description { get; set; }
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

// What the Deny button sends (JSON).
public class DenyRequest
{
    [Required]
    public string Reason { get; set; } = "";
}

// What GET /api/ideas sends back (JSON).
// IsUpload = the file was uploaded through the app, so the review page can play/show it.
public record LinkDto(int Id, string Type, string Label, string Url, bool IsUpload);

public record IdeaDto(
    int Id,
    string Name,
    string Title,
    string Description,
    string Caption,
    List<string> Platforms,
    string Hashtags,
    DateTime CreatedAt,
    string ReviewStatus,
    string DenialReason,
    DateTime? ReviewedAt,
    List<LinkDto> Links);
