using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace api.Auth;

// Who is logged in. Logging in creates a random token; the page keeps it for as long as the browser
// tab is open and sends it with every request ("Authorization: Bearer <token>").
// Kept in memory, so restarting the API logs everyone out. Tokens also expire after SessionLength.
public class SessionStore
{
    public static readonly TimeSpan SessionLength = TimeSpan.FromHours(12);

    private readonly ConcurrentDictionary<string, Session> _sessions = new();

    private record Session(int EmployeeId, DateTime ExpiresAt);

    public string Create(int employeeId)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        _sessions[token] = new Session(employeeId, DateTime.UtcNow + SessionLength);
        return token;
    }

    // The logged-in employee's id, or null if the token is unknown or expired.
    public int? GetEmployeeId(string token)
    {
        if (!_sessions.TryGetValue(token, out var session))
            return null;
        if (session.ExpiresAt < DateTime.UtcNow)
        {
            _sessions.TryRemove(token, out _);
            return null;
        }
        return session.EmployeeId;
    }

    public void Remove(string token) => _sessions.TryRemove(token, out _);

    // When an employee's id is changed in the Intern Directory, they stay logged in.
    public void ReplaceEmployeeId(int oldId, int newId)
    {
        foreach (var (token, session) in _sessions)
            if (session.EmployeeId == oldId)
                _sessions[token] = session with { EmployeeId = newId };
    }

    // Logs the employee out everywhere (e.g. when they are deleted).
    public void RemoveEmployee(int employeeId)
    {
        foreach (var (token, session) in _sessions)
            if (session.EmployeeId == employeeId)
                _sessions.TryRemove(token, out _);
    }
}
