namespace ExampleMessaging.Shared.Models.Performance;

public class TestScenario
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public LoadPatternConfig LoadPattern { get; set; } = new();
    public PerformanceCriteria Criteria { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();

    public TestScenario() { }

    public TestScenario(string name, string description)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }
}

public class LoadPatternConfig
{
    public int MaxVirtualUsers { get; set; } = 10;
    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan RampUpTime { get; set; } = TimeSpan.FromSeconds(10);
    public int RequestsPerSecond { get; set; } = 100;
    public int MessagesPerSecond { get; set; } = 1000;
}

public class PerformanceCriteria
{
    public double MaxAverageResponseTime { get; set; } = 100; // milliseconds
    public double MaxP95ResponseTime { get; set; } = 200; // milliseconds
    public double MaxP99ResponseTime { get; set; } = 500; // milliseconds
    public double MinThroughput { get; set; } = 95; // requests per second
    public double MaxErrorRate { get; set; } = 1; // percentage
    public double MinSuccessRate { get; set; } = 99; // percentage
}
