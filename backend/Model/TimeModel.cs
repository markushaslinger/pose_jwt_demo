using NodaTime;

namespace JwtDemo.Model;

public sealed class TimeUpdate
{
    public LocalDateTime CurrentTime { get; set; }
    public TimeQuality Quality { get; set; }
}

public enum TimeQuality
{
    Low,
    High
}
