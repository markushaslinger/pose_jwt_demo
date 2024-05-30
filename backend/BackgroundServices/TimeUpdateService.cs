using JwtDemo.Core;
using JwtDemo.Hubs;
using JwtDemo.Model;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using NodaTime;

namespace JwtDemo.BackgroundServices;

public sealed class TimeUpdateService(
    IClock clock,
    IHubContext<TimeHub, ITimeClient> hubContext,
    IOptions<Settings> options) : BackgroundService
{
    private const int LowQualityUpdateInterval = 5;
    private readonly IClock _clock = clock;
    private readonly IHubContext<TimeHub, ITimeClient> _hubContext = hubContext;
    private readonly Settings _settings = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(GetDurationUntilNextFullSecond().ToTimeSpan(), stoppingToken);

            var now = GetLocalTime();
            var sendToUsersOnly = now.Second % LowQualityUpdateInterval != 0;
            await SendTimeUpdate(now, sendToUsersOnly);
        }
    }

    private async Task SendTimeUpdate(LocalDateTime currentTime, bool sendToUsersOnly)
    {
        // If sending the update to all clients would take longer than our 1-second interval, we'd have to be smarter here.
        // Luckily, we only have a few clients in this demo, so we can afford to be lazy.
        
        var update = new TimeUpdate
        {
            CurrentTime = currentTime,
            Quality = TimeQuality.High
        };
        await _hubContext.Clients.Group(TimeHub.AuthenticatedGroupName).ReceiveTime(update);

        if (sendToUsersOnly)
        {
            return;
        }

        update.Quality = TimeQuality.Low;
        await _hubContext.Clients.Group(TimeHub.UnauthenticatedGroupName).ReceiveTime(update);
    }

    private Duration GetDurationUntilNextFullSecond()
    {
        var now = GetLocalTime();
        var nextFullSecond = now.PlusSeconds(1).With(TimeAdjusters.TruncateToSecond);
        var diff = nextFullSecond - now;

        return diff.ToDuration();
    }

    private LocalDateTime GetLocalTime()
    {
        var timezone = DateTimeZoneProviders.Tzdb[_settings.Timezone];
        var zonedTime = _clock.GetCurrentInstant().InZone(timezone);

        return zonedTime.LocalDateTime;
    }
}
