using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.Server;

internal class CmdRestart : BaseServerChatCommandDelegateProvider
{
	public CmdRestart(ServerMain server)
		: base(server)
	{
	}

	public override void Handle(IServerPlayer player, int groupId, CmdArgs args)
	{
	}
}
