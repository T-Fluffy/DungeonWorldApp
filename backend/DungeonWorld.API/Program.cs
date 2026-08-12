using System.Text;
using DungeonWorld.API;
using DungeonWorld.API.Auth;
using DungeonWorld.Core.Interfaces;
using DungeonWorld.Core.Options;
using DungeonWorld.Infrastructure.Interfaces;
using DungeonWorld.Infrastructure.Parsing;
using DungeonWorld.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
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
builder.Services.AddScoped<IPdfTextExtractor, PdfPigTextExtractor>();

// Block-based rule parser pipeline: per-book specializations plus a universal fallback.
builder.Services.AddScoped<IBookParser, SeasOfBloodParser>();
builder.Services.AddScoped<IBookParser, DefaultDungeonWorldParser>();
// The factory also injects the fallback by its concrete type.
builder.Services.AddScoped<DefaultDungeonWorldParser>();
// Factory: specific parser -> default rule-based parser
builder.Services.AddScoped<IParserFactory, DungeonWorldParserFactory>();

// Layout analyzer for pre-check diagnostics
builder.Services.AddSingleton<ILayoutAnalyzer, PdfPigLayoutAnalyzer>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
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

// Global exception handler: log the error, never echo stack traces to clients.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandler = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("GlobalExceptionHandler");
        if (exceptionHandler?.Error != null)
        {
            logger.LogError(exceptionHandler.Error,
                "Unhandled exception processing {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred." });
    });
});

// 4. Static Files (for Game Art)
// PULL DIRECTLY FROM CONFIGURATION (Which Docker overrides perfectly!)
var storageConfig = app.Configuration.GetSection(FileStorageOptions.SectionName).Get<FileStorageOptions>();

// Fallback just in case, but usually these won't be null
string imagePath = Path.GetFullPath(storageConfig?.ImageOutputPath ?? "Storage/GameArt");
string booksPath = Path.GetFullPath(storageConfig?.PdfUploadPath ?? "Storage/Books");
string avatarPath = Path.GetFullPath(storageConfig?.AvatarPath ?? "Storage/Avatars");

// Ensure directories exist
Directory.CreateDirectory(imagePath);
Directory.CreateDirectory(booksPath);
Directory.CreateDirectory(avatarPath);

app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(imagePath),
    RequestPath = "/assets/game-art"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(avatarPath),
    RequestPath = "/assets/avatars"
});

app.UseCors(corsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 5. Apply migrations and seed the database at startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DungeonWorldDbContext>();
    db.Database.Migrate();
    await AdminSeeder.SeedAsync(scope.ServiceProvider);
    await CatalogSeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();