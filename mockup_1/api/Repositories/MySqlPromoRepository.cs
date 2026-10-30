using api.Models;
using static api.Repositories.MySqlDatabase;

namespace api.Repositories;

// Promos (`promos` table) and the outreach scripts (`promo_scripts`).
// Does the same as CsvPromoRepository (see IPromoRepository).
public class MySqlPromoRepository : IPromoRepository
{
    private readonly MySqlDatabase _db;

    public MySqlPromoRepository(MySqlDatabase db) => _db = db;

    public async Task<List<Promo>> GetAllAsync()
    {
        await using var connection = await _db.OpenAsync();
        var promos = new List<Promo>();
        await using var reader = await Command(connection, "SELECT * FROM promos").ExecuteReaderAsync();
        while (await reader.ReadAsync())
            promos.Add(new Promo
            {
                Id = reader.GetInt32("id"),
                Name = Text(reader, "name"),
                PartnerType = Text(reader, "partner_type"),
                Campus = Text(reader, "campus"),
                Date = DateOnly.FromDateTime(reader.GetDateTime("date")),
                PromoCode = Text(reader, "promo_code"),
                Status = Promo.UpgradeStatus(Text(reader, "status")),
                ContactName = Text(reader, "contact_name"),
                ContactInfo = Text(reader, "contact_info"),
                CreatedAt = Utc(reader, "created_at"),
                UpdatedAt = Utc(reader, "updated_at")
            });
        return promos.OrderBy(p => p.Date).ThenBy(p => p.Name).ToList(); // sorted the same way as before
    }

    public async Task<Promo> AddAsync(Promo promo)
    {
        promo.CreatedAt = promo.UpdatedAt = DateTime.UtcNow;
        await using var connection = await _db.OpenAsync();
        var command = Command(connection, """
            INSERT INTO promos (name, partner_type, campus, date, promo_code, status,
                contact_name, contact_info, created_at, updated_at)
            VALUES (@Name, @PartnerType, @Campus, @Date, @PromoCode, @Status,
                @ContactName, @ContactInfo, @CreatedAt, @UpdatedAt)
            """, new
            {
                promo.Name, promo.PartnerType, promo.Campus, promo.Date, promo.PromoCode, promo.Status,
                promo.ContactName, promo.ContactInfo, promo.CreatedAt, promo.UpdatedAt
            });
        await command.ExecuteNonQueryAsync();
        promo.Id = (int)command.LastInsertedId; // MySQL picks the new id (AUTO_INCREMENT)
        return promo;
    }

    public async Task<bool> UpdateAsync(Promo promo)
    {
        await using var connection = await _db.OpenAsync();
        return await Command(connection, """
            UPDATE promos
            SET name = @Name, partner_type = @PartnerType, campus = @Campus, date = @Date, promo_code = @PromoCode,
                status = @Status, contact_name = @ContactName, contact_info = @ContactInfo, updated_at = @UpdatedAt
            WHERE id = @Id
            """, new
            {
                promo.Name, promo.PartnerType, promo.Campus, promo.Date, promo.PromoCode,
                promo.Status, promo.ContactName, promo.ContactInfo, UpdatedAt = DateTime.UtcNow, promo.Id
            }).ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var connection = await _db.OpenAsync();
        return await Command(connection, "DELETE FROM promos WHERE id = @id", new { id }).ExecuteNonQueryAsync() > 0;
    }

    public async Task<List<PromoScript>> GetScriptsAsync()
    {
        await using var connection = await _db.OpenAsync();
        var scripts = new List<PromoScript>();
        await using var reader = await Command(connection, "SELECT * FROM promo_scripts ORDER BY id").ExecuteReaderAsync();
        while (await reader.ReadAsync())
            scripts.Add(new PromoScript
            {
                Id = reader.GetInt32("id"),
                PartnerType = Text(reader, "partner_type"),
                Message = Text(reader, "message"),
                ForwardMessage = Text(reader, "forward_message"),
                UpdatedAt = OptionalUtc(reader, "updated_at")
            });
        return scripts;
    }

    public async Task<bool> UpdateScriptAsync(PromoScript script)
    {
        await using var connection = await _db.OpenAsync();
        return await Command(connection,
            "UPDATE promo_scripts SET message = @Message, forward_message = @ForwardMessage, updated_at = @UpdatedAt WHERE id = @Id",
            new { script.Message, script.ForwardMessage, UpdatedAt = DateTime.UtcNow, script.Id }).ExecuteNonQueryAsync() > 0;
    }
}
