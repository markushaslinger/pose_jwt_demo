using JwtDemo.Model;
using Microsoft.AspNetCore.SignalR;

namespace JwtDemo.Hubs;

public interface ITimeClient
{
    Task ReceiveTime(TimeUpdate update);
}

public sealed class TimeHub : Hub<ITimeClient>
{
    public const string AuthenticatedGroupName = "AuthenticatedUsers";
    public const string UnauthenticatedGroupName = "UnauthenticatedUsers";
    
    public override async Task OnConnectedAsync()
    {
        if (Context.User?.Identity?.IsAuthenticated ?? false)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, AuthenticatedGroupName);
        }
        else
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UnauthenticatedGroupName);
        }
        
        await base.OnConnectedAsync();    
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // We have to do the remove for all groups, because there currently is no way to find out which
        // groups a connection is in, without tracking it ourselves in some static, concurrent collection.
        // It will be a no-op for the group the connection is not in.
        var connectionId = Context.ConnectionId;
        await Groups.RemoveFromGroupAsync(connectionId, AuthenticatedGroupName);
        await Groups.RemoveFromGroupAsync(connectionId, UnauthenticatedGroupName);
    }
}
