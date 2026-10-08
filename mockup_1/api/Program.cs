using api.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// The client is opened separately (e.g. Live Server), so allow cross-origin requests.
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddSingleton<IIdeaRepository, CsvIdeaRepository>();

var app = builder.Build();

app.UseCors();
app.MapControllers();

app.Run();
