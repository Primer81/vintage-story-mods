using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockForFluidsLayer : Block
{
	public float InsideDamage;

	public EnumDamageType DamageType;

	public override bool ForFluidsLayer => true;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		InsideDamage = Attributes?["insideDamage"].AsFloat() ?? 0f;
		DamageType = (EnumDamageType)Enum.Parse(typeof(EnumDamageType), Attributes?["damageType"].AsString("Fire") ?? "Fire");
	}

	public override void OnEntityInside(IWorldAccessor world, Entity entity, BlockPos pos)
	{
		if (InsideDamage > 0f && world.Side == EnumAppSide.Server)
		{
			entity.ReceiveDamage(new DamageSource
			{
				Type = DamageType,
				Source = EnumDamageSource.Block,
				SourceBlock = this,
				SourcePos = pos.ToVec3d()
			}, InsideDamage);
		}
	}

	public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
	{
		bool result = true;
		bool preventDefault = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			bool behaviorResult = obj.DoPlaceBlock(world, byPlayer, blockSel, byItemStack, ref handled);
			if (handled != 0)
			{
				result = result && behaviorResult;
				preventDefault = true;
			}
			if (handled == EnumHandling.PreventSubsequent)
			{
				return result;
			}
		}
		if (preventDefault)
		{
			return result;
		}
		world.BlockAccessor.SetBlock(BlockId, blockSel.Position, 2);
		return true;
	}
}
