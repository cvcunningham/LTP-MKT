using api.Models;

namespace api.Repositories;

// Promos (the Promo List / promo calendar) and the outreach scripts.
// To move to MySQL, write a MySqlPromoRepository and change one line in Program.cs.
public interface IPromoRepository
{
    Task<List<Promo>> GetAllAsync(); // sorted by date, soonest first
    Task<Promo> AddAsync(Promo promo);

    // Saves every field except Id and CreatedAt of the promo with promo.Id. False if it doesn't exist.
    Task<bool> UpdateAsync(Promo promo);

    // Permanently removes the promo. False if it doesn't exist.
    Task<bool> DeleteAsync(int id);

    Task<List<PromoScript>> GetScriptsAsync(); // one per partner type

    // Saves the Message and ForwardMessage of the script with script.Id. False if it doesn't exist.
    Task<bool> UpdateScriptAsync(PromoScript script);
}
