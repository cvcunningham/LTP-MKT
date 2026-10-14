using api.Repositories;
using api.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// The client is opened separately (e.g. Live Server), so allow cross-origin requests.
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddSingleton<IIdeaRepository, CsvIdeaRepository>();
builder.Services.AddSingleton<IEmployeeRepository, CsvEmployeeRepository>();
builder.Services.AddSingleton<IPromoRepository, CsvPromoRepository>();

// Uploaded photos/videos go to the company's SharePoint/OneDrive folder.
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IFileStorage, OneDriveFileStorage>();

var app = builder.Build();

app.UseCors();
app.MapControllers();

app.Run();
