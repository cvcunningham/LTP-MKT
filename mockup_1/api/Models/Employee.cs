namespace api.Models;

// One row in Data/employees.csv (later: the `employees` table in MySQL).
// The file is edited by hand for now; the app only reads it (for the name dropdowns).
public class Employee
{
    public const string Member = "Member";
    public const string MarketingChair = "Marketing Chair";
    public const string Executive = "Executive";

    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Campus { get; set; } = "";
    public int? Semester { get; set; } // semester in the program: 1, 2, 3, ... (updated by hand each term)
    public string Role { get; set; } = Member; // Member, Marketing Chair or Executive
}
