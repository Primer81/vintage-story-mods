using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Vintagestory.API.Common;

/// <summary>
/// When the player wrote a chat message. Set consumed.value to true to prevent further processing of this chat message
/// </summary>
/// <param name="byPlayer">The player that submitted the chat message</param>
/// <param name="channelId">The chat group id from where the message was sent from</param>
/// <param name="message">The chat message</param>
/// <param name="consumed">If set, the even is considered consumed, i.e. should no longer be handled further by the game engine</param>
/// <param name="data"></param>
/// <returns>The resulting string.</returns>
public delegate void PlayerChatDelegate(IServerPlayer byPlayer, int channelId, ref string message, ref string data, BoolRef consumed);
