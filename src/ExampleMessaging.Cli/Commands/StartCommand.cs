using System.CommandLine;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ExampleMessaging.Cli.Commands;

public class StartCommand : Command
{
    public StartCommand() : base("start", "Start messaging environment (RabbitMQ, MQTT, services)")
    {
        var servicesOption = new Option<string[]>(
            name: "--services", 
            description: "Services to start (rabbitmq, mqtt, api, amqp-consumer, mqtt-consumer, all)",
            getDefaultValue: () => new[] { "all" })
        {
            AllowMultipleArgumentsPerToken = true
        };
        
        var detachedOption = new Option<bool>(
            name: "--detached", 
            description: "Run services in detached mode (background)",
            getDefaultValue: () => true);

        var timeoutOption = new Option<int>(
            name: "--timeout",
            description: "Timeout in seconds to wait for services to be ready",
            getDefaultValue: () => 30);

        AddOption(servicesOption);
        AddOption(detachedOption);
        AddOption(timeoutOption);

        this.SetHandler(async (services, detached, timeout, logger) =>
        {
            await ExecuteAsync(services, detached, timeout, logger);
        }, servicesOption, detachedOption, timeoutOption, new LoggerBinder());
    }

    private async Task ExecuteAsync(string[] services, bool detached, int timeout, ILogger logger)
    {
        logger.LogInformation("Starting messaging environment...");
        logger.LogInformation("Services: {Services}", string.Join(", ", services));
        logger.LogInformation("Detached mode: {Detached}", detached);

        var servicesToStart = services.Contains("all") 
            ? new[] { "rabbitmq", "mqtt", "api", "amqp-consumer", "mqtt-consumer" }
            : services;

        try
        {
            // Check if Docker is available
            await CheckDockerAvailabilityAsync(logger);

            // Start infrastructure services first
            if (servicesToStart.Contains("rabbitmq") || servicesToStart.Contains("mqtt"))
            {
                await StartInfrastructureAsync(servicesToStart, detached, logger);
                await WaitForInfrastructureReadyAsync(servicesToStart, timeout, logger);
            }

            // Start application services
            if (servicesToStart.Any(s => new[] { "api", "amqp-consumer", "mqtt-consumer" }.Contains(s)))
            {
                await StartApplicationServicesAsync(servicesToStart, detached, logger);
                await WaitForApplicationServicesReadyAsync(servicesToStart, timeout, logger);
            }

            logger.LogInformation("✅ Messaging environment started successfully!");
            
            if (detached)
            {
                logger.LogInformation("Services are running in background. Use 'status' command to check health.");
                logger.LogInformation("Use 'docker-compose logs -f' to view logs.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Failed to start messaging environment: {Error}", ex.Message);
            Environment.ExitCode = 1;
        }
    }

    private async Task CheckDockerAvailabilityAsync(ILogger logger)
    {
        logger.LogInformation("Checking Docker availability...");
        
        var dockerCheck = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = "--version",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(dockerCheck);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start Docker process");
        }

        await process.WaitForExitAsync();
        
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException("Docker is not available. Please install Docker and ensure it's running.");
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        logger.LogDebug("Docker version: {DockerVersion}", output.Trim());
    }

    private async Task StartInfrastructureAsync(string[] services, bool detached, ILogger logger)
    {
        logger.LogInformation("Starting infrastructure services...");

        var dockerComposePath = Path.Combine(Directory.GetCurrentDirectory(), "docker", "docker-compose.yml");
        if (!File.Exists(dockerComposePath))
        {
            throw new FileNotFoundException($"Docker Compose file not found at: {dockerComposePath}");
        }

        var infraServices = new List<string>();
        if (services.Contains("rabbitmq")) infraServices.Add("rabbitmq");
        if (services.Contains("mqtt")) infraServices.Add("mosquitto");

        if (infraServices.Any())
        {
            var arguments = $"up {(detached ? "-d" : "")} {string.Join(" ", infraServices)}";
            
            var processInfo = new ProcessStartInfo
            {
                FileName = "docker-compose",
                Arguments = arguments,
                WorkingDirectory = Path.GetDirectoryName(dockerComposePath),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            logger.LogDebug("Executing: docker-compose {Arguments}", arguments);

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start docker-compose process");
            }

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new InvalidOperationException($"Failed to start infrastructure services: {error}");
            }

            logger.LogInformation("Infrastructure services started: {Services}", string.Join(", ", infraServices));
        }
    }

    private async Task WaitForInfrastructureReadyAsync(string[] services, int timeoutSeconds, ILogger logger)
    {
        logger.LogInformation("Waiting for infrastructure services to be ready...");

        var tasks = new List<Task>();

        if (services.Contains("rabbitmq"))
        {
            tasks.Add(WaitForServiceReadyAsync("RabbitMQ", "localhost", 5672, timeoutSeconds, logger));
            tasks.Add(WaitForServiceReadyAsync("RabbitMQ Management", "localhost", 15672, timeoutSeconds, logger));
        }

        if (services.Contains("mqtt"))
        {
            tasks.Add(WaitForServiceReadyAsync("MQTT", "localhost", 1883, timeoutSeconds, logger));
        }

        if (tasks.Any())
        {
            await Task.WhenAll(tasks);
            logger.LogInformation("✅ All infrastructure services are ready!");
        }
    }

    private async Task StartApplicationServicesAsync(string[] services, bool detached, ILogger logger)
    {
        logger.LogInformation("Starting application services...");

        var tasks = new List<Task>();

        if (services.Contains("api"))
        {
            tasks.Add(StartDotNetServiceAsync("Publisher API", "src/ExampleMessaging.Publisher.Api", detached, logger));
        }

        if (services.Contains("amqp-consumer"))
        {
            tasks.Add(StartDotNetServiceAsync("AMQP Consumer", "src/ExampleMessaging.Amqp.Consumer", detached, logger));
        }

        if (services.Contains("mqtt-consumer"))
        {
            tasks.Add(StartDotNetServiceAsync("MQTT Consumer", "src/ExampleMessaging.Mqtt.Consumer", detached, logger));
        }

        if (tasks.Any())
        {
            await Task.WhenAll(tasks);
        }
    }

    private async Task StartDotNetServiceAsync(string serviceName, string projectPath, bool detached, ILogger logger)
    {
        logger.LogInformation("Starting {ServiceName}...", serviceName);

        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), projectPath);
        if (!Directory.Exists(fullPath))
        {
            logger.LogWarning("Service directory not found: {Path}", fullPath);
            return;
        }

        var processInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run",
            WorkingDirectory = fullPath,
            UseShellExecute = !detached,
            RedirectStandardOutput = detached,
            RedirectStandardError = detached,
            CreateNoWindow = detached
        };

        var process = Process.Start(processInfo);
        if (process == null)
        {
            throw new InvalidOperationException($"Failed to start {serviceName}");
        }

        if (detached)
        {
            logger.LogInformation("{ServiceName} started in background (PID: {ProcessId})", serviceName, process.Id);
        }
        else
        {
            await process.WaitForExitAsync();
            logger.LogInformation("{ServiceName} completed with exit code: {ExitCode}", serviceName, process.ExitCode);
        }
    }

    private async Task WaitForApplicationServicesReadyAsync(string[] services, int timeoutSeconds, ILogger logger)
    {
        logger.LogInformation("Waiting for application services to be ready...");

        var tasks = new List<Task>();

        if (services.Contains("api"))
        {
            tasks.Add(WaitForHttpServiceReadyAsync("Publisher API", "http://localhost:5000/health", timeoutSeconds, logger));
        }

        if (tasks.Any())
        {
            await Task.WhenAll(tasks);
            logger.LogInformation("✅ All application services are ready!");
        }
    }

    private async Task WaitForServiceReadyAsync(string serviceName, string host, int port, int timeoutSeconds, ILogger logger)
    {
        logger.LogDebug("Waiting for {ServiceName} at {Host}:{Port}...", serviceName, host, port);

        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        
        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                using var tcpClient = new System.Net.Sockets.TcpClient();
                await tcpClient.ConnectAsync(host, port);
                logger.LogInformation("✅ {ServiceName} is ready at {Host}:{Port}", serviceName, host, port);
                return;
            }
            catch
            {
                await Task.Delay(1000, cancellationTokenSource.Token);
            }
        }

        throw new TimeoutException($"Timeout waiting for {serviceName} to be ready at {host}:{port}");
    }

    private async Task WaitForHttpServiceReadyAsync(string serviceName, string url, int timeoutSeconds, ILogger logger)
    {
        logger.LogDebug("Waiting for {ServiceName} at {Url}...", serviceName, url);

        using var httpClient = new HttpClient();
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                var response = await httpClient.GetAsync(url, cancellationTokenSource.Token);
                if (response.IsSuccessStatusCode)
                {
                    logger.LogInformation("✅ {ServiceName} is ready at {Url}", serviceName, url);
                    return;
                }
            }
            catch
            {
                await Task.Delay(2000, cancellationTokenSource.Token);
            }
        }

        throw new TimeoutException($"Timeout waiting for {serviceName} to be ready at {url}");
    }
}

public class LoggerBinder : System.CommandLine.Binding.BinderBase<ILogger>
{
    protected override ILogger GetBoundValue(System.CommandLine.Binding.BindingContext bindingContext)
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        
        return loggerFactory.CreateLogger("StartCommand");
    }
}
