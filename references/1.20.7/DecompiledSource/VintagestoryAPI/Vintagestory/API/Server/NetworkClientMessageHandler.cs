namespace Vintagestory.API.Server;

/// <summary>
/// Handler for processing a message
/// </summary>
/// <param name="fromPlayer"></param>
/// <param name="packet"></param>
public delegate void NetworkClientMessageHandler<T>(IServerPlayer fromPlayer, T packet);
