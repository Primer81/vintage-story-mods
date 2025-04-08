using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Vintagestory.ServerMods;

internal class ModSystemDetailModeSync : ModSystem
{
	private ICoreServerAPI sapi;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		sapi = api as ICoreServerAPI;
		api.Network.RegisterChannel("detailmodesync").RegisterMessageType<SetDetailModePacket>();
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		api.Network.GetChannel("detailmodesync").SetMessageHandler<SetDetailModePacket>(onPacket);
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		new MicroblockCommands().Start(api);
	}

	private void onPacket(SetDetailModePacket packet)
	{
		BlockEntityChisel.ForceDetailingMode = packet.Enable;
	}

	internal void Toggle(string playerUID, bool on)
	{
		sapi.Network.GetChannel("detailmodesync").SendPacket(new SetDetailModePacket
		{
			Enable = on
		}, sapi.World.PlayerByUid(playerUID) as IServerPlayer);
	}
}
