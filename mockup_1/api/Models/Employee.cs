namespace api.Models;

// One row in Data/employees.csv (later: the `employees` table in MySQL).
// Added, edited and deleted on the Intern Directory page.
public class Employee
{
    public const string Lead = "Lead";

    // The choices in the Intern Directory's form (the API rejects anything else).
    public static readonly string[] Campuses = { "Alabama", "Ole Miss" };
    public static readonly string[] Semesters = { "1-2", "3-4", "5-6", "7-8" };

    // Role comes from the semester: interns in semesters 7-8 are Leads, everyone else has no role.
    public static string RoleFor(string semester) => semester == "7-8" ? Lead : "";

    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Campus { get; set; } = "";
    public string Semester { get; set; } = ""; // the pair of semesters in the program, e.g. "1-2" (moved up by hand)
    public string Role { get; set; } = "";     // "Lead" or blank (see RoleFor)

    // Never sent to the pages. Empty = the starting password (see Auth/Passwords.cs).
    public string PasswordHash { get; set; } = "";
}
