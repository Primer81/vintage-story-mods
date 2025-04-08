using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class ModSystemShipConstructionSitePreview : ModSystem
{
	private ICoreClientAPI capi;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Client;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		api.Event.RegisterGameTickListener(onTick, 100);
	}

	private void onTick(float dt)
	{
		if (capi.World.Player.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible is ItemRoller)
		{
			int orient = ItemRoller.GetOrient(capi.World.Player);
			List<BlockPos> siteList = ItemRoller.siteListByFacing[orient];
			List<BlockPos> waterEdgeList = ItemRoller.waterEdgeByFacing[orient];
			int c = ColorUtil.ColorFromRgba(0, 50, 150, 50);
			capi.World.HighlightBlocks(capi.World.Player, 941, siteList, EnumHighlightBlocksMode.AttachedToSelectedBlock, EnumHighlightShape.Cube);
			capi.World.HighlightBlocks(capi.World.Player, 942, waterEdgeList, new List<int> { c }, EnumHighlightBlocksMode.AttachedToSelectedBlock, EnumHighlightShape.Cube);
		}
		else
		{
			capi.World.HighlightBlocks(capi.World.Player, 941, ItemRoller.emptyList, EnumHighlightBlocksMode.AttachedToSelectedBlock, EnumHighlightShape.Cube);
			capi.World.HighlightBlocks(capi.World.Player, 942, ItemRoller.emptyList, EnumHighlightBlocksMode.AttachedToSelectedBlock, EnumHighlightShape.Cube);
		}
	}
}
