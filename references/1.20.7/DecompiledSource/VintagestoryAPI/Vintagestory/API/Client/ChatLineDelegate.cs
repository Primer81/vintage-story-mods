using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

public delegate void ChatLineDelegate(int groupId, string message, EnumChatType chattype, string data);
