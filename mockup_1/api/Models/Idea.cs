namespace api.Models;

// One row in Data/ideas.csv (later: the `ideas` table in MySQL).
public class Idea
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Denied = "Denied";

    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Caption { get; set; } = "";
    public string Platforms { get; set; } = ""; // e.g. "Instagram;TikTok"
    public string Hashtags { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    public string ReviewStatus { get; set; } = Pending; // Pending -> Approved or Denied (final)
    public string DenialReason { get; set; } = "";
    public DateTime? ReviewedAt { get; set; }

    // Not stored in ideas.csv; filled in from links.csv by IdeaId.
    public List<IdeaLink> Links { get; set; } = new();
}
