using api.Auth;
using api.Models;
using api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

// Logging in and out, and changing your own password.
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IEmployeeRepository _employees;
    private readonly SessionStore _sessions;

    public AuthController(IEmployeeRepository employees, SessionStore sessions)
    {
        _employees = employees;
        _sessions = sessions;
    }

    // POST /api/auth/login   (JSON body: id, password) -> the token, who you are, and what you may do
    [HttpPost("login")]
    public async Task<ActionResult<LoginDto>> Login(LoginRequest request)
    {
        var employee = (await _employees.GetAllAsync()).FirstOrDefault(e => e.Id == request.Id);
        // The same message whether the ID or the password is wrong, so IDs can't be guessed.
        if (employee == null || !Passwords.Verify(request.Password ?? "", employee.PasswordHash))
            return Unauthorized(new { errors = new[] { "That employee ID and password don't match. Please try again." } });

        return new LoginDto(_sessions.Create(employee.Id), ToMe(employee));
    }

    // GET /api/auth/me   (who is logged in; used when the page is refreshed)
    [HttpGet("me")]
    [RequirePermission]
    public MeDto Me() => ToMe(HttpContext.CurrentEmployee());

    // POST /api/auth/logout
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        var header = Request.Headers.Authorization.ToString();
        if (header.StartsWith("Bearer "))
            _sessions.Remove(header["Bearer ".Length..]);
        return NoContent();
    }

    // POST /api/auth/password   (JSON body: currentPassword, newPassword)
    [HttpPost("password")]
    [RequirePermission]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var me = HttpContext.CurrentEmployee();
        if (!Passwords.Verify(request.CurrentPassword ?? "", me.PasswordHash))
            return BadRequest(new { errors = new[] { "Your current password isn't right." } });
        var newPassword = request.NewPassword ?? "";
        if (newPassword.Length < Passwords.MinLength)
            return BadRequest(new { errors = new[] { $"Your new password needs at least {Passwords.MinLength} characters." } });

        await _employees.SetPasswordHashAsync(me.Id, Passwords.Hash(newPassword));
        return NoContent();
    }

    private static MeDto ToMe(Employee employee) => new(DtoMapping.ToDto(employee), Permissions.For(employee));
}
