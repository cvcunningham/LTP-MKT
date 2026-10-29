using api.Models;

namespace api.Auth;

// What each access level may do on each page, and the Intern Directory's per-page Yes/No switches.
// The API checks these on every request (see RequirePermission), and the page gets the same list at
// login to decide which tabs and buttons to show.
public static class Permissions
{
    public const string SubmitIdeas = "SubmitIdeas";                 // Ideas Form
    public const string ViewIdeas = "ViewIdeas";                     // Ideas Bank, view only
    public const string ReviewIdeas = "ReviewIdeas";                 // ... approve, deny, delete
    public const string ViewTracker = "ViewTracker";                 // Production Tracker, view only
    public const string AssignCrew = "AssignCrew";                   // ... change the Filmer and Editor
    public const string EditTracker = "EditTracker";                 // ... change everything (people, date, status)
    public const string DeleteTracker = "DeleteTracker";
    public const string ViewContentCalendar = "ViewContentCalendar"; // Content Calendar (view only for everyone)
    public const string ViewPromos = "ViewPromos";                   // Promo Calendar, view only (and Copy scripts)
    public const string EditPromos = "EditPromos";                   // ... add/edit promos, edit script wording
    public const string DeletePromos = "DeletePromos";
    public const string ViewDirectory = "ViewDirectory";             // Intern Directory, view only
    public const string ManageDirectory = "ManageDirectory";         // ... add, edit, delete, reset passwords

    // The pages, in navbar order. The ids match PAGES in common.js and the Page column of
    // employee_page_access.csv.
    public const string DirectoryPage = "directory";
    public static readonly string[] Pages =
        { "form", "bank", "tracker", "content-calendar", "promo-calendar", DirectoryPage };

    // What "Yes" gives on a page the person's level doesn't normally include: view only.
    private static readonly Dictionary<string, string[]> ViewOnly = new()
    {
        ["form"] = new[] { SubmitIdeas },
        ["bank"] = new[] { ViewIdeas },
        ["tracker"] = new[] { ViewTracker },
        ["content-calendar"] = new[] { ViewContentCalendar },
        ["promo-calendar"] = new[] { ViewPromos },
        [DirectoryPage] = new[] { ViewDirectory }
    };

    // What each level may do, page by page. A page that isn't listed is "No" by default for that level.
    private static readonly Dictionary<string, string[]> Level1 = new()
    {
        ["form"] = new[] { SubmitIdeas },
        ["content-calendar"] = new[] { ViewContentCalendar }
    };
    private static readonly Dictionary<string, string[]> Level2 = new(Level1)
    {
        ["tracker"] = new[] { ViewTracker, AssignCrew },
        ["promo-calendar"] = new[] { ViewPromos }
    };
    private static readonly Dictionary<string, string[]> Level3 = new(Level1)
    {
        ["tracker"] = new[] { ViewTracker, AssignCrew, EditTracker },
        ["promo-calendar"] = new[] { ViewPromos, EditPromos }
    };
    private static readonly Dictionary<string, string[]> Level4 = new(Level1)
    {
        ["bank"] = new[] { ViewIdeas, ReviewIdeas },
        ["tracker"] = new[] { ViewTracker, AssignCrew, EditTracker, DeleteTracker },
        ["promo-calendar"] = new[] { ViewPromos, EditPromos, DeletePromos },
        [DirectoryPage] = new[] { ViewDirectory, ManageDirectory }
    };

    private static readonly Dictionary<int, Dictionary<string, string[]>> ByLevel = new()
    {
        [1] = Level1,
        [2] = Level2,
        [3] = Level3,
        [4] = Level4
    };

    // The pages that are "Yes" by default for a level (none for no/an unknown level).
    public static string[] DefaultPages(int level) =>
        ByLevel.TryGetValue(level, out var pages) ? Pages.Where(pages.ContainsKey).ToArray() : Array.Empty<string>();

    // Yes/No for one page: the Lead's choice in the Intern Directory, or else the level's default.
    public static bool HasPage(Employee employee, string page) =>
        employee.PageAccess.TryGetValue(page, out var allowed) ? allowed : DefaultPages(employee.AccessLevel).Contains(page);

    // Someone with no (or an unknown) level gets only the pages a Lead has set to Yes, view only.
    public static string[] For(Employee employee)
    {
        var levelPages = ByLevel.GetValueOrDefault(employee.AccessLevel) ?? new Dictionary<string, string[]>();
        return Pages
            .Where(page => HasPage(employee, page))
            .SelectMany(page => levelPages.GetValueOrDefault(page) ?? ViewOnly[page])
            .ToArray();
    }

    public static bool Has(Employee employee, string permission) => For(employee).Contains(permission);

    // Only the Yes/No choices that differ from the level's defaults are stored, so a page left at
    // its default follows the level if the level changes later. Unknown page ids are ignored.
    public static Dictionary<string, bool> Overrides(int level, IReadOnlyDictionary<string, bool> choices)
    {
        var defaults = DefaultPages(level);
        return choices
            .Where(choice => Pages.Contains(choice.Key) && choice.Value != defaults.Contains(choice.Key))
            .ToDictionary(choice => choice.Key, choice => choice.Value);
    }
}
