using Vintagestory.API.Common;

namespace Vintagestory.Common;

public class CreativeNetworkUtil : InventoryNetworkUtil
{
	public CreativeNetworkUtil(InventoryBase inv, ICoreAPI api)
		: base(inv, api)
	{
	}

	public override Packet_InventoryContents ToPacket(IPlayer player)
	{
		return new Packet_InventoryContents
		{
			ClientId = player.ClientId,
			InventoryId = inv.InventoryID,
			InventoryClass = inv.ClassName
		};
	}

	public override void UpdateFromPacket(IWorldAccessor world, Packet_InventoryContents packet)
	{
		(inv as InventoryPlayerCreative).UpdateFromWorld(world);
	}

	public override void UpdateFromPacket(IWorldAccessor resolver, Packet_InventoryUpdate packet)
	{
	}

	public override void UpdateFromPacket(IWorldAccessor resolver, Packet_InventoryDoubleUpdate packet)
	{
	}
}
