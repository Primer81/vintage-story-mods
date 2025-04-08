using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockBaseReturnTeleporter : Block
{
	private WorldInteraction[] interactions;

	public override void OnLoaded(ICoreAPI api)
	{
		interactions = new WorldInteraction[2]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-basereturn-activate",
				HotKeyCode = "shift",
				MouseButton = EnumMouseButton.Right,
				ShouldApply = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection es)
				{
					if (bs == null)
					{
						return false;
					}
					BlockEntityBaseReturnTeleporter blockEntity = GetBlockEntity<BlockEntityBaseReturnTeleporter>(bs.Position);
					return blockEntity != null && !blockEntity.Activated;
				}
			},
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-basereturn-deactivate",
				HotKeyCode = "shift",
				MouseButton = EnumMouseButton.Right,
				ShouldApply = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => bs != null && (GetBlockEntity<BlockEntityBaseReturnTeleporter>(bs.Position)?.Activated ?? false)
			}
		};
		base.OnLoaded(api);
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (byPlayer.Entity.Controls.ShiftKey)
		{
			GetBlockEntity<BlockEntityBaseReturnTeleporter>(blockSel.Position)?.OnInteract(byPlayer);
			return true;
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}
}
