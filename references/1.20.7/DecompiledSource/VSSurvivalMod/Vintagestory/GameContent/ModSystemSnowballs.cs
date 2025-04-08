using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ModSystemSnowballs : ModSystem
{
	private ICoreAPI api;

	public override double ExecuteOrder()
	{
		return 1.0;
	}

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		this.api = api;
		api.RegisterBlockBehaviorClass("Snowballable", typeof(BlockBehaviorSnowballable));
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		api.Event.ServerRunPhase(EnumServerRunPhase.WorldReady, addSnowballableBehavior);
	}

	private void addSnowballableBehavior()
	{
		foreach (Block block in api.World.Blocks)
		{
			if (!(block.Code == null) && block.Id != 0 && ((block.snowLevel == 1f && block.Variant.ContainsKey("height")) || block.snowLevel > 1f || (block.Attributes != null && block.Attributes.KeyExists("snowballableDecrementedBlockCode"))))
			{
				block.BlockBehaviors = block.BlockBehaviors.Append(new BlockBehaviorSnowballable(block));
				block.CollectibleBehaviors = block.CollectibleBehaviors.Append(new BlockBehaviorSnowballable(block));
			}
		}
	}
}
