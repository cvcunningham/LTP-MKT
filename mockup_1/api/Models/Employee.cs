namespace api.Models;

// One row in Data/employees.csv (later: the `employees` table in MySQL).
// Added, edited and deleted on the Intern Directory page.
public class Employee
{
    public const string Lead = "Lead";
    public const int LeadLevel = 4;

    // The choices in the Intern Directory's form (the API rejects anything else).
    public static readonly string[] Campuses = { "Alabama", "Ole Miss" };
    public static readonly string[] Semesters = { "1-2", "3-4", "5-6", "7-8" };
    public static readonly int[] AccessLevels = { 1, 2, 3, 4 };

    // Role comes from the access level: Level 4 interns are Leads, everyone else has no role.
    public static string RoleFor(int accessLevel) => accessLevel == LeadLevel ? Lead : "";

    // The level that usually goes with a semester (1-2 -> 1, 3-4 -> 2, ...). Used for rows saved
    // before access levels existed, so nobody's access changed when levels were added.
    public static int LevelForSemester(string semester) => Array.IndexOf(Semesters, semester) + 1;

    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Campus { get; set; } = "";
    public string Semester { get; set; } = ""; // the pair of semesters in the program, e.g. "1-2" (moved up by hand; information only)
    public int AccessLevel { get; set; }       // 1-4: what they can do (see Auth/Permissions.cs). 0 = not set.
    public string Role { get; set; } = "";     // "Lead" or blank (see RoleFor)

    // Never sent to the pages. Empty = the starting password (see Auth/Passwords.cs).
    public string PasswordHash { get; set; } = "";

    // Not stored in employees.csv; filled in from employee_page_access.csv by EmployeeId.
    // Page id -> Yes (true) / No (false), only for pages a Lead set differently from the level's default.
    public Dictionary<string, bool> PageAccess { get; set; } = new();
}
