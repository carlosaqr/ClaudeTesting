using Microsoft.AspNetCore.SignalR;

namespace Catalogo.Api.Hubs;

/// <summary>
/// Real-time channel. The server pushes "itemChanged" (an ItemChange payload) after every
/// create, update or delete. Clients only listen; they never call the hub.
/// </summary>
public class CatalogHub : Hub
{
    public const string Path = "/hubs/catalog";
    public const string ItemChangedEvent = "itemChanged";
}
