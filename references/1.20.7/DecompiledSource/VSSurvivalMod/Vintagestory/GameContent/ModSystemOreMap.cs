using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ModSystemOreMap : ModSystem
{
	private ICoreClientAPI capi;

	private ICoreAPI api;

	private ICoreServerAPI sapi;

	public ProspectingMetaData prospectingMetaData;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override double ExecuteOrder()
	{
		return 1.0;
	}

	public override void Start(ICoreAPI api)
	{
		api.ModLoader.GetModSystem<WorldMapManager>().RegisterMapLayer<OreMapLayer>("ores", 0.75);
		api.Network.RegisterChannel("oremap").RegisterMessageType<PropickReading>().RegisterMessageType<ProspectingMetaData>()
			.RegisterMessageType<DeleteReadingPacket>();
		this.api = api;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		api.Network.GetChannel("oremap").SetMessageHandler<PropickReading>(onPropickReadingPacket).SetMessageHandler<ProspectingMetaData>(onPropickMetaData);
		capi = api;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		api.Event.PlayerJoin += Event_PlayerJoin;
		api.Network.GetChannel("oremap").SetMessageHandler<DeleteReadingPacket>(onDeleteReading);
	}

	private void onDeleteReading(IServerPlayer fromPlayer, DeleteReadingPacket packet)
	{
		if (api.ModLoader.GetModSystem<WorldMapManager>().MapLayers.FirstOrDefault((MapLayer ml) => ml is OreMapLayer) is OreMapLayer oml)
		{
			oml.Delete(fromPlayer, packet.Index);
		}
	}

	private void Event_PlayerJoin(IServerPlayer byPlayer)
	{
		ProPickWorkSpace ppws = ObjectCacheUtil.TryGet<ProPickWorkSpace>(api, "propickworkspace");
		if (ppws != null)
		{
			sapi.Network.GetChannel("oremap").SendPacket(new ProspectingMetaData
			{
				PageCodes = ppws.pageCodes
			}, byPlayer);
		}
	}

	private void onPropickMetaData(ProspectingMetaData packet)
	{
		prospectingMetaData = packet;
	}

	private void onPropickReadingPacket(PropickReading reading)
	{
		if (capi.ModLoader.GetModSystem<WorldMapManager>().MapLayers.FirstOrDefault((MapLayer ml) => ml is OreMapLayer) is OreMapLayer oml)
		{
			oml.ownPropickReadings.Add(reading);
			oml.RebuildMapComponents();
		}
	}

	public void DidProbe(PropickReading results, IServerPlayer splr)
	{
		if (api.ModLoader.GetModSystem<WorldMapManager>().MapLayers.FirstOrDefault((MapLayer ml) => ml is OreMapLayer) is OreMapLayer oml)
		{
			oml.getOrLoadReadings(splr).Add(results);
		}
	}
}
