using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockDisplayCase : Block
{
	private WorldInteraction[] interactions;

	public float height;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		height = Attributes["height"].AsFloat(0.5f);
		if (api.Side == EnumAppSide.Client)
		{
			interactions = ObjectCacheUtil.GetOrCreate(api, "displayCaseInteractions", () => new WorldInteraction[2]
			{
				new WorldInteraction
				{
					MouseButton = EnumMouseButton.Right,
					ActionLangCode = "blockhelp-displaycase-place"
				},
				new WorldInteraction
				{
					MouseButton = EnumMouseButton.Right,
					RequireFreeHand = true,
					ActionLangCode = "blockhelp-displaycase-remove"
				}
			});
		}
	}

	public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
	{
		return true;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityDisplayCase bedc)
		{
			return bedc.OnInteract(byPlayer, blockSel);
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}
}
