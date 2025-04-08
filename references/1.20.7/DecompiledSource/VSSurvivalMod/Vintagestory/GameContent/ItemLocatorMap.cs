using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class ItemLocatorMap : Item, ITradeableCollectible
{
	private ModSystemStructureLocator strucLocSys;

	private GenStoryStructures storyStructures;

	private LocatorProps props;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		strucLocSys = api.ModLoader.GetModSystem<ModSystemStructureLocator>();
		storyStructures = api.ModLoader.GetModSystem<GenStoryStructures>();
		props = Attributes["locatorProps"].AsObject<LocatorProps>();
	}

	public bool OnDidTrade(EntityTradingHumanoid trader, ItemStack stack, EnumTradeDirection tradeDir)
	{
		StructureLocation loc = strucLocSys.FindFreshStructureLocation(props.SchematicCode, trader.SidedPos.AsBlockPos, 350);
		stack.Attributes.SetVec3i("position", loc.Position);
		stack.Attributes.SetInt("regionX", loc.RegionX);
		stack.Attributes.SetInt("regionZ", loc.RegionZ);
		strucLocSys.ConsumeStructureLocation(loc);
		return true;
	}

	public EnumTransactionResult OnTryTrade(EntityTradingHumanoid eTrader, ItemSlot tradeSlot, EnumTradeDirection tradeDir)
	{
		if (tradeSlot is ItemSlotTrade slottrade && strucLocSys.FindFreshStructureLocation(props.SchematicCode, eTrader.SidedPos.AsBlockPos, 350) == null)
		{
			slottrade.TradeItem.Stock = 0;
			return EnumTransactionResult.TraderNotEnoughSupplyOrDemand;
		}
		return EnumTransactionResult.Success;
	}

	public bool ShouldTrade(EntityTradingHumanoid trader, TradeItem tradeIdem, EnumTradeDirection tradeDir)
	{
		return strucLocSys.FindFreshStructureLocation(props.SchematicCode, trader.SidedPos.AsBlockPos, 350) != null;
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
		handling = EnumHandHandling.Handled;
		if (!((byEntity as EntityPlayer).Player is IServerPlayer player))
		{
			return;
		}
		WaypointMapLayer wml = api.ModLoader.GetModSystem<WorldMapManager>().MapLayers.FirstOrDefault((MapLayer ml) => ml is WaypointMapLayer) as WaypointMapLayer;
		ITreeAttribute attr = slot.Itemstack.Attributes;
		Vec3d pos = null;
		if (attr.HasAttribute("structureIndex") || attr.HasAttribute("positionX"))
		{
			pos = getStructureCenter(attr);
		}
		if (pos == null)
		{
			foreach (KeyValuePair<string, StoryStructureLocation> val in storyStructures.storyStructureInstances)
			{
				if (!(val.Key != props.SchematicCode))
				{
					pos = val.Value.CenterPos.ToVec3d().Add(0.5, 0.5, 0.5);
					break;
				}
			}
		}
		if (pos == null)
		{
			player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("No location found on this map"), EnumChatType.Notification);
			return;
		}
		if (props.Offset != null)
		{
			pos.Add(props.Offset);
		}
		if (!attr.HasAttribute("randomX"))
		{
			Random rnd = new Random(api.World.Seed + Code.GetHashCode());
			attr.SetFloat("randomX", (float)rnd.NextDouble() * props.RandomX * 2f - props.RandomX);
			attr.SetFloat("randomZ", (float)rnd.NextDouble() * props.RandomZ * 2f - props.RandomZ);
		}
		pos.X += attr.GetFloat("randomX");
		pos.Z += attr.GetFloat("randomZ");
		if (!byEntity.World.Config.GetBool("allowMap", defaultValue: true) || wml == null)
		{
			Vec3d vec = pos.Sub(byEntity.Pos.XYZ);
			vec.Y = 0.0;
			player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("{0} blocks distance", (int)vec.Length()), EnumChatType.Notification);
			return;
		}
		string puid = (byEntity as EntityPlayer).PlayerUID;
		if (wml.Waypoints.Where((Waypoint wp) => wp.OwningPlayerUid == puid).FirstOrDefault((Waypoint wp) => wp.Position == pos) != null)
		{
			player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("Location already marked on your map"), EnumChatType.Notification);
			return;
		}
		wml.AddWaypoint(new Waypoint
		{
			Color = ColorUtil.ColorFromRgba((int)(props.WaypointColor[0] * 255.0), (int)(props.WaypointColor[1] * 255.0), (int)(props.WaypointColor[2] * 255.0), (int)(props.WaypointColor[3] * 255.0)),
			Icon = props.WaypointIcon,
			Pinned = true,
			Position = pos,
			OwningPlayerUid = puid,
			Title = Lang.Get(props.WaypointText)
		}, player);
		string msg = (attr.HasAttribute("randomX") ? Lang.Get("Approximate location of {0} added to your world map", Lang.Get(props.WaypointText)) : Lang.Get("Location of {0} added to your world map", Lang.Get(props.WaypointText)));
		player.SendMessage(GlobalConstants.GeneralChatGroup, msg, EnumChatType.Notification);
	}

	private Vec3d getStructureCenter(ITreeAttribute attr)
	{
		GeneratedStructure struc = strucLocSys.GetStructure(new StructureLocation
		{
			StructureIndex = attr.GetInt("structureIndex", -1),
			Position = attr.GetVec3i("position"),
			RegionX = attr.GetInt("regionX"),
			RegionZ = attr.GetInt("regionZ")
		});
		if (struc == null)
		{
			return null;
		}
		Vec3i c = struc.Location.Center;
		return new Vec3d((double)c.X + 0.5, (double)c.Y + 0.5, (double)c.Z + 0.5);
	}
}
