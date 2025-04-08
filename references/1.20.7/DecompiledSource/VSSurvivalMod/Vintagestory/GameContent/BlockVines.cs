using System;
using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockVines : Block
{
	public BlockFacing VineFacing;

	private int[] origWindMode;

	private BlockPos tmpPos = new BlockPos();

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		VineFacing = BlockFacing.FromCode(Variant["horizontalorientation"]);
	}

	public override void OnDecalTesselation(IWorldAccessor world, MeshData decalMesh, BlockPos pos)
	{
		int verticesCount = decalMesh.VerticesCount;
		IBlockAccessor ba = api.World.BlockAccessor;
		Block ablock = ba.GetBlockOnSide(pos, VineFacing.Opposite);
		if (ablock.Id != 0 && ablock.CanAttachBlockAt(ba, this, tmpPos.Set(pos).Add(VineFacing.Opposite), VineFacing) && !(ablock is BlockLeaves))
		{
			for (int i = 0; i < verticesCount; i++)
			{
				decalMesh.Flags[i] &= -503316481;
			}
			return;
		}
		int windData = ((ba.GetBlockAbove(pos, 1, 1) is BlockVines) ? 1 : 0) + ((ba.GetBlockAbove(pos, 2, 1) is BlockVines) ? 1 : 0) + ((ba.GetBlockAbove(pos, 3, 1) is BlockVines) ? 1 : 0);
		int windDatam1 = ((windData != 3 || !(ba.GetBlockAbove(pos, 4, 1) is BlockVines)) ? (Math.Max(0, windData - 1) << 29) : (windData << 29));
		windData <<= 29;
		if (ba.GetBlockAbove(pos, 1, 1) is BlockVines)
		{
			tmpPos.Set(pos, pos.dimension).Up().Add(VineFacing.Opposite);
			Block uablock = ba.GetBlock(tmpPos);
			if (uablock.Id != 0 && uablock.CanAttachBlockAt(ba, this, tmpPos, VineFacing) && !(ablock is BlockLeaves))
			{
				for (int j = 0; j < verticesCount; j++)
				{
					if ((double)decalMesh.xyz[j * 3 + 1] > 0.5)
					{
						decalMesh.Flags[j] &= -503316481;
					}
					else
					{
						decalMesh.Flags[j] = (decalMesh.Flags[j] & 0x1FFFFFF) | origWindMode[j] | windData;
					}
				}
				return;
			}
		}
		otherwiseAllWave(decalMesh, verticesCount, windData, windDatam1);
	}

	public override void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
	{
		if (origWindMode == null)
		{
			int cnt = sourceMesh.FlagsCount;
			origWindMode = (int[])sourceMesh.Flags.Clone();
			for (int k = 0; k < cnt; k++)
			{
				origWindMode[k] &= 503316480;
			}
		}
		int verticesCount = sourceMesh.VerticesCount;
		bool num = ((lightRgbsByCorner[24] >> 24) & 0xFF) >= 159;
		Block ablock = chunkExtBlocks[extIndex3d + TileSideEnum.MoveIndex[VineFacing.Opposite.Index]];
		if (!num || (ablock.Id != 0 && ablock.CanAttachBlockAt(api.World.BlockAccessor, this, tmpPos.Set(pos, pos.dimension).Add(VineFacing.Opposite), VineFacing) && !(ablock is BlockLeaves)))
		{
			for (int i = 0; i < verticesCount; i++)
			{
				sourceMesh.Flags[i] &= -503316481;
			}
			return;
		}
		int windData = ((api.World.BlockAccessor.GetBlockAbove(pos, 1, 1) is BlockVines) ? 1 : 0) + ((api.World.BlockAccessor.GetBlockAbove(pos, 2, 1) is BlockVines) ? 1 : 0) + ((api.World.BlockAccessor.GetBlockAbove(pos, 3, 1) is BlockVines) ? 1 : 0);
		int windDatam1 = ((windData != 3 || !(api.World.BlockAccessor.GetBlockAbove(pos, 4, 1) is BlockVines)) ? (Math.Max(0, windData - 1) << 29) : (windData << 29));
		windData <<= 29;
		if (chunkExtBlocks[extIndex3d + TileSideEnum.MoveIndex[BlockFacing.UP.Index]] is BlockVines)
		{
			Block uablock = chunkExtBlocks[extIndex3d + TileSideEnum.MoveIndex[VineFacing.Opposite.Index] + TileSideEnum.MoveIndex[BlockFacing.UP.Index]];
			if (uablock.Id != 0 && uablock.CanAttachBlockAt(api.World.BlockAccessor, this, tmpPos.Set(pos, pos.dimension).Up().Add(VineFacing.Opposite), VineFacing) && !(ablock is BlockLeaves))
			{
				for (int j = 0; j < verticesCount; j++)
				{
					if ((double)sourceMesh.xyz[j * 3 + 1] > 0.5)
					{
						sourceMesh.Flags[j] &= -503316481;
					}
					else
					{
						sourceMesh.Flags[j] = (sourceMesh.Flags[j] & 0x1FFFFFF) | origWindMode[j] | windData;
					}
				}
				return;
			}
		}
		otherwiseAllWave(sourceMesh, verticesCount, windData, windDatam1);
	}

	private void otherwiseAllWave(MeshData decalMesh, int verticesCount, int windData, int windDatam1)
	{
		for (int i = 0; i < verticesCount; i++)
		{
			if ((double)decalMesh.xyz[i * 3 + 1] > 0.5)
			{
				decalMesh.Flags[i] = (decalMesh.Flags[i] & 0x1FFFFFF) | origWindMode[i] | windDatam1;
			}
			else
			{
				decalMesh.Flags[i] = (decalMesh.Flags[i] & 0x1FFFFFF) | origWindMode[i] | windData;
			}
		}
	}

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, BlockPatchAttributes attributes = null)
	{
		if (!blockAccessor.GetBlock(pos).IsReplacableBy(this))
		{
			return false;
		}
		if (onBlockFace.IsHorizontal && TryAttachTo(blockAccessor, pos, onBlockFace))
		{
			return true;
		}
		Block upBlock = blockAccessor.GetBlockAbove(pos, 1, 1);
		if (upBlock is BlockVines)
		{
			BlockFacing facing = ((BlockVines)upBlock).VineFacing;
			blockAccessor.SetBlock(blockAccessor.GetBlock(CodeWithParts(facing.Code))?.BlockId ?? upBlock.BlockId, pos);
			return true;
		}
		for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++)
		{
			if (TryAttachTo(blockAccessor, pos, BlockFacing.HORIZONTALS[i]))
			{
				return true;
			}
		}
		return false;
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return false;
		}
		if (blockSel.Face.IsHorizontal && TryAttachTo(world.BlockAccessor, blockSel.Position, blockSel.Face))
		{
			return true;
		}
		Block upBlock = world.BlockAccessor.GetBlockAbove(blockSel.Position, 1, 1);
		if (upBlock is BlockVines)
		{
			BlockFacing facing = ((BlockVines)upBlock).VineFacing;
			Block block = world.BlockAccessor.GetBlock(CodeWithParts(facing.Code));
			world.BlockAccessor.SetBlock(block?.BlockId ?? upBlock.BlockId, blockSel.Position);
			return true;
		}
		failureCode = "requirevineattachable";
		return false;
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return GetHandbookDropsFromBreakDrops(handbookStack, forPlayer);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		string[] parts = Code.Path.Split('-');
		Block block = world.BlockAccessor.GetBlock(new AssetLocation(parts[0] + "-" + parts[^2].Replace("end", "section") + "-north"));
		return new ItemStack[1]
		{
			new ItemStack(block)
		};
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		string[] parts = Code.Path.Split('-');
		return new ItemStack(world.BlockAccessor.GetBlock(new AssetLocation(parts[0] + "-" + parts[^2] + "-north")));
	}

	private bool TryAttachTo(IBlockAccessor blockAccessor, BlockPos blockpos, BlockFacing onBlockFace)
	{
		BlockPos attachingBlockPos = blockpos.AddCopy(onBlockFace.Opposite);
		if (blockAccessor.GetBlock(attachingBlockPos).CanAttachBlockAt(blockAccessor, this, attachingBlockPos, onBlockFace))
		{
			int blockId = blockAccessor.GetBlock(CodeWithParts(onBlockFace.Code)).BlockId;
			blockAccessor.SetBlock(blockId, blockpos);
			return true;
		}
		return false;
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		if (!CanVineStay(world, pos))
		{
			world.BlockAccessor.SetBlock(0, pos);
			world.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
		}
	}

	private bool CanVineStay(IWorldAccessor world, BlockPos pos)
	{
		BlockPos apos = pos.AddCopy(VineFacing.Opposite);
		if (!world.BlockAccessor.GetBlock(apos).CanAttachBlockAt(world.BlockAccessor, this, apos, VineFacing))
		{
			return world.BlockAccessor.GetBlock(pos.UpCopy()) is BlockVines;
		}
		return true;
	}

	public override AssetLocation GetRotatedBlockCode(int angle)
	{
		BlockFacing blockFacing = BlockFacing.FromCode(LastCodePart());
		int rotatedIndex = ((angle == 180) ? blockFacing.HorizontalAngleIndex : blockFacing.Opposite.HorizontalAngleIndex) + angle / 90;
		BlockFacing newFacing = BlockFacing.HORIZONTALS_ANGLEORDER[GameMath.Mod(rotatedIndex, 4)];
		return CodeWithParts(newFacing.Code);
	}

	public override bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object extra)
	{
		extra = null;
		if (offThreadRandom.NextDouble() > 0.1)
		{
			return false;
		}
		BlockFacing attachFace = VineFacing.Opposite;
		BlockPos npos = pos.AddCopy(attachFace);
		Block block = world.BlockAccessor.GetBlock(npos);
		if (block.CanAttachBlockAt(world.BlockAccessor, this, npos, VineFacing) || block is BlockLeaves)
		{
			return false;
		}
		npos.Set(pos);
		int i;
		for (i = 0; i < 5; i++)
		{
			npos.Y++;
			Block upblock = world.BlockAccessor.GetBlock(npos);
			if (upblock is BlockLeaves || upblock.CanAttachBlockAt(world.BlockAccessor, this, npos, BlockFacing.DOWN))
			{
				return false;
			}
			if (!(upblock is BlockVines))
			{
				break;
			}
			if (world.BlockAccessor.GetBlockOnSide(npos, attachFace).CanAttachBlockAt(world.BlockAccessor, this, npos, VineFacing))
			{
				return false;
			}
		}
		return i < 5;
	}

	public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
	{
		world.BlockAccessor.SetBlock(0, pos);
	}
}
