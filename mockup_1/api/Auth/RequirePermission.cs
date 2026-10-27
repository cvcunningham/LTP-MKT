using api.Models;
using api.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace api.Auth;

// Put on a controller action: [RequirePermission(Permissions.EditPromos)]
// The request must come from someone logged in (401 otherwise) who has at least one of the listed
// permissions (403 otherwise). With no permissions listed, being logged in is enough.
// The employee is read fresh from employees.csv each time, so a change of semester applies at once.
// The action can get the logged-in employee with HttpContext.CurrentEmployee().
public class RequirePermissionAttribute : TypeFilterAttribute
{
    public RequirePermissionAttribute(params string[] anyOf) : base(typeof(PermissionFilter))
    {
        Arguments = new object[] { anyOf };
    }

    private class PermissionFilter : IAsyncActionFilter
    {
        private readonly string[] _anyOf;
        private readonly SessionStore _sessions;
        private readonly IEmployeeRepository _employees;

        public PermissionFilter(string[] anyOf, SessionStore sessions, IEmployeeRepository employees)
        {
            _anyOf = anyOf;
            _sessions = sessions;
            _employees = employees;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var employee = await LoggedInEmployee(context.HttpContext, _sessions, _employees);
            if (employee == null)
            {
                context.Result = new UnauthorizedObjectResult(new { errors = new[] { "Please log in again." } });
                return;
            }
            if (_anyOf.Length > 0 && !_anyOf.Any(p => Permissions.Has(employee, p)))
            {
                context.Result = new ObjectResult(new { errors = new[] { "You don't have access to do that." } })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }
            context.HttpContext.Items[typeof(Employee)] = employee;
            await next();
        }
    }

    // The employee whose token came with the request, or null (no token, unknown, expired, or deleted).
    public static async Task<Employee?> LoggedInEmployee(HttpContext http, SessionStore sessions, IEmployeeRepository employees)
    {
        var header = http.Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer "))
            return null;
        if (sessions.GetEmployeeId(header["Bearer ".Length..]) is not int id)
            return null;
        return (await employees.GetAllAsync()).FirstOrDefault(e => e.Id == id);
    }
}

public static class HttpContextExtensions
{
    // The logged-in employee (only inside an action with [RequirePermission]).
    public static Employee CurrentEmployee(this HttpContext http) => (Employee)http.Items[typeof(Employee)]!;
}
