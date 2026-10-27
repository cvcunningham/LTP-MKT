namespace api.Models;

// Turns the stored rows into what the API sends back. Shared by the controllers.
// `employees` (by Id) turns the stored employee ids into names.
public static class DtoMapping
{
    public static IdeaDto ToDto(Idea idea, IReadOnlyDictionary<int, Employee> employees) => new(
        idea.Id,
        idea.SubmittedById,
        NameOf(idea.SubmittedById, idea.Name, employees),
        idea.Title,
        idea.Campus,
        idea.ContentType,
        idea.ContentCategory,
        idea.Concept,
        idea.Caption,
        idea.CreatedAt,
        idea.ReviewStatus,
        idea.ReviewedAt,
        idea.ReviewStatus == Idea.Rejected ? idea.ReviewedAt?.AddDays(Idea.RejectedDaysKept) : null,
        idea.Links.Select(l => new LinkDto(l.Id, l.Type, l.Label, l.Url, l.IsUpload)).ToList());

    public static ProductionDto ToDto(ProductionItem item, IReadOnlyDictionary<int, Employee> employees) => new(
        item.Id,
        item.FilmerId,
        NameOf(item.FilmerId, item.Filmer, employees),
        item.EditorId,
        NameOf(item.EditorId, item.Editor, employees),
        item.ReviewerId,
        NameOf(item.ReviewerId, item.Reviewer, employees),
        item.PosterId,
        NameOf(item.PosterId, item.Poster, employees),
        item.TargetPostDate,
        item.Status,
        item.UpdatedAt,
        ToDto(item.Idea, employees));

    public static EmployeeDto ToDto(Employee e) =>
        new(e.Id, e.Name, e.Email, e.Phone, e.Campus, e.Semester, e.Role);

    public static PromoDto ToDto(Promo p) =>
        new(p.Id, p.Name, p.PartnerType, p.Campus, p.Date, p.PromoCode, p.Status, p.ContactName, p.ContactInfo, p.UpdatedAt);

    public static PromoScriptDto ToDto(PromoScript s) =>
        new(s.Id, s.PartnerType, s.Message, s.ForwardMessage, s.UpdatedAt);

    // The employee's name; the typed name on rows from before employees.csv; or "(former employee #3)"
    // if the employee was removed from employees.csv.
    private static string NameOf(int? id, string typedName, IReadOnlyDictionary<int, Employee> employees)
    {
        if (id is not int employeeId)
            return typedName;
        return employees.TryGetValue(employeeId, out var employee) ? employee.Name : $"(former employee #{employeeId})";
    }
}
