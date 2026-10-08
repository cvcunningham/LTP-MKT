using System.ComponentModel.DataAnnotations;

namespace api.Models;

// What the submit form sends (JSON).
public class IdeaRequest
{
    public string? Name { get; set; }

    [Required]
    public string Title { get; set; } = "";

    public string? Description { get; set; }
    public string? Caption { get; set; }
    public List<string> Platforms { get; set; } = new();
    public string? Hashtags { get; set; }
    public List<LinkRequest> Links { get; set; } = new();
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
public record LinkDto(string Type, string Label, string Url);

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
