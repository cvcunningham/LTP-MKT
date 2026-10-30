using api.Models;
using MySqlConnector;
using static api.Repositories.MySqlDatabase;

namespace api.Repositories;

// Ideas, their links and the Production Tracker, in the `ideas`, `idea_links` and
// `production_items` tables. Does the same as CsvIdeaRepository (see IIdeaRepository).
public class MySqlIdeaRepository : IIdeaRepository
{
    private readonly MySqlDatabase _db;

    public MySqlIdeaRepository(MySqlDatabase db) => _db = db;

    public async Task<List<Idea>> GetAllAsync()
    {
        await using var connection = await _db.OpenAsync();
        return await ReadIdeasWithLinksAsync(connection, "");
    }

    public async Task<Idea?> GetByIdAsync(int id)
    {
        await using var connection = await _db.OpenAsync();
        return (await ReadIdeasWithLinksAsync(connection, "WHERE id = @id", new { id })).FirstOrDefault();
    }

    public async Task<IdeaLink?> GetLinkAsync(int linkId)
    {
        await using var connection = await _db.OpenAsync();
        return (await ReadLinksAsync(connection, "WHERE id = @linkId", new { linkId })).FirstOrDefault();
    }

    public async Task<Idea> AddAsync(Idea idea, List<IdeaLink> links)
    {
        await using var connection = await _db.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync(); // the idea and its links, or nothing

        idea.Id = await InsertIdeaAsync(connection, transaction, idea, keepId: false);
        foreach (var link in links)
        {
            link.IdeaId = idea.Id;
            link.Id = await InsertLinkAsync(connection, transaction, link, keepId: false);
        }

        await transaction.CommitAsync();
        idea.Links = links;
        return idea;
    }

    public async Task<bool> ApproveAsync(int id)
    {
        await using var connection = await _db.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync(); // approve + tracker item, or nothing
        if (!await DecideAsync(connection, transaction, id, Idea.Approved))
            return false;

        await Command(connection,
            "INSERT INTO production_items (idea_id, status, updated_at) VALUES (@id, @status, @now)",
            new { id, status = ProductionItem.Statuses[0], now = DateTime.UtcNow }, transaction).ExecuteNonQueryAsync();
        await transaction.CommitAsync();
        return true;
    }

    public async Task<bool> RejectAsync(int id)
    {
        await using var connection = await _db.OpenAsync();
        return await DecideAsync(connection, null, id, Idea.Rejected);
    }

    // The links and tracker items go too (ON DELETE CASCADE).
    public async Task<int> DeleteRejectedBeforeAsync(DateTime cutoff)
    {
        await using var connection = await _db.OpenAsync();
        return await Command(connection,
            "DELETE FROM ideas WHERE review_status = @rejected AND reviewed_at < @cutoff",
            new { rejected = Idea.Rejected, cutoff }).ExecuteNonQueryAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var connection = await _db.OpenAsync();
        return await Command(connection, "DELETE FROM ideas WHERE id = @id", new { id }).ExecuteNonQueryAsync() > 0;
    }

    public async Task<List<ProductionItem>> GetProductionAsync()
    {
        await using var connection = await _db.OpenAsync();
        var ideas = (await ReadIdeasWithLinksAsync(connection, "")).ToDictionary(i => i.Id);

        var items = new List<ProductionItem>();
        await using (var reader = await Command(connection,
            // Soonest target post date first; items with no date at the end.
            "SELECT * FROM production_items ORDER BY target_post_date IS NULL, target_post_date, id")
            .ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                items.Add(new ProductionItem
                {
                    Id = reader.GetInt32("id"),
                    IdeaId = reader.GetInt32("idea_id"),
                    Filmer = Text(reader, "filmer"),
                    Editor = Text(reader, "editor"),
                    Reviewer = Text(reader, "reviewer"),
                    Poster = Text(reader, "poster"),
                    TargetPostDate = OptionalDate(reader, "target_post_date"),
                    Status = ProductionItem.UpgradeStatus(Text(reader, "status")),
                    UpdatedAt = Utc(reader, "updated_at"),
                    FilmerId = OptionalInt(reader, "filmer_id"),
                    EditorId = OptionalInt(reader, "editor_id"),
                    ReviewerId = OptionalInt(reader, "reviewer_id"),
                    PosterId = OptionalInt(reader, "poster_id")
                });
        }

        items.ForEach(p => p.Idea = ideas[p.IdeaId]); // the foreign key means every item has its idea
        return items;
    }

    public async Task<bool> UpdateProductionAsync(ProductionItem item)
    {
        await using var connection = await _db.OpenAsync();
        return await Command(connection, """
            UPDATE production_items
            SET filmer_id = @FilmerId, editor_id = @EditorId, reviewer_id = @ReviewerId, poster_id = @PosterId,
                filmer = @Filmer, editor = @Editor, reviewer = @Reviewer, poster = @Poster,
                target_post_date = @TargetPostDate, status = @Status, updated_at = @UpdatedAt
            WHERE id = @Id
            """, new
            {
                item.FilmerId, item.EditorId, item.ReviewerId, item.PosterId,
                item.Filmer, item.Editor, item.Reviewer, item.Poster,
                item.TargetPostDate, item.Status, UpdatedAt = DateTime.UtcNow, item.Id
            }).ExecuteNonQueryAsync() > 0;
    }

    // Deleting the idea deletes the item and the links too (ON DELETE CASCADE).
    public async Task<bool> DeleteProductionAsync(int id)
    {
        await using var connection = await _db.OpenAsync();
        return await Command(connection,
            "DELETE FROM ideas WHERE id = (SELECT idea_id FROM production_items WHERE id = @id)",
            new { id }).ExecuteNonQueryAsync() > 0;
    }

    public async Task<HashSet<int>> GetEmployeeIdsInUseAsync()
    {
        await using var connection = await _db.OpenAsync();
        var ids = new HashSet<int>();
        await using var reader = await Command(connection, """
            SELECT submitted_by_id AS id FROM ideas
            UNION SELECT filmer_id FROM production_items
            UNION SELECT editor_id FROM production_items
            UNION SELECT reviewer_id FROM production_items
            UNION SELECT poster_id FROM production_items
            """).ExecuteReaderAsync();
        while (await reader.ReadAsync())
            if (OptionalInt(reader, "id") is int id)
                ids.Add(id);
        return ids;
    }

    public async Task ReplaceEmployeeIdAsync(int oldId, int newId)
    {
        await using var connection = await _db.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var ids = new { oldId, newId };
        await Command(connection, "UPDATE ideas SET submitted_by_id = @newId WHERE submitted_by_id = @oldId", ids, transaction)
            .ExecuteNonQueryAsync();
        foreach (var column in new[] { "filmer_id", "editor_id", "reviewer_id", "poster_id" })
            await Command(connection, $"UPDATE production_items SET {column} = @newId WHERE {column} = @oldId", ids, transaction)
                .ExecuteNonQueryAsync();
        await transaction.CommitAsync();
    }

    // Sets a Pending idea to Approved or Rejected. "AND review_status = Pending" makes decisions final:
    // 0 rows changed = it doesn't exist or was already decided.
    private static async Task<bool> DecideAsync(MySqlConnection connection, MySqlTransaction? transaction, int id, string status) =>
        await Command(connection,
            "UPDATE ideas SET review_status = @status, reviewed_at = @now WHERE id = @id AND review_status = @pending",
            new { status, now = DateTime.UtcNow, id, pending = Idea.Pending }, transaction).ExecuteNonQueryAsync() > 0;

    // Inserts the idea and returns its id: MySQL picks a new one, or (keepId, when copying the CSV
    // files in) idea.Id is kept.
    public static async Task<int> InsertIdeaAsync(MySqlConnection connection, MySqlTransaction transaction, Idea i, bool keepId)
    {
        var command = Command(connection, """
            INSERT INTO ideas (id, name, title, concept, caption, created_at, review_status, denial_reason,
                reviewed_at, campus, content_type, content_category, submitted_by_id)
            VALUES (@Id, @Name, @Title, @Concept, @Caption, @CreatedAt, @ReviewStatus, @DenialReason,
                @ReviewedAt, @Campus, @ContentType, @ContentCategory, @SubmittedById)
            """, new
            {
                Id = keepId ? (int?)i.Id : null, i.Name, i.Title, i.Concept, i.Caption, i.CreatedAt, i.ReviewStatus,
                i.DenialReason, i.ReviewedAt, i.Campus, i.ContentType, i.ContentCategory, i.SubmittedById
            }, transaction);
        await command.ExecuteNonQueryAsync();
        return (int)command.LastInsertedId;
    }

    public static async Task<int> InsertLinkAsync(MySqlConnection connection, MySqlTransaction transaction, IdeaLink l, bool keepId)
    {
        var command = Command(connection, """
            INSERT INTO idea_links (id, idea_id, type, label, url, drive_item_id)
            VALUES (@Id, @IdeaId, @Type, @Label, @Url, @DriveItemId)
            """, new { Id = keepId ? (int?)l.Id : null, l.IdeaId, l.Type, l.Label, l.Url, l.DriveItemId }, transaction);
        await command.ExecuteNonQueryAsync();
        return (int)command.LastInsertedId;
    }

    // The ideas matching `where` (e.g. "WHERE id = @id"), newest first, each with its links.
    private static async Task<List<Idea>> ReadIdeasWithLinksAsync(MySqlConnection connection, string where, object? parameters = null)
    {
        var ideas = new List<Idea>();
        await using (var reader = await Command(connection, $"SELECT * FROM ideas {where} ORDER BY created_at DESC", parameters)
            .ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                ideas.Add(new Idea
                {
                    Id = reader.GetInt32("id"),
                    Name = Text(reader, "name"),
                    Title = Text(reader, "title"),
                    Concept = Text(reader, "concept"),
                    Caption = Text(reader, "caption"),
                    CreatedAt = Utc(reader, "created_at"),
                    ReviewStatus = Text(reader, "review_status") is "" ? Idea.Pending : Text(reader, "review_status"),
                    DenialReason = Text(reader, "denial_reason"),
                    ReviewedAt = OptionalUtc(reader, "reviewed_at"),
                    Campus = Text(reader, "campus"),
                    ContentType = Text(reader, "content_type"),
                    ContentCategory = Text(reader, "content_category"),
                    SubmittedById = OptionalInt(reader, "submitted_by_id")
                });
        }

        var links = (await ReadLinksAsync(connection, "")).ToLookup(l => l.IdeaId);
        ideas.ForEach(i => i.Links = links[i.Id].ToList());
        return ideas;
    }

    private static async Task<List<IdeaLink>> ReadLinksAsync(MySqlConnection connection, string where, object? parameters = null)
    {
        var links = new List<IdeaLink>();
        await using var reader = await Command(connection, $"SELECT * FROM idea_links {where} ORDER BY id", parameters)
            .ExecuteReaderAsync();
        while (await reader.ReadAsync())
            links.Add(new IdeaLink
            {
                Id = reader.GetInt32("id"),
                IdeaId = reader.GetInt32("idea_id"),
                Type = Text(reader, "type"),
                Label = Text(reader, "label"),
                Url = Text(reader, "url"),
                DriveItemId = Text(reader, "drive_item_id")
            });
        return links;
    }
}
