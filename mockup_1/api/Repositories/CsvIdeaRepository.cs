using System.Globalization;
using api.Models;

namespace api.Repositories;

public class CsvIdeaRepository : IIdeaRepository
{
    private const string IdeasHeader =
        "Id,Name,Title,Description,Caption,Platforms,Hashtags,CreatedAt,ReviewStatus,DenialReason,ReviewedAt";
    private const string LinksHeader = "Id,IdeaId,Type,Label,Url";

    private readonly string _ideasPath;
    private readonly string _linksPath;
    private readonly SemaphoreSlim _lock = new(1, 1); // one read/write at a time

    public CsvIdeaRepository(IWebHostEnvironment env)
    {
        _ideasPath = Path.Combine(env.ContentRootPath, "Data", "ideas.csv");
        _linksPath = Path.Combine(env.ContentRootPath, "Data", "links.csv");
        CsvFile.EnsureSchema(_ideasPath, IdeasHeader);
        CsvFile.EnsureSchema(_linksPath, LinksHeader);
    }

    public async Task<List<Idea>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try
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
        finally { _lock.Release(); }
    }

    public async Task<Idea?> GetByIdAsync(int id) =>
        (await GetAllAsync()).FirstOrDefault(i => i.Id == id);

    public async Task<Idea> AddAsync(Idea idea, List<IdeaLink> links)
    {
        await _lock.WaitAsync();
        try
        {
            // Next Id = highest Id + 1 (MySQL's AUTO_INCREMENT will do this for us later).
            idea.Id = ReadIdeas().Select(i => i.Id).DefaultIfEmpty(0).Max() + 1;
            CsvFile.AppendRow(_ideasPath, ToRow(idea));

            var nextLinkId = ReadLinks().Select(l => l.Id).DefaultIfEmpty(0).Max() + 1;
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

    public Task<bool> SetReviewAsync(int id, string reviewStatus, string denialReason) =>
        UpdateIdea(id, idea =>
        {
            if (idea.ReviewStatus != Idea.Pending)
                return false; // decisions are final
            idea.ReviewStatus = reviewStatus;
            idea.DenialReason = denialReason;
            idea.ReviewedAt = DateTime.UtcNow;
            return true;
        });

    public async Task<bool> DeleteAsync(int id)
    {
        await _lock.WaitAsync();
        try
        {
            var ideas = ReadIdeas();
            if (ideas.RemoveAll(i => i.Id == id) == 0)
                return false;

            var links = ReadLinks();
            links.RemoveAll(l => l.IdeaId == id);

            CsvFile.WriteAll(_ideasPath, IdeasHeader, ideas.Select(ToRow));
            CsvFile.WriteAll(_linksPath, LinksHeader, links.Select(ToRow));
            return true;
        }
        finally { _lock.Release(); }
    }

    // Finds the idea, applies the change, and rewrites ideas.csv if the change returned true.
    private async Task<bool> UpdateIdea(int id, Func<Idea, bool> change)
    {
        await _lock.WaitAsync();
        try
        {
            var ideas = ReadIdeas();
            var idea = ideas.FirstOrDefault(i => i.Id == id);
            if (idea == null || !change(idea))
                return false;

            CsvFile.WriteAll(_ideasPath, IdeasHeader, ideas.Select(ToRow));
            return true;
        }
        finally { _lock.Release(); }
    }

    private static string[] ToRow(Idea i) => new[]
    {
        i.Id.ToString(), i.Name, i.Title, i.Description, i.Caption, i.Platforms, i.Hashtags,
        i.CreatedAt.ToString("o"), i.ReviewStatus, i.DenialReason,
        i.ReviewedAt?.ToString("o") ?? ""
    };

    private static string[] ToRow(IdeaLink l) => new[]
    {
        l.Id.ToString(), l.IdeaId.ToString(), l.Type, l.Label, l.Url
    };

    private List<Idea> ReadIdeas() =>
        CsvFile.ReadRows(_ideasPath).Select(r => new Idea
        {
            Id = int.Parse(r[0]),
            Name = r[1],
            Title = r[2],
            Description = r[3],
            Caption = r[4],
            Platforms = r[5],
            Hashtags = r[6],
            CreatedAt = ParseDate(r[7]),
            ReviewStatus = r[8] == "" ? Idea.Pending : r[8],
            DenialReason = r[9],
            ReviewedAt = r[10] == "" ? null : ParseDate(r[10])
        }).ToList();

    private List<IdeaLink> ReadLinks() =>
        CsvFile.ReadRows(_linksPath).Select(r => new IdeaLink
        {
            Id = int.Parse(r[0]),
            IdeaId = int.Parse(r[1]),
            Type = r[2],
            Label = r[3],
            Url = r[4]
        }).ToList();

    private static DateTime ParseDate(string value) =>
        DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}
