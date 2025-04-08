using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

public delegate void ClientChatLineDelegate(int groupId, ref string message, ref EnumHandling handled);
