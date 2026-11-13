using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using StackExchange.Redis;
using UrlShortener.WriteApi.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var redisConnection = builder.Configuration.GetConnectionString("Redis");

/// <summary>
/// Registers Redis for the Write API.
/// Unlike the Read API which uses Redis for caching, the Write API uses this connection 
/// strictly for Cache Invalidation (executing KeyDeleteAsync) when links are modified or soft-deleted.
/// </summary>
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnection!));

builder.Services.AddControllers();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    /// <summary>
    /// Configures Scalar UI specifically for the Write API.
    /// Since this service handles complex state mutations (POST, PATCH, DELETE) and validates DTOs,
    /// an interactive testing UI is architecturally necessary here, whereas it is intentionally omitted in the Read API.
    /// </summary>
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("URL Shortener Write API")
               .WithTheme(ScalarTheme.DeepSpace);
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();