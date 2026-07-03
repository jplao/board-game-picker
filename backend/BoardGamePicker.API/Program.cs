using BoardGamePicker.API.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(GetConnectionString()));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Use raw SQL so this works regardless of migration history state.
    // IF NOT EXISTS makes it safe to run on every startup.
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""BoardGames"" (
            ""Id""          SERIAL PRIMARY KEY,
            ""Name""        VARCHAR(200) NOT NULL,
            ""MinPlayers""  INTEGER NOT NULL,
            ""MaxPlayers""  INTEGER NOT NULL,
            ""MinRuntime""  INTEGER NOT NULL,
            ""MaxRuntime""  INTEGER NOT NULL,
            ""MinAge""      INTEGER NOT NULL,
            ""ImageUrl""    TEXT,
            ""Description"" TEXT NOT NULL,
            ""Type""        VARCHAR(100) NOT NULL,
            ""Category""    VARCHAR(100) NOT NULL,
            ""BggRank""     INTEGER NOT NULL,
            ""IsOwned""     BOOLEAN NOT NULL DEFAULT TRUE
        )
    ");

    await SeedData.SeedAsync(db);
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.MapControllers();
app.Run();

static string GetConnectionString()
{
    // Option 1: DATABASE_URL in postgresql:// URI format (Railway, Heroku, Render)
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (!string.IsNullOrEmpty(databaseUrl))
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');
        return $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={Uri.UnescapeDataString(userInfo[1])};SSL Mode=Require;Trust Server Certificate=true";
    }

    // Option 2: Individual PG* variables (also provided by Railway's Postgres plugin)
    var pgHost = Environment.GetEnvironmentVariable("PGHOST");
    if (!string.IsNullOrEmpty(pgHost))
    {
        var pgPort = Environment.GetEnvironmentVariable("PGPORT") ?? "5432";
        var pgDb = Environment.GetEnvironmentVariable("PGDATABASE") ?? "railway";
        var pgUser = Environment.GetEnvironmentVariable("PGUSER") ?? "postgres";
        var pgPass = Environment.GetEnvironmentVariable("PGPASSWORD") ?? "";
        return $"Host={pgHost};Port={pgPort};Database={pgDb};Username={pgUser};Password={pgPass};SSL Mode=Require;Trust Server Certificate=true";
    }

    // Local development fallback
    return "Host=localhost;Database=boardgamepicker;Username=postgres;Password=postgres";
}
