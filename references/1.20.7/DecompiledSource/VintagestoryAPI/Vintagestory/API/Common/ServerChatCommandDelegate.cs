using Vintagestory.API.Server;

namespace Vintagestory.API.Common;

public delegate void ServerChatCommandDelegate(IServerPlayer player, int groupId, CmdArgs args);
