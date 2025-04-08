using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class BEBehaviorAnimatable : BlockEntityBehavior
{
	public BlockEntityAnimationUtil animUtil;

	public BEBehaviorAnimatable(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		animUtil = new BlockEntityAnimationUtil(api, Blockentity);
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		animUtil?.Dispose();
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
		base.OnBlockBroken(byPlayer);
		animUtil?.Dispose();
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		animUtil?.Dispose();
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (animUtil.activeAnimationsByAnimCode.Count <= 0)
		{
			if (animUtil.animator != null)
			{
				return animUtil.animator.ActiveAnimationCount > 0;
			}
			return false;
		}
		return true;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		if (Api is ICoreClientAPI capi && capi.Settings.Bool["extendedDebugInfo"])
		{
			dsc.AppendLine(string.Format("Active animations: {0}", string.Join(", ", animUtil.activeAnimationsByAnimCode.Keys)));
		}
	}
}
