namespace api.Models;

// Turns the stored rows into what the API sends back. Shared by both controllers.
public static class DtoMapping
{
    public static IdeaDto ToDto(Idea idea) => new(
        idea.Id,
        idea.Name,
        idea.Title,
        idea.Campus,
        idea.ContentType,
        idea.ContentCategory,
        idea.Concept,
        idea.WhyItCouldWork,
        idea.Caption,
        idea.Platforms.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList(),
        idea.Hashtags,
        idea.CreatedAt,
        idea.ReviewStatus,
        idea.ReviewedAt,
        idea.ReviewStatus == Idea.Rejected ? idea.ReviewedAt?.AddDays(Idea.RejectedDaysKept) : null,
        idea.Links.Select(l => new LinkDto(l.Id, l.Type, l.Label, l.Url, l.IsUpload)).ToList());

    public static ProductionDto ToDto(ProductionItem item) => new(
        item.Id,
        item.Filmer,
        item.Editor,
        item.Reviewer,
        item.Poster,
        item.TargetPostDate,
        item.Status,
        item.UpdatedAt,
        ToDto(item.Idea));
}
