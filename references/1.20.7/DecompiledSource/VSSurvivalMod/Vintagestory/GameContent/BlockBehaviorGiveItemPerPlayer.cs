using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class BlockBehaviorGiveItemPerPlayer : BlockBehavior
{
	private string interactionHelpCode;

	public BlockBehaviorGiveItemPerPlayer(Block block)
		: base(block)
	{
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		interactionHelpCode = block.Attributes["interactionHelpCode"].AsString();
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
	{
		block.GetBEBehavior<BEBehaviorGiveItemPerPlayer>(blockSel.Position)?.OnInteract(byPlayer);
		handling = EnumHandling.PreventDefault;
		return true;
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, ref EnumHandling handling)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = interactionHelpCode,
				MouseButton = EnumMouseButton.Right
			}
		};
	}
}
