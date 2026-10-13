namespace api.Models;

// One row in Data/ideas.csv (later: the `ideas` table in MySQL).
public class Idea
{
    public const string Pending = "Pending";
    public const string Approved = "Approved"; // moved to the Production Tracker
    public const string Rejected = "Rejected"; // deleted automatically after RejectedDaysKept

    public const int RejectedDaysKept = 30;

    public int Id { get; set; }
    public string Name { get; set; } = "";  // who submitted it
    public string Title { get; set; } = ""; // content name
    public string Campus { get; set; } = "";
    public string ContentType { get; set; } = "";     // the format, e.g. Reel, Photo, Carousel
    public string ContentCategory { get; set; } = ""; // the topic, e.g. Food, Events
    public string Concept { get; set; } = "";
    public string WhyItCouldWork { get; set; } = "";
    public string Caption { get; set; } = "";
    public string Platforms { get; set; } = ""; // e.g. "Instagram;TikTok"
    public string Hashtags { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    public string ReviewStatus { get; set; } = Pending; // Pending -> Approved or Rejected (final)
    public DateTime? ReviewedAt { get; set; }

    // Only filled in on older ideas, from when denying needed a reason. Kept so that data isn't lost.
    public string DenialReason { get; set; } = "";

    // Not stored in ideas.csv; filled in from links.csv by IdeaId.
    public List<IdeaLink> Links { get; set; } = new();
}
