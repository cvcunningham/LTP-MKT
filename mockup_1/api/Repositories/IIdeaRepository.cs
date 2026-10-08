using api.Models;

namespace api.Repositories;

// The controller only talks to this interface. To move to MySQL, write a
// MySqlIdeaRepository that implements it and change one line in Program.cs.
public interface IIdeaRepository
{
    Task<List<Idea>> GetAllAsync(); // every idea with its links
    Task<Idea?> GetByIdAsync(int id);
    Task<Idea> AddAsync(Idea idea, List<IdeaLink> links);

    // Only changes an idea that is still Pending; returns false otherwise.
    // In MySQL: UPDATE ideas SET ... WHERE id = @id AND review_status = 'Pending'
    Task<bool> SetReviewAsync(int id, string reviewStatus, string denialReason);

    // Permanently removes the idea and all its links; false if the idea doesn't exist.
    // In MySQL: DELETE FROM idea_links WHERE idea_id = @id; DELETE FROM ideas WHERE id = @id
    Task<bool> DeleteAsync(int id);
}
