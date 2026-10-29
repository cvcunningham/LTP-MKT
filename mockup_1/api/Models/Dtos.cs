namespace api.Models;

// What the submit form sends, as JSON in the "idea" part of a multipart form.
// Uploaded files would come in the "files" part, in the same order as Uploads
// (the upload button is hidden on the form for now, so Uploads is normally empty).
public class IdeaRequest
{
    public string Title { get; set; } = "";
    public string? Campus { get; set; }
    public string? ContentType { get; set; }
    public string? ContentCategory { get; set; }
    public string? Concept { get; set; } // the concept and why it could work
    public string? Caption { get; set; }
    public List<LinkRequest> Links { get; set; } = new();
    public List<UploadRequest> Uploads { get; set; } = new();
}

public class UploadRequest
{
    public string? Label { get; set; }
}

public class LinkRequest
{
    public string Type { get; set; } = "";
    public string? Label { get; set; }
    public string Url { get; set; } = "";
}

// What the Production Tracker's Save button sends (JSON). The ids are employees.Id, or null for
// "not assigned".
public class ProductionUpdateRequest
{
    public int? FilmerId { get; set; }
    public int? EditorId { get; set; }
    public int? ReviewerId { get; set; }
    public int? PosterId { get; set; }
    public DateOnly? TargetPostDate { get; set; } // "2026-11-01", or null for no date
    public string Status { get; set; } = "";
}

// The login form (JSON).
public class LoginRequest
{
    public int Id { get; set; }
    public string? Password { get; set; }
}

// Change your own password (JSON).
public class ChangePasswordRequest
{
    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }
}

// What logging in sends back: the token to send with every request, and who you are.
public record LoginDto(string Token, MeDto Me);

// The logged-in employee and what they may do (Auth/Permissions.cs), so the page can show the right
// tabs and buttons. The API checks the same permissions on every request.
public record MeDto(EmployeeDto Employee, string[] Permissions);

// What the Intern Directory's Add / Save sends (JSON). Id is the employee's (new) id.
// PageAccess = the Yes/No switch for each page id (e.g. "tracker": true); a page that's left out
// gets the level's default.
public class EmployeeRequest
{
    public int? Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Campus { get; set; }
    public string? Semester { get; set; }
    public int? AccessLevel { get; set; }
    public Dictionary<string, bool> PageAccess { get; set; } = new();
}

// What the Promo List's Add / Save sends (JSON).
public class PromoRequest
{
    public string? Name { get; set; }
    public string? PartnerType { get; set; }
    public string? Campus { get; set; }
    public DateOnly? Date { get; set; } // "2026-11-01"
    public string? PromoCode { get; set; }
    public string? Status { get; set; }
    public string? ContactName { get; set; }
    public string? ContactInfo { get; set; }
}

// What an outreach script's Save sends (JSON).
public class ScriptUpdateRequest
{
    public string? Message { get; set; }
    public string? ForwardMessage { get; set; }
}

// What GET /api/ideas sends back (JSON).
// IsUpload = the file was uploaded through the app, so the page can play/show it.
public record LinkDto(int Id, string Type, string Label, string Url, bool IsUpload);

// Name = the submitter's name (from employees.csv, or the typed name on older ideas).
public record IdeaDto(
    int Id,
    int? SubmittedById,
    string Name,
    string Title,
    string Campus,
    string ContentType,
    string ContentCategory,
    string Concept,
    string Caption,
    DateTime CreatedAt,
    string ReviewStatus,
    DateTime? ReviewedAt,
    DateTime? DeletesOn, // for Rejected ideas: when the app deletes it automatically
    List<LinkDto> Links);

// What GET /api/production sends back (JSON). Each job has the employee's id (null if not
// assigned) and a name to show (the employee's name, or the typed name on older rows).
public record ProductionDto(
    int Id,
    int? FilmerId,
    string Filmer,
    int? EditorId,
    string Editor,
    int? ReviewerId,
    string Reviewer,
    int? PosterId,
    string Poster,
    DateOnly? TargetPostDate,
    string Status,
    DateTime UpdatedAt,
    IdeaDto Idea);

// What GET /api/employees sends back (JSON). PageAccess = Yes (true) / No (false) for every page id;
// CustomAccess = a Lead set at least one page differently from the level's default.
public record EmployeeDto(
    int Id,
    string Name,
    string Email,
    string Phone,
    string Campus,
    string Semester,
    int AccessLevel,
    string Role,
    Dictionary<string, bool> PageAccess,
    bool CustomAccess);

// What GET /api/promos sends back (JSON).
public record PromoDto(
    int Id,
    string Name,
    string PartnerType,
    string Campus,
    DateOnly Date,
    string PromoCode,
    string Status,
    string ContactName,
    string ContactInfo,
    DateTime UpdatedAt);

// What GET /api/promos/scripts sends back (JSON).
public record PromoScriptDto(int Id, string PartnerType, string Message, string ForwardMessage, DateTime? UpdatedAt);
