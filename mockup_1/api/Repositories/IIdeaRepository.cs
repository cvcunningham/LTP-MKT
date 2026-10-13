using api.Models;

namespace api.Repositories;

// The controllers only talk to this interface. To move to MySQL, write a
// MySqlIdeaRepository that implements it and change one line in Program.cs.
public interface IIdeaRepository
{
    Task<List<Idea>> GetAllAsync(); // every idea with its links
    Task<Idea?> GetByIdAsync(int id);
    Task<IdeaLink?> GetLinkAsync(int linkId);
    Task<Idea> AddAsync(Idea idea, List<IdeaLink> links);

    // Both only change an idea that is still Pending; they return false otherwise.
    // In MySQL: UPDATE ideas SET ... WHERE id = @id AND review_status = 'Pending'
    // Approving also creates the idea's Production Tracker item (in MySQL: one transaction).
    Task<bool> ApproveAsync(int id);
    Task<bool> RejectAsync(int id);

    // Permanently removes Rejected ideas decided before the cutoff, with their links.
    // Returns how many were removed.
    Task<int> DeleteRejectedBeforeAsync(DateTime cutoff);

    // Permanently removes the idea, all its links and its Production Tracker item;
    // false if the idea doesn't exist. Uploaded files are kept in the OneDrive folder.
    // In MySQL: ON DELETE CASCADE on idea_links and production_items does this.
    Task<bool> DeleteAsync(int id);

    // Production Tracker items, each with its idea (and the idea's links) filled in.
    Task<List<ProductionItem>> GetProductionAsync();

    // Saves the production details (Filmer, Editor, Reviewer, Poster, TargetPostDate, Status)
    // of the item with item.Id. False if it doesn't exist.
    Task<bool> UpdateProductionAsync(ProductionItem item);

    // Permanently removes the item together with its idea and links. False if it doesn't exist.
    Task<bool> DeleteProductionAsync(int id);
}
