using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Server;

namespace Vintagestory.Server;

public class CmdInfo
{
	public CmdInfo(ServerMain server)
	{
		server.api.commandapi.Create("info").RequiresPrivilege(Privilege.controlserver).WithDesc("Server information")
			.BeginSub("ident")
			.WithDesc("Get save game identifier")
			.HandleWith((TextCommandCallingArgs args) => TextCommandResult.Success(server.SaveGameData.SavegameIdentifier))
			.EndSub()
			.BeginSub("seed")
			.WithDesc("Get world seed")
			.HandleWith((TextCommandCallingArgs args) => TextCommandResult.Success(server.SaveGameData.Seed.ToString() ?? ""))
			.EndSub()
			.BeginSub("createdversion")
			.WithDesc("Get game version on which this save game was created on")
			.HandleWith((TextCommandCallingArgs args) => TextCommandResult.Success(server.SaveGameData.CreatedGameVersion))
			.EndSub()
			.BeginSub("mapsize")
			.WithDesc("Get world map size")
			.HandleWith((TextCommandCallingArgs args) => TextCommandResult.Success(server.SaveGameData.MapSizeX + "x" + server.SaveGameData.MapSizeY + "x" + server.SaveGameData.MapSizeZ))
			.EndSub();
	}
}
