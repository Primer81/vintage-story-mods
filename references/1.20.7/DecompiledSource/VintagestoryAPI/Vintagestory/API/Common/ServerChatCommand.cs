using Vintagestory.API.Server;

namespace Vintagestory.API.Common;

/// <summary>
/// A handler for creating server commands.
/// </summary>
public class ServerChatCommand : ChatCommand
{
	public ServerChatCommandDelegate handler;

	/// <summary>
	/// Whether or not the player has the privilage to run the command.
	/// </summary>
	/// <param name="player"></param>
	/// <returns></returns>
	public bool HasPrivilege(IServerPlayer player)
	{
		return player.HasPrivilege(RequiredPrivilege);
	}

	public override void CallHandler(IPlayer player, int groupId, CmdArgs args)
	{
		handler((IServerPlayer)player, groupId, args);
	}
}
