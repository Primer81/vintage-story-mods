namespace Vintagestory.API.Common;

/// <summary>
/// A handle for creating client commands.
/// </summary>
public class ClientChatCommand : ChatCommand
{
	public ClientChatCommandDelegate handler;

	public override void CallHandler(IPlayer player, int groupId, CmdArgs args)
	{
		handler(groupId, args);
	}
}
