using System.Globalization;
using api.Models;

namespace api.Repositories;

public class CsvPromoRepository : IPromoRepository
{
    // CsvFile.EnsureSchema upgrades older files to these headers (matching columns by name).
    private const string PromosHeader =
        "Id,Name,PartnerType,Campus,Date,PromoCode,Status,ContactName,ContactInfo,CreatedAt,UpdatedAt";
    private const string ScriptsHeader = "Id,PartnerType,Message,ForwardMessage,UpdatedAt";

    private readonly string _promosPath;
    private readonly string _scriptsPath;
    private readonly SemaphoreSlim _lock = new(1, 1); // one read/write at a time

    public CsvPromoRepository(IWebHostEnvironment env)
    {
        _promosPath = Path.Combine(env.ContentRootPath, "Data", "promos.csv");
        _scriptsPath = Path.Combine(env.ContentRootPath, "Data", "scripts.csv");
        CsvFile.EnsureSchema(_promosPath, PromosHeader);
        CsvFile.EnsureSchema(_scriptsPath, ScriptsHeader);
    }

    public async Task<List<Promo>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try { return ReadPromos().OrderBy(p => p.Date).ThenBy(p => p.Name).ToList(); }
        finally { _lock.Release(); }
    }

    public async Task<Promo> AddAsync(Promo promo)
    {
        await _lock.WaitAsync();
        try
        {
            // Next Id = highest Id + 1 (MySQL's AUTO_INCREMENT will do this for us later).
            promo.Id = ReadPromos().Select(p => p.Id).DefaultIfEmpty(0).Max() + 1;
            promo.CreatedAt = promo.UpdatedAt = DateTime.UtcNow;
            CsvFile.AppendRow(_promosPath, ToRow(promo));
            return promo;
        }
        finally { _lock.Release(); }
    }

    public async Task<bool> UpdateAsync(Promo changes)
    {
        await _lock.WaitAsync();
        try
        {
            var promos = ReadPromos();
            var promo = promos.FirstOrDefault(p => p.Id == changes.Id);
            if (promo == null)
                return false;

            promo.Name = changes.Name;
            promo.PartnerType = changes.PartnerType;
            promo.Campus = changes.Campus;
            promo.Date = changes.Date;
            promo.PromoCode = changes.PromoCode;
            promo.Status = changes.Status;
            promo.ContactName = changes.ContactName;
            promo.ContactInfo = changes.ContactInfo;
            promo.UpdatedAt = DateTime.UtcNow;

            CsvFile.WriteAll(_promosPath, PromosHeader, promos.Select(ToRow));
            return true;
        }
        finally { _lock.Release(); }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await _lock.WaitAsync();
        try
        {
            var promos = ReadPromos();
            if (promos.RemoveAll(p => p.Id == id) == 0)
                return false;
            CsvFile.WriteAll(_promosPath, PromosHeader, promos.Select(ToRow));
            return true;
        }
        finally { _lock.Release(); }
    }

    public async Task<List<PromoScript>> GetScriptsAsync()
    {
        await _lock.WaitAsync();
        try { return ReadScripts(); }
        finally { _lock.Release(); }
    }

    public async Task<bool> UpdateScriptAsync(PromoScript changes)
    {
        await _lock.WaitAsync();
        try
        {
            var scripts = ReadScripts();
            var script = scripts.FirstOrDefault(s => s.Id == changes.Id);
            if (script == null)
                return false;

            script.Message = changes.Message;
            script.ForwardMessage = changes.ForwardMessage;
            script.UpdatedAt = DateTime.UtcNow;

            CsvFile.WriteAll(_scriptsPath, ScriptsHeader, scripts.Select(ToRow));
            return true;
        }
        finally { _lock.Release(); }
    }

    private static string[] ToRow(Promo p) => new[]
    {
        p.Id.ToString(), p.Name, p.PartnerType, p.Campus, p.Date.ToString("yyyy-MM-dd"), p.PromoCode,
        p.Status, p.ContactName, p.ContactInfo, p.CreatedAt.ToString("o"), p.UpdatedAt.ToString("o")
    };

    private static string[] ToRow(PromoScript s) => new[]
    {
        s.Id.ToString(), s.PartnerType, s.Message, s.ForwardMessage, s.UpdatedAt?.ToString("o") ?? ""
    };

    private List<Promo> ReadPromos() =>
        CsvFile.ReadRows(_promosPath).Select(r => new Promo
        {
            Id = int.Parse(r[0]),
            Name = r[1],
            PartnerType = r[2],
            Campus = r[3],
            Date = DateOnly.ParseExact(r[4], "yyyy-MM-dd", CultureInfo.InvariantCulture),
            PromoCode = r[5],
            Status = Promo.UpgradeStatus(r[6]),
            ContactName = r[7],
            ContactInfo = r[8],
            CreatedAt = ParseDate(r[9]),
            UpdatedAt = ParseDate(r[10])
        }).ToList();

    private List<PromoScript> ReadScripts() =>
        CsvFile.ReadRows(_scriptsPath).Select(r => new PromoScript
        {
            Id = int.Parse(r[0]),
            PartnerType = r[1],
            Message = r[2],
            ForwardMessage = r[3],
            UpdatedAt = r[4] == "" ? null : ParseDate(r[4])
        }).ToList();

    private static DateTime ParseDate(string value) =>
        DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}
