using api.Models;

namespace api.Auth;

// What each semester group may do. The API checks these on every request (see RequirePermission),
// and the page gets the same list at login to decide which tabs and buttons to show.
public static class Permissions
{
    public const string SubmitIdeas = "SubmitIdeas";                 // Ideas Form
    public const string ViewContentCalendar = "ViewContentCalendar"; // Content Calendar (view only for everyone)
    public const string ViewTracker = "ViewTracker";                 // Production Tracker, view only
    public const string EditTracker = "EditTracker";                 // ... assign people, dates, status
    public const string DeleteTracker = "DeleteTracker";
    public const string ViewPromos = "ViewPromos";                   // Promo Calendar, view only (and Copy scripts)
    public const string EditPromos = "EditPromos";                   // ... add/edit promos, edit script wording
    public const string DeletePromos = "DeletePromos";
    public const string ReviewIdeas = "ReviewIdeas";                 // Ideas Bank: approve, deny, delete
    public const string ManageDirectory = "ManageDirectory";         // Intern Directory: add, edit, delete, reset passwords

    private static readonly string[] Semester12 = { SubmitIdeas, ViewContentCalendar };
    private static readonly string[] Semester34 = Semester12.Concat(new[] { ViewTracker, ViewPromos }).ToArray();
    private static readonly string[] Semester56 = Semester34.Concat(new[] { EditTracker, EditPromos }).ToArray();
    private static readonly string[] Semester78 = Semester56
        .Concat(new[] { DeleteTracker, DeletePromos, ReviewIdeas, ManageDirectory }).ToArray();

    private static readonly Dictionary<string, string[]> BySemester = new()
    {
        ["1-2"] = Semester12,
        ["3-4"] = Semester34,
        ["5-6"] = Semester56,
        ["7-8"] = Semester78
    };

    // Someone with no (or an unknown) semester can't do anything until a Lead sets it.
    public static string[] For(Employee employee) =>
        BySemester.TryGetValue(employee.Semester, out var permissions) ? permissions : Array.Empty<string>();

    public static bool Has(Employee employee, string permission) => For(employee).Contains(permission);
}
