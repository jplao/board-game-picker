using System.Text;
using BoardGamePicker.API.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(GetConnectionString()));

// JWT auth
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? builder.Configuration["JWT_SECRET"]
    ?? "dev-secret-change-in-production-min-32-chars!!";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
        };
    });

builder.Services.AddAuthorization();

// Make JWT_SECRET available via IConfiguration for AuthController
builder.Configuration["JWT_SECRET"] = jwtSecret;

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

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""Users"" (
            ""Id""           SERIAL PRIMARY KEY,
            ""Email""        VARCHAR(256) NOT NULL UNIQUE,
            ""PasswordHash"" TEXT NOT NULL,
            ""Role""         VARCHAR(20) NOT NULL DEFAULT 'user'
        )
    ");

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
            ""IsOwned""     BOOLEAN NOT NULL DEFAULT TRUE,
            ""UserId""      INTEGER REFERENCES ""Users""(""Id"") ON DELETE SET NULL
        )
    ");

    // Add UserId column to existing deployments that pre-date this migration
    db.Database.ExecuteSqlRaw(@"
        ALTER TABLE ""BoardGames""
        ADD COLUMN IF NOT EXISTS ""UserId"" INTEGER REFERENCES ""Users""(""Id"") ON DELETE SET NULL
    ");

    await SeedData.SeedAsync(db);
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

static string GetConnectionString()
{
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (!string.IsNullOrEmpty(databaseUrl))
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');
        return $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={Uri.UnescapeDataString(userInfo[1])};SSL Mode=Require;Trust Server Certificate=true";
    }

    var pgHost = Environment.GetEnvironmentVariable("PGHOST");
    if (!string.IsNullOrEmpty(pgHost))
    {
        var pgPort = Environment.GetEnvironmentVariable("PGPORT") ?? "5432";
        var pgDb = Environment.GetEnvironmentVariable("PGDATABASE") ?? "railway";
        var pgUser = Environment.GetEnvironmentVariable("PGUSER") ?? "postgres";
        var pgPass = Environment.GetEnvironmentVariable("PGPASSWORD") ?? "";
        return $"Host={pgHost};Port={pgPort};Database={pgDb};Username={pgUser};Password={pgPass};SSL Mode=Require;Trust Server Certificate=true";
    }

    return "Host=localhost;Database=boardgamepicker;Username=postgres;Password=postgres";
}
