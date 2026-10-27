using api.Auth;
using api.Models;
using api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

// The Promo Calendar page: the Promo List (also shown on the promo calendar) and the outreach scripts.
// Semesters 3-4 can view; 5-6 can also add and edit; only 7-8 can delete (Auth/Permissions.cs).
[ApiController]
[Route("api/promos")]
public class PromosController : ControllerBase
{
    private const int MaxScriptLength = 4000;

    private readonly IPromoRepository _repo;

    public PromosController(IPromoRepository repo)
    {
        _repo = repo;
    }

    // GET /api/promos   (every promo, soonest first; past promos stay until someone deletes them)
    [HttpGet]
    [RequirePermission(Permissions.ViewPromos)]
    public async Task<List<PromoDto>> GetAll() =>
        (await _repo.GetAllAsync()).Select(DtoMapping.ToDto).ToList();

    // GET /api/promos/options   (the Partner type and Status choices, in order, for the dropdowns)
    [HttpGet("options")]
    [RequirePermission(Permissions.ViewPromos)]
    public object GetOptions() => new { partnerTypes = Promo.PartnerTypes, statuses = Promo.Statuses };

    // POST /api/promos   (JSON body: name, partnerType, campus, date, promoCode, status, contactName, contactInfo)
    [HttpPost]
    [RequirePermission(Permissions.EditPromos)]
    public async Task<ActionResult<PromoDto>> Create(PromoRequest request)
    {
        var (promo, errors) = Validate(request);
        if (errors.Count > 0)
            return BadRequest(new { errors });
        return Ok(DtoMapping.ToDto(await _repo.AddAsync(promo)));
    }

    // PUT /api/promos/3   (same body as POST)
    [HttpPut("{id}")]
    [RequirePermission(Permissions.EditPromos)]
    public async Task<IActionResult> Update(int id, PromoRequest request)
    {
        var (promo, errors) = Validate(request);
        if (errors.Count > 0)
            return BadRequest(new { errors });
        promo.Id = id;
        return await _repo.UpdateAsync(promo) ? NoContent() : NotFound();
    }

    // DELETE /api/promos/3   (permanent)
    [HttpDelete("{id}")]
    [RequirePermission(Permissions.DeletePromos)]
    public async Task<IActionResult> Delete(int id) =>
        await _repo.DeleteAsync(id) ? NoContent() : NotFound();

    // GET /api/promos/scripts   (one outreach script per partner type)
    [HttpGet("scripts")]
    [RequirePermission(Permissions.ViewPromos)]
    public async Task<List<PromoScriptDto>> GetScripts() =>
        (await _repo.GetScriptsAsync()).Select(DtoMapping.ToDto).ToList();

    // PUT /api/promos/scripts/1   (JSON body: message, forwardMessage)
    [HttpPut("scripts/{id}")]
    [RequirePermission(Permissions.EditPromos)]
    public async Task<IActionResult> UpdateScript(int id, ScriptUpdateRequest request)
    {
        var message = request.Message?.Trim() ?? "";
        var forwardMessage = request.ForwardMessage?.Trim() ?? "";
        if (message == "")
            return BadRequest(new { errors = new[] { "The message to the partner can't be empty." } });
        if (message.Length > MaxScriptLength || forwardMessage.Length > MaxScriptLength)
            return BadRequest(new { errors = new[] { $"Each message can be up to {MaxScriptLength} characters." } });

        var script = new PromoScript { Id = id, Message = message, ForwardMessage = forwardMessage };
        return await _repo.UpdateScriptAsync(script) ? NoContent() : NotFound();
    }

    // Checks a promo from the form. Name, partner type, campus, date and status are required.
    private static (Promo Promo, List<string> Errors) Validate(PromoRequest request)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add("Please enter the partner's name.");
        if (!Promo.PartnerTypes.Contains(request.PartnerType))
            errors.Add("Please choose a partner type: " + string.Join(", ", Promo.PartnerTypes) + ".");
        if (!Promo.Campuses.Contains(request.Campus))
            errors.Add("Please choose a campus: " + string.Join(", ", Promo.Campuses) + ".");
        if (request.Date == null)
            errors.Add("Please choose the date of the promo.");
        if (!Promo.Statuses.Contains(request.Status))
            errors.Add("Please choose a status from the list.");

        var promo = new Promo
        {
            Name = request.Name?.Trim() ?? "",
            PartnerType = request.PartnerType ?? "",
            Campus = request.Campus ?? "",
            Date = request.Date ?? default,
            PromoCode = request.PromoCode?.Trim() ?? "",
            Status = request.Status ?? "",
            ContactName = request.ContactName?.Trim() ?? "",
            ContactInfo = request.ContactInfo?.Trim() ?? ""
        };
        return (promo, errors);
    }
}
