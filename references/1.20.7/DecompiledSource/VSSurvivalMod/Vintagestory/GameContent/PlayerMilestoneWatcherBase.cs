using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public abstract class PlayerMilestoneWatcherBase
{
	protected ICoreAPI api;

	public string Code;

	public virtual void Init(ICoreAPI api)
	{
		this.api = api;
	}

	public virtual void OnItemStackReceived(ItemStack stack, string eventName)
	{
	}

	public virtual void OnBlockPlaced(BlockPos pos, Block block, ItemStack withStackInHands)
	{
	}

	public virtual void OnBlockLookedAt(BlockSelection blockSel)
	{
	}

	public virtual void FromJson(JsonObject job)
	{
	}

	public virtual void ToJson(JsonObject job)
	{
	}
}
