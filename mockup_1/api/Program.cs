using api.Auth;
using api.Repositories;
using api.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// The client is opened separately (e.g. Live Server), so allow cross-origin requests.
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// Where the data is kept: "Csv" (the files in Data/, the default in appsettings.json) or "MySql"
// (the database). To use MySQL on your computer, set "Database:Provider" to "MySql" in your own
// dotnet user-secrets (see README). The controllers work the same with either.
var useMySql = builder.Configuration["Database:Provider"] == "MySql";
if (useMySql)
{
    builder.Services.AddSingleton<MySqlDatabase>();
    builder.Services.AddSingleton<IIdeaRepository, MySqlIdeaRepository>();
    builder.Services.AddSingleton<IEmployeeRepository, MySqlEmployeeRepository>();
    builder.Services.AddSingleton<IPromoRepository, MySqlPromoRepository>();
}
else
{
    builder.Services.AddSingleton<IIdeaRepository, CsvIdeaRepository>();
    builder.Services.AddSingleton<IEmployeeRepository, CsvEmployeeRepository>();
    builder.Services.AddSingleton<IPromoRepository, CsvPromoRepository>();
}

// Who is logged in (in memory; restarting the API logs everyone out).
builder.Services.AddSingleton<SessionStore>();

// Uploaded photos/videos go to the company's SharePoint/OneDrive folder.
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IFileStorage, OneDriveFileStorage>();

var app = builder.Build();

app.Logger.LogInformation(useMySql ? "Data: MySQL database" : "Data: CSV files in Data/");

// Creates the MySQL database and tables if they're missing (and copies the CSV files in the first time).
if (useMySql)
    await app.Services.GetRequiredService<MySqlDatabase>().SetupAsync(app.Environment, app.Logger);

app.UseCors();
app.MapControllers();

app.Run();
