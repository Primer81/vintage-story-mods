using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockBeehive : Block
{
	private BlockPos atPos = new BlockPos();

	private Cuboidf[] nocoll = new Cuboidf[0];

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
		if (world.Side != EnumAppSide.Client)
		{
			EntityProperties type = world.GetEntityType(new AssetLocation("beemob"));
			Entity entity = world.ClassRegistry.CreateEntity(type);
			if (entity != null)
			{
				entity.ServerPos.X = (float)pos.X + 0.5f;
				entity.ServerPos.Y = (float)pos.Y + 0.5f;
				entity.ServerPos.Z = (float)pos.Z + 0.5f;
				entity.ServerPos.Yaw = (float)world.Rand.NextDouble() * 2f * (float)Math.PI;
				entity.Pos.SetFrom(entity.ServerPos);
				entity.Attributes.SetString("origin", "brokenbeehive");
				world.SpawnEntity(entity);
			}
		}
	}

	public override Cuboidf[] GetParticleCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return nocoll;
	}

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldgenRand, BlockPatchAttributes attributes = null)
	{
		for (int i = 2; i < 7; i++)
		{
			atPos.Set(pos.X, pos.Y - i, pos.Z);
			Block aboveBlock = blockAccessor.GetBlock(atPos);
			EnumBlockMaterial abovemat = aboveBlock.GetBlockMaterial(blockAccessor, atPos);
			if ((abovemat != EnumBlockMaterial.Wood && abovemat != EnumBlockMaterial.Leaves) || !aboveBlock.SideSolid[BlockFacing.DOWN.Index])
			{
				continue;
			}
			atPos.Set(pos.X, pos.Y - i - 1, pos.Z);
			Block block = blockAccessor.GetBlock(atPos);
			EnumBlockMaterial mat = block.GetBlockMaterial(blockAccessor, atPos);
			BlockPos belowPos = atPos.DownCopy();
			if (mat == EnumBlockMaterial.Wood && abovemat == EnumBlockMaterial.Wood && blockAccessor.GetBlock(belowPos).GetBlockMaterial(blockAccessor, belowPos) == EnumBlockMaterial.Wood && block.Variant["rotation"] == "ud")
			{
				Block inlogblock = blockAccessor.GetBlock(new AssetLocation("wildbeehive-inlog-" + aboveBlock.Variant["wood"]));
				blockAccessor.SetBlock(inlogblock.BlockId, atPos);
				if (EntityClass != null)
				{
					blockAccessor.SpawnBlockEntity(EntityClass, atPos);
				}
				return true;
			}
			if (mat == EnumBlockMaterial.Leaves || mat == EnumBlockMaterial.Air)
			{
				int dx = pos.X % 32;
				int dz = pos.Z % 32;
				int surfacey = blockAccessor.GetMapChunkAtBlockPos(atPos).WorldGenTerrainHeightMap[dz * 32 + dx];
				if (pos.Y - surfacey < 4)
				{
					return false;
				}
				blockAccessor.SetBlock(BlockId, atPos);
				if (EntityClass != null)
				{
					blockAccessor.SpawnBlockEntity(EntityClass, atPos);
				}
				return true;
			}
		}
		return false;
	}
}
