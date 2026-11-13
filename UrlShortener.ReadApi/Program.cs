using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
var redisConnection = builder.Configuration.GetConnectionString("Redis");

/// <summary>
/// Registers the Redis multiplexer as a Singleton.
/// ConnectionMultiplexer is thread-safe and architecturally designed to be shared across the entire application lifespan,
/// preventing socket exhaustion caused by opening/closing connections per request.
/// </summary>
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnection!));

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();