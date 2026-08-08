using System.Text;
using DungeonWorld.API.Auth;
using DungeonWorld.Core.Interfaces;
using DungeonWorld.Core.Options;
using DungeonWorld.Infrastructure.Helpers;
using DungeonWorld.Infrastructure.Parsers;
using DungeonWorld.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuration
builder.Services.Configure<FileStorageOptions>(
    builder.Configuration.GetSection(FileStorageOptions.SectionName));

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.Key))
    throw new InvalidOperationException("The 'Jwt:Key' secret is not configured.");

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

// JWT Bearer authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

// Token issuer for login/register
builder.Services.AddScoped<ITokenIssuer, JwtTokenIssuer>();

// 2. Database (PostgreSQL via EF Core)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "The 'DefaultConnection' connection string is not configured.");

builder.Services.AddPersistence(connectionString);

// 3. Parsing services
builder.Services.AddScoped<IBookParser, SinglePageParser>();
builder.Services.AddScoped<IBookParser, DoublePageParser>();
builder.Services.AddScoped<IBookParser, DungeonWorldBookParser>();
// Register Factory (replaces direct IBookParser injection)
builder.Services.AddScoped<IParserFactory, DungeonWorldParserFactory>();

// Optional: Layout analyzer for pre-checks
builder.Services.AddSingleton<ILayoutAnalyzer, PdfPigLayoutAnalyzer>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS for the Vite dev server (and any future frontend origin)
const string corsPolicy = "FrontendPolicy";
var allowedOrigins = builder.Configuration["Cors:Origins"]
    ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicy, policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// 3. Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 4. Static Files (for Game Art)
// PULL DIRECTLY FROM CONFIGURATION (Which Docker overrides perfectly!)
var storageConfig = app.Configuration.GetSection(FileStorageOptions.SectionName).Get<FileStorageOptions>();

// Fallback just in case, but usually these won't be null
string imagePath = Path.GetFullPath(storageConfig?.ImageOutputPath ?? "Storage/GameArt");
string uploadPath = Path.GetFullPath(storageConfig?.PdfUploadPath ?? "Storage/Uploads");

// Ensure directories exist
Directory.CreateDirectory(imagePath);
Directory.CreateDirectory(uploadPath);

app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(imagePath),
    RequestPath = "/assets/game-art"
});

app.UseCors(corsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 5. Ensure the database schema exists (dev-friendly; swap for EF migrations in production)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DungeonWorldDbContext>();
    db.Database.EnsureCreated();
}

app.Run();