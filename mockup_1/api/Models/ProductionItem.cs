namespace api.Models;

// One row in Data/production.csv (later: the `production_items` table in MySQL).
// Created when an idea is approved. The content name, campus, type, category, caption and
// links come from the idea (by IdeaId); this row only holds the production details.
public class ProductionItem
{
    public const string Posted = "Posted";

    // The Status choices, in order.
    public static readonly string[] Statuses =
        { "Not started", "Filming", "Editing", "Ready for review", "Scheduled", Posted };

    public int Id { get; set; }
    public int IdeaId { get; set; }
    public string Filmer { get; set; } = "";
    public string Editor { get; set; } = "";
    public string Reviewer { get; set; } = ""; // reviewer/approver of the finished content
    public string Poster { get; set; } = "";   // who posts it
    public DateOnly? TargetPostDate { get; set; }
    public string Status { get; set; } = Statuses[0];
    public DateTime UpdatedAt { get; set; }

    // Not stored in production.csv; filled in from ideas.csv by IdeaId.
    public Idea Idea { get; set; } = new();
}
