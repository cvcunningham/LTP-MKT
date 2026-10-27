namespace api.Models;

// One row in Data/production.csv (later: the `production_items` table in MySQL).
// Created when an idea is approved. The content name, campus, type, category, caption and
// links come from the idea (by IdeaId); this row only holds the production details.
public class ProductionItem
{
    public const string Scheduled = "Scheduled"; // needs a target post date
    public const string Completed = "Completed";

    // The Status choices, in order. A newly approved idea starts at the first one.
    // Only Scheduled and Completed items show on the Content Calendar.
    public static readonly string[] Statuses =
        { "Needs filming", "Needs editing", "Needs final review", Scheduled, Completed };

    // Older status names, and what they mean now (applied when production.csv is read).
    public static string UpgradeStatus(string status) => status switch
    {
        "" or "Not started" or "Filming" => "Needs filming",
        "Editing" => "Needs editing",
        "Ready for review" => "Needs final review",
        "Posted" => Completed,
        _ => status
    };

    public int Id { get; set; }
    public int IdeaId { get; set; }
    // Who does each job (employees.Id, or null if nobody is assigned yet).
    public int? FilmerId { get; set; }
    public int? EditorId { get; set; }
    public int? ReviewerId { get; set; } // reviewer/approver of the finished content
    public int? PosterId { get; set; }   // who posts it

    // Typed names, only on rows from before employees.csv. Shown when the Id above is empty,
    // and cleared the next time the item is saved.
    public string Filmer { get; set; } = "";
    public string Editor { get; set; } = "";
    public string Reviewer { get; set; } = "";
    public string Poster { get; set; } = "";
    public DateOnly? TargetPostDate { get; set; }
    public string Status { get; set; } = Statuses[0];
    public DateTime UpdatedAt { get; set; }

    // Not stored in production.csv; filled in from ideas.csv by IdeaId.
    public Idea Idea { get; set; } = new();
}
