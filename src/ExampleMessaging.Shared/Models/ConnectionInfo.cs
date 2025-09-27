using ExampleMessaging.Shared.Models;

namespace ExampleMessaging.Shared.Models;

public class ConnectionInfo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ProtocolType Protocol { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string VirtualHost { get; set; } = "/";
    public ConnectionStatus Status { get; set; } = ConnectionStatus.Disconnected;
    public DateTimeOffset LastConnected { get; set; }
    public DateTimeOffset LastHeartbeat { get; set; }
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int RetryAttempts { get; set; } = 0;
    public int MaxRetryAttempts { get; set; } = 5;

    public ConnectionInfo() { }

    public ConnectionInfo(ProtocolType protocol, string host, int port, string username = "", string password = "")
    {
        Protocol = protocol;
        Host = host ?? throw new ArgumentNullException(nameof(host));
        Port = port;
        Username = username ?? string.Empty;
        Password = password ?? string.Empty;
    }

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(Host) && 
               Port > 0 && Port <= 65535 &&
               Enum.IsDefined(typeof(ProtocolType), Protocol);
    }

    public bool CanRetry()
    {
        return RetryAttempts < MaxRetryAttempts;
    }

    public void UpdateStatus(ConnectionStatus status)
    {
        Status = status;
        
        if (status == ConnectionStatus.Connected)
        {
            LastConnected = DateTimeOffset.UtcNow;
            LastHeartbeat = DateTimeOffset.UtcNow;
            RetryAttempts = 0;
        }
        else if (status == ConnectionStatus.Failed || status == ConnectionStatus.Disconnected)
        {
            RetryAttempts++;
        }
    }

    public void ResetHeartbeat()
    {
        LastHeartbeat = DateTimeOffset.UtcNow;
    }

    public bool IsHeartbeatExpired()
    {
        return DateTimeOffset.UtcNow - LastHeartbeat > ConnectionTimeout;
    }
}
