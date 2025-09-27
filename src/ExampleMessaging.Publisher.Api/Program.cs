using ExampleMessaging.Shared.Logging;
using ExampleMessaging.Publisher.Api.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog logging
builder.Host.ConfigureSerilog("ExampleMessaging.Publisher.Api");

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add correlation context for request tracking
// builder.Services.AddCorrelationLogging(); // Temporarily disabled due to DI scoping issues

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
// app.UseMiddleware<CorrelationIdMiddleware>(); // Temporarily disabled due to DI scoping issues
// app.UseMiddleware<GlobalExceptionHandlingMiddleware>(); // Temporarily disabled due to DI scoping issues

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();

// Add health check endpoint
app.MapHealthChecks("/health");

app.MapControllers();

try
{
    Log.Information("Starting ExampleMessaging Publisher API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible for testing
public partial class Program { }
