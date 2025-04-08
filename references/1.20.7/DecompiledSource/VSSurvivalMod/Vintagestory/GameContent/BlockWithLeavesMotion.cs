using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockWithLeavesMotion : Block
{
	public override void OnDecalTesselation(IWorldAccessor world, MeshData decalMesh, BlockPos pos)
	{
		if (VertexFlags.WindMode == EnumWindBitMode.NoWind)
		{
			return;
		}
		int sideDisableWindwave = 0;
		int groundOffset = 0;
		bool enableWind = api.World.BlockAccessor.GetLightLevel(pos, EnumLightLevelType.OnlySunLight) >= 14;
		if (enableWind)
		{
			for (int tileSide = 0; tileSide < 6; tileSide++)
			{
				BlockFacing facing = BlockFacing.ALLFACES[tileSide];
				Block nblock = world.BlockAccessor.GetBlock(pos.AddCopy(facing));
				if (nblock.BlockMaterial != EnumBlockMaterial.Leaves && nblock.SideSolid[BlockFacing.ALLFACES[tileSide].Opposite.Index])
				{
					sideDisableWindwave |= 1 << tileSide;
				}
			}
			for (groundOffset = 1; groundOffset < 8; groundOffset++)
			{
				Block block = api.World.BlockAccessor.GetBlockBelow(pos, groundOffset);
				if (block.VertexFlags.WindMode == EnumWindBitMode.NoWind && block.SideSolid[BlockFacing.UP.Index])
				{
					break;
				}
			}
		}
		decalMesh.ToggleWindModeSetWindData(sideDisableWindwave, enableWind, groundOffset);
	}

	public override void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
	{
		if (VertexFlags.WindMode == EnumWindBitMode.NoWind)
		{
			return;
		}
		bool enableWind = ((lightRgbsByCorner[24] >> 24) & 0xFF) >= 159;
		int groundOffset = 1;
		int sideDisableWindshear = 0;
		if (enableWind)
		{
			for (int tileSide = 0; tileSide < 6; tileSide++)
			{
				Block nblock = chunkExtBlocks[extIndex3d + TileSideEnum.MoveIndex[tileSide]];
				if (nblock.BlockMaterial != EnumBlockMaterial.Leaves && nblock.SideSolid[TileSideEnum.GetOpposite(tileSide)])
				{
					sideDisableWindshear |= 1 << tileSide;
				}
			}
			int downMoveIndex = TileSideEnum.MoveIndex[5];
			int movedIndex3d = extIndex3d + downMoveIndex;
			for (; groundOffset < 8; groundOffset++)
			{
				Block block = ((movedIndex3d < 0) ? api.World.BlockAccessor.GetBlockBelow(pos, groundOffset) : chunkExtBlocks[movedIndex3d]);
				if (block.VertexFlags.WindMode == EnumWindBitMode.NoWind && block.SideSolid[4])
				{
					break;
				}
				movedIndex3d += downMoveIndex;
			}
		}
		sourceMesh.ToggleWindModeSetWindData(sideDisableWindshear, enableWind, groundOffset);
	}
}
