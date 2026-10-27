namespace api.Models;

// One row in Data/promos.csv (later: the `promos` table in MySQL).
// A promo with a partner organization, shown in the Promo List and on the promo calendar.
public class Promo
{
    public static readonly string[] PartnerTypes = { "Sorority", "Student org", "Business" };

    // A promo is always at one campus (it picks the ordering app for [APP] in the scripts).
    public static readonly string[] Campuses = { "Alabama", "Ole Miss" };

    // The Status choices, in order. Cancelled can be chosen at any point.
    // Only Scheduled and Completed promos show on the promo calendar.
    public static readonly string[] Statuses = { "Reaching out", "Scheduled", "Completed", "Cancelled" };

    // Older status names, and what they mean now (applied when promos.csv is read).
    public static string UpgradeStatus(string status) => status switch
    {
        "Confirmed" => "Scheduled",
        _ => status
    };

    public int Id { get; set; }
    public string Name { get; set; } = "";        // the partner, e.g. "Alpha Chi Omega"
    public string PartnerType { get; set; } = ""; // Sorority, Student org or Business
    public string Campus { get; set; } = "";      // one of Campuses
    public DateOnly Date { get; set; }
    public string PromoCode { get; set; } = "";   // the code customers enter in the ordering app
    public string Status { get; set; } = Statuses[0];
    public string ContactName { get; set; } = "";
    public string ContactInfo { get; set; } = ""; // email or phone
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// One row in Data/scripts.csv (later: the `promo_scripts` table in MySQL): the outreach script for
// one partner type. Message goes to the partner; ForwardMessage is what they send on to their members.
// Placeholders filled in from a promo: [SORORITY] / [ORGANIZATION] / [BUSINESS] (its name),
// [CODE], [DAY/DATE] and [APP] (the ordering app for its campus).
public class PromoScript
{
    public int Id { get; set; }
    public string PartnerType { get; set; } = "";
    public string Message { get; set; } = "";
    public string ForwardMessage { get; set; } = "";
    public DateTime? UpdatedAt { get; set; }
}
