namespace api.Models;

// One row in Data/ideas.csv (later: the `ideas` table in MySQL).
public class Idea
{
    public const string Pending = "Pending";
    public const string Approved = "Approved"; // moved to the Production Tracker
    public const string Rejected = "Rejected"; // deleted automatically after RejectedDaysKept

    public const int RejectedDaysKept = 30;

    // The choices on the Ideas Form (the API rejects anything else).
    public static readonly string[] Campuses = { "Alabama", "Ole Miss", "Either" };
    public static readonly string[] ContentCategories = { "Food/Drink", "Customer/Student Life", "Employee", "Other" };

    public int Id { get; set; }
    public int? SubmittedById { get; set; } // who submitted it (employees.Id)
    public string Name { get; set; } = "";   // typed name, only on ideas from before employees.csv
    public string Title { get; set; } = ""; // content name
    public string Campus { get; set; } = "";
    public string ContentType { get; set; } = "";     // the format, e.g. Reel, Photo, Carousel
    public string ContentCategory { get; set; } = ""; // the topic: one of ContentCategories
    public string Concept { get; set; } = "";         // the concept and why it could work
    public string Caption { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    public string ReviewStatus { get; set; } = Pending; // Pending -> Approved or Rejected (final)
    public DateTime? ReviewedAt { get; set; }

    // Only filled in on older ideas, from when denying needed a reason. Kept so that data isn't lost.
    public string DenialReason { get; set; } = "";

    // Not stored in ideas.csv; filled in from links.csv by IdeaId.
    public List<IdeaLink> Links { get; set; } = new();
}
