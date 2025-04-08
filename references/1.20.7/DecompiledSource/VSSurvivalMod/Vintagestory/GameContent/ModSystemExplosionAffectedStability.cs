using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class ModSystemExplosionAffectedStability : ModSystem
{
	private ICoreServerAPI sapi;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Server;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		api.Event.RegisterEventBusListener(onExplosion, 0.5, "onexplosion");
		api.Event.DidPlaceBlock += OnBlockPlacedEvent;
	}

	private void onExplosion(string eventName, ref EnumHandling handling, IAttribute data)
	{
		ITreeAttribute obj = data as ITreeAttribute;
		BlockPos pos = obj.GetBlockPos("pos");
		double radius = obj.GetDouble("destructionRadius");
		double cnt = radius * radius * radius;
		int radint = (int)Math.Round(radius) + 1;
		Random rnd = sapi.World.Rand;
		BlockPos tmpPos = new BlockPos();
		while (cnt-- > 0.0)
		{
			int dx = rnd.Next(2 * radint) - radint;
			int dy = rnd.Next(2 * radint) - radint;
			int dz = rnd.Next(2 * radint) - radint;
			tmpPos.Set(pos.X + dx, pos.Y + dy, pos.Z + dz);
			sapi.World.BlockAccessor.GetBlock(tmpPos, 1).GetBehavior<BlockBehaviorUnstableRock>()?.CheckCollapsible(sapi.World, tmpPos);
		}
	}

	private void OnBlockPlacedEvent(IServerPlayer byPlayer, int oldblockId, BlockSelection blockSel, ItemStack withItemStack)
	{
		(withItemStack?.Block?.GetBehavior<BlockBehaviorUnstableRock>())?.CheckCollapsible(sapi.World, blockSel.Position);
	}
}
