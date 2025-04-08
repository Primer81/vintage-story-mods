using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class CrossAndSnowlayerTesselator : IBlockTesselator
{
	private float blockheight;

	public CrossAndSnowlayerTesselator(float blockheight)
	{
		this.blockheight = blockheight;
	}

	public void Tesselate(TCTCache vars)
	{
		bool belowBlockSolid = false;
		int extIndex = vars.extIndex3d + TileSideEnum.MoveIndex[5];
		belowBlockSolid = vars.tct.currentChunkFluidBlocksExt[extIndex].SideSolid[BlockFacing.UP.Index];
		if (!belowBlockSolid)
		{
			belowBlockSolid = vars.tct.currentChunkBlocksExt[extIndex].SideSolid[BlockFacing.UP.Index];
		}
		if (belowBlockSolid)
		{
			float saveRandomOffetX = vars.finalX;
			float saveRandomOffetZ = vars.finalZ;
			vars.finalX = vars.lx;
			vars.finalZ = vars.lz;
			int snowLayerVertexFlags = vars.VertexFlags & 0x1FFFFFF & -1793;
			MeshData[] meshPools = vars.tct.GetPoolForPass(EnumChunkRenderPass.Opaque, 1);
			for (int tileSide = 0; tileSide < 6; tileSide++)
			{
				if ((vars.drawFaceFlags & TileSideEnum.ToFlags(tileSide)) != 0)
				{
					vars.CalcBlockFaceLight(tileSide, vars.extIndex3d + TileSideEnum.MoveIndex[tileSide]);
					CubeTesselator.DrawBlockFace(vars, tileSide, vars.blockFaceVertices[tileSide], vars.textureAtlasPositionsByTextureSubId[vars.fastBlockTextureSubidsByFace[6]], snowLayerVertexFlags | BlockFacing.ALLFACES[tileSide].NormalPackedFlags, 0, meshPools, blockheight);
				}
			}
			vars.finalX = saveRandomOffetX;
			vars.finalZ = saveRandomOffetZ;
		}
		vars.drawFaceFlags = 3;
		CrossTesselator.DrawCross(vars, 1.41f);
	}
}
