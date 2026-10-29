using System.Globalization;
using api.Models;

namespace api.Repositories;

public class CsvIdeaRepository : IIdeaRepository
{
    // CsvFile.EnsureSchema upgrades older files to these headers (matching columns by name).
    private const string IdeasHeader =
        "Id,Name,Title,Concept,Caption,CreatedAt,ReviewStatus,DenialReason,ReviewedAt," +
        "Campus,ContentType,ContentCategory,SubmittedById";
    private const string LinksHeader = "Id,IdeaId,Type,Label,Url,DriveItemId";
    private const string ProductionHeader =
        "Id,IdeaId,Filmer,Editor,Reviewer,Poster,TargetPostDate,Status,UpdatedAt," +
        "FilmerId,EditorId,ReviewerId,PosterId";

    private readonly string _ideasPath;
    private readonly string _linksPath;
    private readonly string _productionPath;
    private readonly SemaphoreSlim _lock = new(1, 1); // one read/write at a time

    public CsvIdeaRepository(IWebHostEnvironment env)
    {
        _ideasPath = Path.Combine(env.ContentRootPath, "Data", "ideas.csv");
        _linksPath = Path.Combine(env.ContentRootPath, "Data", "links.csv");
        _productionPath = Path.Combine(env.ContentRootPath, "Data", "production.csv");
        CsvFile.EnsureSchema(_ideasPath, IdeasHeader);
        CsvFile.EnsureSchema(_linksPath, LinksHeader);
        CsvFile.EnsureSchema(_productionPath, ProductionHeader);
    }

    public async Task<List<Idea>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try { return ReadIdeasWithLinks(); }
        finally { _lock.Release(); }
    }

    public async Task<Idea?> GetByIdAsync(int id) =>
        (await GetAllAsync()).FirstOrDefault(i => i.Id == id);

    public async Task<IdeaLink?> GetLinkAsync(int linkId)
    {
        await _lock.WaitAsync();
        try { return ReadLinks().FirstOrDefault(l => l.Id == linkId); }
        finally { _lock.Release(); }
    }

    public async Task<Idea> AddAsync(Idea idea, List<IdeaLink> links)
    {
        await _lock.WaitAsync();
        try
        {
            // Next Id = highest Id + 1 (MySQL's AUTO_INCREMENT will do this for us later).
            idea.Id = NextId(ReadIdeas().Select(i => i.Id));
            CsvFile.AppendRow(_ideasPath, ToRow(idea));

            var nextLinkId = NextId(ReadLinks().Select(l => l.Id));
            foreach (var link in links)
            {
                link.Id = nextLinkId++;
                link.IdeaId = idea.Id;
                CsvFile.AppendRow(_linksPath, ToRow(link));
            }

            idea.Links = links;
            return idea;
        }
        finally { _lock.Release(); }
    }

    public async Task<bool> ApproveAsync(int id)
    {
        await _lock.WaitAsync();
        try
        {
            if (!Decide(id, Idea.Approved))
                return false;

            var item = new ProductionItem
            {
                Id = NextId(ReadProduction().Select(p => p.Id)),
                IdeaId = id,
                UpdatedAt = DateTime.UtcNow
            };
            CsvFile.AppendRow(_productionPath, ToRow(item));
            return true;
        }
        finally { _lock.Release(); }
    }

    public async Task<bool> RejectAsync(int id)
    {
        await _lock.WaitAsync();
        try { return Decide(id, Idea.Rejected); }
        finally { _lock.Release(); }
    }

    public async Task<int> DeleteRejectedBeforeAsync(DateTime cutoff)
    {
        await _lock.WaitAsync();
        try
        {
            var expired = ReadIdeas()
                .Where(i => i.ReviewStatus == Idea.Rejected && i.ReviewedAt < cutoff)
                .Select(i => i.Id)
                .ToHashSet();
            if (expired.Count > 0)
                DeleteIdeas(expired);
            return expired.Count;
        }
        finally { _lock.Release(); }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await _lock.WaitAsync();
        try
        {
            if (ReadIdeas().All(i => i.Id != id))
                return false;
            DeleteIdeas(new HashSet<int> { id });
            return true;
        }
        finally { _lock.Release(); }
    }

    public async Task<List<ProductionItem>> GetProductionAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var ideas = ReadIdeasWithLinks().ToDictionary(i => i.Id);
            return ReadProduction()
                .Where(p => ideas.ContainsKey(p.IdeaId))
                .Select(p =>
                {
                    p.Idea = ideas[p.IdeaId];
                    return p;
                })
                .OrderBy(p => p.TargetPostDate ?? DateOnly.MaxValue) // soonest first; no date at the end
                .ThenBy(p => p.Id)
                .ToList();
        }
        finally { _lock.Release(); }
    }

    public async Task<bool> UpdateProductionAsync(ProductionItem changes)
    {
        await _lock.WaitAsync();
        try
        {
            var items = ReadProduction();
            var item = items.FirstOrDefault(p => p.Id == changes.Id);
            if (item == null)
                return false;

            item.FilmerId = changes.FilmerId;
            item.EditorId = changes.EditorId;
            item.ReviewerId = changes.ReviewerId;
            item.PosterId = changes.PosterId;
            // Typed names from before employees.csv (the controller says which ones to keep).
            item.Filmer = changes.Filmer;
            item.Editor = changes.Editor;
            item.Reviewer = changes.Reviewer;
            item.Poster = changes.Poster;
            item.TargetPostDate = changes.TargetPostDate;
            item.Status = changes.Status;
            item.UpdatedAt = DateTime.UtcNow;

            CsvFile.WriteAll(_productionPath, ProductionHeader, items.Select(ToRow));
            return true;
        }
        finally { _lock.Release(); }
    }

    public async Task<bool> DeleteProductionAsync(int id)
    {
        await _lock.WaitAsync();
        try
        {
            var item = ReadProduction().FirstOrDefault(p => p.Id == id);
            if (item == null)
                return false;
            DeleteIdeas(new HashSet<int> { item.IdeaId });
            return true;
        }
        finally { _lock.Release(); }
    }

    public async Task<HashSet<int>> GetEmployeeIdsInUseAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var ids = ReadIdeas().Select(i => i.SubmittedById)
                .Concat(ReadProduction().SelectMany(p => new[] { p.FilmerId, p.EditorId, p.ReviewerId, p.PosterId }));
            return ids.OfType<int>().ToHashSet();
        }
        finally { _lock.Release(); }
    }

    public async Task ReplaceEmployeeIdAsync(int oldId, int newId)
    {
        await _lock.WaitAsync();
        try
        {
            int? Swap(int? id) => id == oldId ? newId : id;

            var ideas = ReadIdeas();
            ideas.ForEach(i => i.SubmittedById = Swap(i.SubmittedById));
            CsvFile.WriteAll(_ideasPath, IdeasHeader, ideas.Select(ToRow));

            var production = ReadProduction();
            production.ForEach(p =>
            {
                p.FilmerId = Swap(p.FilmerId);
                p.EditorId = Swap(p.EditorId);
                p.ReviewerId = Swap(p.ReviewerId);
                p.PosterId = Swap(p.PosterId);
            });
            CsvFile.WriteAll(_productionPath, ProductionHeader, production.Select(ToRow));
        }
        finally { _lock.Release(); }
    }

    // Sets a Pending idea to Approved or Rejected. Must be called while holding the lock.
    private bool Decide(int id, string status)
    {
        var ideas = ReadIdeas();
        var idea = ideas.FirstOrDefault(i => i.Id == id);
        if (idea == null || idea.ReviewStatus != Idea.Pending)
            return false; // decisions are final

        idea.ReviewStatus = status;
        idea.ReviewedAt = DateTime.UtcNow;
        CsvFile.WriteAll(_ideasPath, IdeasHeader, ideas.Select(ToRow));
        return true;
    }

    // Removes the ideas and everything that belongs to them. Must be called while holding the lock.
    private void DeleteIdeas(HashSet<int> ideaIds)
    {
        var ideas = ReadIdeas();
        var links = ReadLinks();
        var production = ReadProduction();
        ideas.RemoveAll(i => ideaIds.Contains(i.Id));
        links.RemoveAll(l => ideaIds.Contains(l.IdeaId));
        production.RemoveAll(p => ideaIds.Contains(p.IdeaId));

        CsvFile.WriteAll(_ideasPath, IdeasHeader, ideas.Select(ToRow));
        CsvFile.WriteAll(_linksPath, LinksHeader, links.Select(ToRow));
        CsvFile.WriteAll(_productionPath, ProductionHeader, production.Select(ToRow));
    }

    private List<Idea> ReadIdeasWithLinks()
    {
        var links = ReadLinks();
        return ReadIdeas()
            .Select(idea =>
            {
                idea.Links = links.Where(l => l.IdeaId == idea.Id).ToList();
                return idea;
            })
            .OrderByDescending(i => i.CreatedAt)
            .ToList();
    }

    private static int NextId(IEnumerable<int> ids) => ids.DefaultIfEmpty(0).Max() + 1;

    private static string[] ToRow(Idea i) => new[]
    {
        i.Id.ToString(), i.Name, i.Title, i.Concept, i.Caption,
        i.CreatedAt.ToString("o"), i.ReviewStatus, i.DenialReason,
        i.ReviewedAt?.ToString("o") ?? "",
        i.Campus, i.ContentType, i.ContentCategory, FormatId(i.SubmittedById)
    };

    private static string[] ToRow(IdeaLink l) => new[]
    {
        l.Id.ToString(), l.IdeaId.ToString(), l.Type, l.Label, l.Url, l.DriveItemId
    };

    private static string[] ToRow(ProductionItem p) => new[]
    {
        p.Id.ToString(), p.IdeaId.ToString(), p.Filmer, p.Editor, p.Reviewer, p.Poster,
        p.TargetPostDate?.ToString("yyyy-MM-dd") ?? "", p.Status, p.UpdatedAt.ToString("o"),
        FormatId(p.FilmerId), FormatId(p.EditorId), FormatId(p.ReviewerId), FormatId(p.PosterId)
    };

    // An optional id (e.g. who submitted an idea): "" when there's none.
    private static string FormatId(int? id) => id?.ToString() ?? "";
    private static int? ParseId(string value) => int.TryParse(value, out var id) ? id : null;

    private List<Idea> ReadIdeas() =>
        CsvFile.ReadRows(_ideasPath).Select(r => new Idea
        {
            Id = int.Parse(r[0]),
            Name = r[1],
            Title = r[2],
            Concept = r[3],
            Caption = r[4],
            CreatedAt = ParseDate(r[5]),
            ReviewStatus = r[6] == "" ? Idea.Pending : r[6],
            DenialReason = r[7],
            ReviewedAt = r[8] == "" ? null : ParseDate(r[8]),
            Campus = r[9],
            ContentType = r[10],
            ContentCategory = r[11],
            SubmittedById = ParseId(r[12])
        }).ToList();

    private List<IdeaLink> ReadLinks() =>
        CsvFile.ReadRows(_linksPath).Select(r => new IdeaLink
        {
            Id = int.Parse(r[0]),
            IdeaId = int.Parse(r[1]),
            Type = r[2],
            Label = r[3],
            Url = r[4],
            DriveItemId = r[5]
        }).ToList();

    private List<ProductionItem> ReadProduction() =>
        CsvFile.ReadRows(_productionPath).Select(r => new ProductionItem
        {
            Id = int.Parse(r[0]),
            IdeaId = int.Parse(r[1]),
            Filmer = r[2],
            Editor = r[3],
            Reviewer = r[4],
            Poster = r[5],
            TargetPostDate = r[6] == "" ? null : DateOnly.ParseExact(r[6], "yyyy-MM-dd", CultureInfo.InvariantCulture),
            Status = ProductionItem.UpgradeStatus(r[7]),
            UpdatedAt = ParseDate(r[8]),
            FilmerId = ParseId(r[9]),
            EditorId = ParseId(r[10]),
            ReviewerId = ParseId(r[11]),
            PosterId = ParseId(r[12])
        }).ToList();

    private static DateTime ParseDate(string value) =>
        DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}
