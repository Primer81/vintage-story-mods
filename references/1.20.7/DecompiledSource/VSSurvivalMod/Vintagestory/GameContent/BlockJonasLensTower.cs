using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockJonasLensTower : Block
{
	private WorldInteraction[] interactions;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		if (api.Side == EnumAppSide.Client)
		{
			interactions = ObjectCacheUtil.GetOrCreate(api, "lensInteractions", () => new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-lens-pickup",
					HotKeyCode = null,
					MouseButton = EnumMouseButton.Right
				}
			});
		}
	}

	public override float OnBlockBreaking(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
	{
		return 999f;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		GetBlockEntity<BEJonasLensTower>(blockSel)?.OnInteract(byPlayer);
		return true;
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		BEJonasLensTower blockEntity = GetBlockEntity<BEJonasLensTower>(selection);
		if (blockEntity != null && blockEntity.RecentlyCollectedBy(forPlayer))
		{
			return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
		}
		return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}
}
