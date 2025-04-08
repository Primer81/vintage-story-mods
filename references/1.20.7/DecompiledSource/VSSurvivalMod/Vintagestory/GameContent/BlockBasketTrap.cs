using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockBasketTrap : Block
{
	protected float rotInterval = (float)Math.PI / 8f;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		CanStep = false;
	}

	public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
	{
		bool num = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
		if (num)
		{
			BlockEntityBasketTrap be = GetBlockEntity<BlockEntityBasketTrap>(blockSel.Position);
			if (be != null)
			{
				BlockPos targetPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
				double y = byPlayer.Entity.Pos.X - ((double)targetPos.X + blockSel.HitPosition.X);
				double dz = (double)(float)byPlayer.Entity.Pos.Z - ((double)targetPos.Z + blockSel.HitPosition.Z);
				float roundRad = (float)(int)Math.Round((float)Math.Atan2(y, dz) / rotInterval) * rotInterval;
				be.RotationYDeg = roundRad * (180f / (float)Math.PI);
				be.MarkDirty(redrawOnClient: true);
			}
		}
		return num;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		return GetBlockEntity<BlockEntityBasketTrap>(blockSel.Position)?.Interact(byPlayer, blockSel) ?? base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		BlockEntityBasketTrap be = GetBlockEntity<BlockEntityBasketTrap>(pos);
		if (be != null && be.TrapState == EnumTrapState.Destroyed)
		{
			return new ItemStack[1]
			{
				new ItemStack(world.GetItem(new AssetLocation("cattailtops")), 6 + world.Rand.Next(8))
			};
		}
		return base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
	}

	public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
	{
		BlockEntityBasketTrap be = GetBlockEntity<BlockEntityBasketTrap>(pos);
		if (be != null)
		{
			blockModelData = be.GetCurrentMesh(null).Clone().Rotate(Vec3f.Half, 0f, (be.RotationYDeg - 90f) * ((float)Math.PI / 180f), 0f);
			decalModelData = be.GetCurrentMesh(decalTexSource).Clone().Rotate(Vec3f.Half, 0f, (be.RotationYDeg - 90f) * ((float)Math.PI / 180f), 0f);
		}
		else
		{
			base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
		}
	}
}
