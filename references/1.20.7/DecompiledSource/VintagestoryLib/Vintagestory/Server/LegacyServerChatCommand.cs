using Vintagestory.API.Common;

namespace Vintagestory.Server;

public interface LegacyServerChatCommand
{
	string syntax { get; set; }

	ServerChatCommandDelegate GetDelegate();
}
