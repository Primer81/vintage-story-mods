namespace Vintagestory.API.Client;

/// <summary>
/// Handler for processing a message
/// </summary>
/// <param name="packet"></param>
public delegate void NetworkServerMessageHandler<T>(T packet);
