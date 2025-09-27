using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace ExampleMessaging.Cli.Commands;

public static class StartCommand
{
    public static Command Create()
    {
        var detachedOption = new Option<bool>("--detached") { Description = "Run services in detached mode" };
        var waitOption = new Option<bool>("--wait") { Description = "Wait for all services to be healthy before returning" };
        var timeoutOption = new Option<int>("--timeout") { Description = "Timeout in seconds to wait for services to be ready" };
        var verboseOption = new Option<bool>("--verbose") { Description = "Enable verbose output" };

        // Set default values
        waitOption.DefaultValueFactory = _ => true;
        timeoutOption.DefaultValueFactory = _ => 30;

        var command = new Command("start", "Start the messaging environment with Docker Compose");
        command.Add(detachedOption);
        command.Add(waitOption);
        command.Add(timeoutOption);
        command.Add(verboseOption);

        command.SetAction(async (parseResult) =>
        {
            var detached = parseResult.GetValue(detachedOption);
            var wait = parseResult.GetValue(waitOption);
            var timeout = parseResult.GetValue(timeoutOption);
            var verbose = parseResult.GetValue(verboseOption);

            await ExecuteAsync(detached, wait, timeout, verbose);
        });

        return command;
    }

    private static async Task ExecuteAsync(bool detached, bool wait, int timeout, bool verbose)
    {
        try
        {
            Console.WriteLine("🚀 Starting messaging environment...");
            Console.WriteLine($"   Mode: {(detached ? "Detached" : "Attached")}");
            Console.WriteLine($"   Wait for health: {wait}");
            Console.WriteLine($"   Timeout: {timeout}s");
            Console.WriteLine();

            // Check if Docker is available
            if (!await IsDockerAvailableAsync(verbose))
            {
                Console.WriteLine("❌ Docker is not available or not running");
                Environment.Exit(1);
                return;
            }

            if (verbose)
                Console.WriteLine("✅ Docker is available");

            // Start infrastructure services
            await StartInfrastructureAsync(detached, verbose);
            
            if (wait)
            {
                await WaitForServicesHealthyAsync(timeout, verbose);
            }

            Console.WriteLine();
            Console.WriteLine("✅ Messaging environment started successfully!");
            
            if (!detached)
            {
                Console.WriteLine("📋 Services running:");
                Console.WriteLine("   - RabbitMQ Management: http://localhost:15672 (guest/guest)");
                Console.WriteLine("   - MQTT Broker: localhost:1883");
                Console.WriteLine();
                Console.WriteLine("Press Ctrl+C to stop services...");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error starting messaging environment: {ex.Message}");
            if (verbose)
            {
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
            }
            Environment.Exit(1);
        }
    }

    private static async Task<bool> IsDockerAvailableAsync(bool verbose)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null) return false;

            await process.WaitForExitAsync();
            
            if (process.ExitCode == 0)
            {
                if (verbose)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    Console.WriteLine($"   Docker version: {output.Trim()}");
                }
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static async Task StartInfrastructureAsync(bool detached, bool verbose)
    {
        Console.WriteLine("🐋 Starting Docker Compose services...");

        var args = detached ? "up -d" : "up";
        
        var processInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"compose -f ../../docker/docker-compose.yml {args}",
            RedirectStandardOutput = !detached || verbose,
            RedirectStandardError = !detached || verbose,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processInfo);
        if (process == null)
            throw new InvalidOperationException("Failed to start docker compose process");

        if (!detached || verbose)
        {
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            var output = await outputTask;
            var error = await errorTask;

            if (verbose && !string.IsNullOrEmpty(output))
                Console.WriteLine($"   Output: {output}");

            if (!string.IsNullOrEmpty(error))
                Console.WriteLine($"   Error: {error}");

            if (process.ExitCode != 0)
                throw new InvalidOperationException($"Docker compose failed with exit code {process.ExitCode}");
        }
        else
        {
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"Docker compose failed with exit code {process.ExitCode}");
        }

        Console.WriteLine("✅ Docker Compose services started");
    }

    private static async Task WaitForServicesHealthyAsync(int timeoutSeconds, bool verbose)
    {
        Console.WriteLine("⏳ Waiting for services to be healthy...");

        var services = new[]
        {
            ("RabbitMQ", "localhost", 5672),
            ("MQTT", "localhost", 1883)
        };

        var timeout = TimeSpan.FromSeconds(timeoutSeconds);
        var startTime = DateTime.UtcNow;

        foreach (var (serviceName, host, port) in services)
        {
            if (verbose)
                Console.WriteLine($"   Checking {serviceName} at {host}:{port}...");

            while (DateTime.UtcNow - startTime < timeout)
            {
                if (await IsPortOpenAsync(host, port))
                {
                    Console.WriteLine($"✅ {serviceName} is ready");
                    break;
                }

                if (DateTime.UtcNow - startTime >= timeout)
                    throw new TimeoutException($"Timeout waiting for {serviceName} to be ready at {host}:{port}");

                await Task.Delay(1000);
            }
        }

        Console.WriteLine("✅ All services are healthy");
    }

    private static async Task<bool> IsPortOpenAsync(string host, int port)
    {
        try
        {
            using var client = new System.Net.Sockets.TcpClient();
            await client.ConnectAsync(host, port);
            return client.Connected;
        }
        catch
        {
            return false;
        }
    }
}
