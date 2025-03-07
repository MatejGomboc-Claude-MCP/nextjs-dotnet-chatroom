using ChatRoom.Api.Data;
using ChatRoom.Api.Hubs;
using ChatRoom.Api.Middleware;
using ChatRoom.Api.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add SignalR
builder.Services.AddSignalR();

// Add Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        // Rate limit based on IP address
        return RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,         // 100 requests
                Window = TimeSpan.FromMinutes(1)   // per minute
            });
    });

    // Add specific rate limiters for endpoints that might be abused
    options.AddPolicy("message_creation", context =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 30,         // 30 message creations
                Window = TimeSpan.FromMinutes(1)   // per minute
            });
    });

    // Configure rate limit response
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        
        await context.HttpContext.Response.WriteAsync(
            """{"error":"Too many requests. Please try again later."}""", 
            cancellationToken: token);
    };
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", corsBuilder =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
            ?? new[] { "http://localhost:3000" };
        
        corsBuilder
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Add database context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Add services
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IReactionService, ReactionService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database")
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else 
{
    app.UseHttpsRedirection();
}

// Use rate limiting
app.UseRateLimiter();

// Use custom middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors("CorsPolicy");
app.UseAuthorization();

// Add health check endpoint
app.MapHealthChecks("/health");

app.MapControllers();
app.MapHub<ChatHub>("/chatHub");

// Apply migrations instead of EnsureCreated for better production support
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // In development, we can create migrations if they don't exist
        if (app.Environment.IsDevelopment())
        {
            // Migrate the database
            context.Database.Migrate();
        }
        else
        {
            // In production, just apply existing migrations
            context.Database.Migrate();
        }
        
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

// Clean up expired connections in SignalR on a regular basis
var timer = new Timer(async _ =>
{
    try
    {
        using var scope = app.Services.CreateScope();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();
        await ChatHub.CleanupExpiredConnections(hubContext);
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error cleaning up expired SignalR connections");
    }
}, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

app.Run();
