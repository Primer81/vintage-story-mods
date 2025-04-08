using System.Runtime.CompilerServices;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf;

public class LiquidTesselator : IBlockTesselator
{
	private readonly int extChunkSize;

	private readonly Block[] extChunkDataFluids;

	private readonly Block[] extChunkDataBlocks;

	internal bool[] isLiquidBlock;

	private readonly int moveUp;

	private readonly int moveSouth;

	private readonly int moveNorthWest;

	private readonly int moveNorthEast;

	private readonly int moveSouthWest;

	private readonly int moveSouthEast;

	private readonly int moveAboveNorth;

	private readonly int moveAboveSouth;

	private readonly int moveAboveEast;

	private readonly int moveAboveWest;

	private const int byte0 = 2;

	private const int byte1 = 3;

	private int lavaFlag;

	private int extraFlags;

	private int chunksize;

	private BlockPos tmpPos = new BlockPos();

	private readonly float[] waterStillFlowVector = new float[8];

	private readonly float[] waterDownFlowVector = new float[8] { 0f, -1f, 0f, -1f, 0f, -1f, 0f, -1f };

	private readonly int[] shouldWave = new int[24];

	private float[] flowVectorsN;

	private float[] flowVectorsE;

	private float[] flowVectorsS;

	private float[] flowVectorsW;

	private float[] upFlowVectors = new float[8];

	private FastVec3f[] upQuadOffsets = new FastVec3f[8];

	public LiquidTesselator(ChunkTesselator tct)
	{
		chunksize = 32;
		extChunkSize = 34;
		extChunkDataFluids = tct.currentChunkFluidBlocksExt;
		extChunkDataBlocks = tct.currentChunkBlocksExt;
		moveUp = extChunkSize * extChunkSize;
		moveSouth = extChunkSize;
		moveNorthWest = -extChunkSize - 1;
		moveNorthEast = -extChunkSize + 1;
		moveSouthWest = extChunkSize - 1;
		moveSouthEast = extChunkSize + 1;
		moveAboveNorth = (extChunkSize - 1) * extChunkSize;
		moveAboveSouth = (extChunkSize + 1) * extChunkSize;
		moveAboveEast = extChunkSize * extChunkSize + 1;
		moveAboveWest = extChunkSize * extChunkSize - 1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool SideSolid(int extIndex3d, BlockFacing facing)
	{
		bool sideSolid = extChunkDataFluids[extIndex3d].SideSolid[facing.Index];
		if (!sideSolid)
		{
			sideSolid = extChunkDataBlocks[extIndex3d].SideSolid[facing.Index];
		}
		return sideSolid;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool IsSameLiquid(int extIndex3d)
	{
		return isLiquidBlock[extChunkDataFluids[extIndex3d].BlockId];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int SameLiquidLevelAt(int extIndex3d)
	{
		return extChunkDataFluids[extIndex3d].LiquidLevel;
	}

	public void Tesselate(TCTCache vars)
	{
		if (isLiquidBlock == null)
		{
			isLiquidBlock = vars.tct.isLiquidBlock;
		}
		int extIndex3d = vars.extIndex3d;
		int liquidLevel = vars.block.LiquidLevel;
		float waterLevel = ChunkTesselator.waterLevels[liquidLevel];
		IBlockFlowing blockFlowing = vars.block as IBlockFlowing;
		lavaFlag = ((blockFlowing != null && blockFlowing.IsLava) ? 33554432 : 0);
		extraFlags = 0;
		Block aboveLiquid = extChunkDataFluids[extIndex3d + moveUp];
		_ = extChunkDataFluids[extIndex3d - moveUp];
		Block belowBlock = extChunkDataBlocks[extIndex3d - moveUp];
		Block inBlock = extChunkDataBlocks[extIndex3d];
		upFlowVectors.Fill(0f);
		if (!belowBlock.SideSolid.OnSide(BlockFacing.UP) || belowBlock.Replaceable >= 6000)
		{
			flowVectorsN = (SideSolid(extIndex3d - moveSouth, BlockFacing.SOUTH) ? waterStillFlowVector : waterDownFlowVector);
			flowVectorsE = (SideSolid(extIndex3d + 1, BlockFacing.WEST) ? waterStillFlowVector : waterDownFlowVector);
			flowVectorsS = (SideSolid(extIndex3d + moveSouth, BlockFacing.NORTH) ? waterStillFlowVector : waterDownFlowVector);
			flowVectorsW = (SideSolid(extIndex3d - 1, BlockFacing.EAST) ? waterStillFlowVector : waterDownFlowVector);
		}
		else
		{
			flowVectorsN = (flowVectorsE = (flowVectorsS = (flowVectorsW = waterStillFlowVector)));
		}
		float northWestLevel = 1f;
		float southWestLevel = 1f;
		float northEastLevel = 1f;
		float southEastLevel = 1f;
		int waveNorthEast = 2;
		int waveNorthWest = 2;
		int waveSouthEast = 2;
		int waveSouthWest = 2;
		if (aboveLiquid.MatterState != EnumMatterState.Liquid)
		{
			if (liquidLevel == 7)
			{
				bool aboveWestWater = IsSameLiquid(extIndex3d + moveAboveWest);
				bool aboveEastWater = IsSameLiquid(extIndex3d + moveAboveEast);
				bool num = IsSameLiquid(extIndex3d + moveAboveSouth);
				bool aboveNorthWater = IsSameLiquid(extIndex3d + moveAboveNorth);
				bool aboveNorthEastWater = aboveNorthWater || aboveEastWater || IsSameLiquid(extIndex3d + moveAboveNorth + 1);
				bool aboveSouthEastWater = num || aboveEastWater || IsSameLiquid(extIndex3d + moveAboveSouth + 1);
				bool aboveSouthWestWater = num || aboveWestWater || IsSameLiquid(extIndex3d + moveAboveSouth - 1);
				bool num2 = aboveNorthWater || aboveWestWater || IsSameLiquid(extIndex3d + moveAboveNorth - 1);
				waveNorthEast = (aboveNorthEastWater ? 2 : 3);
				waveNorthWest = (num2 ? 2 : 3);
				waveSouthEast = (aboveSouthEastWater ? 2 : 3);
				waveSouthWest = (aboveSouthWestWater ? 2 : 3);
				northWestLevel = (num2 ? 1f : waterLevel);
				southWestLevel = (aboveSouthWestWater ? 1f : waterLevel);
				northEastLevel = (aboveNorthEastWater ? 1f : waterLevel);
				southEastLevel = (aboveSouthEastWater ? 1f : waterLevel);
				if (num2 && aboveSouthWestWater && aboveNorthEastWater && aboveSouthEastWater && !vars.tct.isPartiallyTransparent[aboveLiquid.BlockId] && aboveLiquid.SideOpaque[5] && vars.drawFaceFlags == 16)
				{
					return;
				}
				Vec3i normali = blockFlowing?.FlowNormali ?? null;
				if (normali != null)
				{
					float flowVectorX = (float)normali.X / 2f;
					float flowVectorZ = (float)normali.Z / 2f;
					upFlowVectors[0] = flowVectorX;
					upFlowVectors[1] = flowVectorZ;
					upFlowVectors[2] = flowVectorX;
					upFlowVectors[3] = flowVectorZ;
					upFlowVectors[4] = flowVectorX;
					upFlowVectors[5] = flowVectorZ;
					upFlowVectors[6] = flowVectorX;
					upFlowVectors[7] = flowVectorZ;
				}
			}
			else
			{
				int westWater = SameLiquidLevelAt(extIndex3d - 1);
				int eastWater = SameLiquidLevelAt(extIndex3d + 1);
				int southWater = SameLiquidLevelAt(extIndex3d + moveSouth);
				int northWater = SameLiquidLevelAt(extIndex3d - moveSouth);
				int nwWater = SameLiquidLevelAt(extIndex3d + moveNorthWest);
				int neWater = SameLiquidLevelAt(extIndex3d + moveNorthEast);
				int swWater = SameLiquidLevelAt(extIndex3d + moveSouthWest);
				int esWater = SameLiquidLevelAt(extIndex3d + moveSouthEast);
				int aboveWestWater2 = (IsSameLiquid(extIndex3d + moveAboveWest) ? 8 : 0);
				int aboveEastWater2 = (IsSameLiquid(extIndex3d + moveAboveEast) ? 8 : 0);
				int aboveSouthWater = (IsSameLiquid(extIndex3d + moveAboveSouth) ? 8 : 0);
				int aboveNorthWater2 = (IsSameLiquid(extIndex3d + moveAboveNorth) ? 8 : 0);
				int aboveNorthWestWater = (IsSameLiquid(extIndex3d + moveAboveNorth - 1) ? 8 : 0);
				int aboveNorthEastWater2 = (IsSameLiquid(extIndex3d + moveAboveNorth + 1) ? 8 : 0);
				int aboveSouthWestWater2 = (IsSameLiquid(extIndex3d + moveAboveSouth - 1) ? 8 : 0);
				int aboveSouthEastWater2 = (IsSameLiquid(extIndex3d + moveAboveSouth + 1) ? 8 : 0);
				northWestLevel = ChunkTesselator.waterLevels[GameMath.Max(liquidLevel, northWater, westWater, nwWater, aboveWestWater2, aboveNorthWater2, aboveNorthWestWater)];
				southWestLevel = ChunkTesselator.waterLevels[GameMath.Max(liquidLevel, southWater, westWater, swWater, aboveSouthWater, aboveWestWater2, aboveSouthWestWater2)];
				northEastLevel = ChunkTesselator.waterLevels[GameMath.Max(liquidLevel, northWater, eastWater, neWater, aboveEastWater2, aboveNorthWater2, aboveNorthEastWater2)];
				southEastLevel = ChunkTesselator.waterLevels[GameMath.Max(liquidLevel, southWater, eastWater, esWater, aboveEastWater2, aboveSouthWater, aboveSouthEastWater2)];
				waveNorthEast = ((northEastLevel < 1f && aboveNorthEastWater2 == 0 && aboveNorthWater2 == 0 && aboveEastWater2 == 0) ? 3 : 2);
				waveNorthWest = ((northWestLevel < 1f && aboveNorthWestWater == 0 && aboveWestWater2 == 0 && aboveNorthWater2 == 0) ? 3 : 2);
				waveSouthEast = ((southEastLevel < 1f && aboveSouthEastWater2 == 0 && aboveSouthWater == 0 && aboveEastWater2 == 0) ? 3 : 2);
				waveSouthWest = ((southWestLevel < 1f && aboveSouthWestWater2 == 0 && aboveSouthWater == 0 && aboveWestWater2 == 0) ? 3 : 2);
				Vec3i normali2 = blockFlowing?.FlowNormali ?? null;
				float flowVectorX2;
				float flowVectorZ2;
				if (normali2 != null)
				{
					flowVectorX2 = (float)normali2.X / 2f;
					flowVectorZ2 = (float)normali2.Z / 2f;
				}
				else
				{
					float nWestToEastFlow = Cmp(northWestLevel, northEastLevel);
					float sWestToEastFlow = Cmp(southWestLevel, southEastLevel);
					float num3 = Cmp(northWestLevel, southWestLevel);
					float eNorthToSouthFlow = Cmp(northEastLevel, southEastLevel);
					flowVectorX2 = nWestToEastFlow + sWestToEastFlow;
					flowVectorZ2 = num3 + eNorthToSouthFlow;
				}
				upFlowVectors[0] = flowVectorX2;
				upFlowVectors[1] = flowVectorZ2;
				upFlowVectors[2] = flowVectorX2;
				upFlowVectors[3] = flowVectorZ2;
				upFlowVectors[4] = flowVectorX2;
				upFlowVectors[5] = flowVectorZ2;
				upFlowVectors[6] = flowVectorX2;
				upFlowVectors[7] = flowVectorZ2;
			}
		}
		shouldWave[16] = waveSouthWest;
		shouldWave[17] = waveSouthEast;
		shouldWave[18] = waveNorthWest;
		shouldWave[19] = waveNorthEast;
		shouldWave[0] = waveNorthWest;
		shouldWave[1] = waveNorthEast;
		shouldWave[8] = waveSouthEast;
		shouldWave[9] = waveSouthWest;
		shouldWave[4] = waveNorthEast;
		shouldWave[5] = waveSouthEast;
		shouldWave[12] = waveSouthWest;
		shouldWave[13] = waveNorthWest;
		int verts = 0;
		int drawFaceFlags = vars.drawFaceFlags;
		MeshData[] meshPools = vars.tct.GetPoolForPass(EnumChunkRenderPass.Liquid, 1);
		bool num4 = (1 & drawFaceFlags) != 0;
		bool renderE = (2 & drawFaceFlags) != 0;
		bool renderS = (4 & drawFaceFlags) != 0;
		bool renderW = (8 & drawFaceFlags) != 0;
		bool nSolid = false;
		bool eSolid = false;
		bool wSolid = false;
		bool sSolid = false;
		if (inBlock.Id != 0)
		{
			tmpPos.Set(vars.posX, vars.posY, vars.posZ);
			nSolid = inBlock.SideIsSolid(tmpPos, BlockFacing.NORTH.Index);
			eSolid = inBlock.SideIsSolid(tmpPos, BlockFacing.EAST.Index);
			sSolid = inBlock.SideIsSolid(tmpPos, BlockFacing.SOUTH.Index);
			wSolid = inBlock.SideIsSolid(tmpPos, BlockFacing.WEST.Index);
		}
		if ((0x20u & (uint)drawFaceFlags) != 0)
		{
			vars.CalcBlockFaceLight(5, extIndex3d - moveUp);
			DrawLiquidBlockFace(vars, 5, 1f, 1f, 1f, 1f, vars.blockFaceVertices[5], upFlowVectors, 20, 1f, 1f, meshPools);
			verts += 4;
		}
		if ((0x10u & (uint)drawFaceFlags) != 0)
		{
			if (vars.block.LiquidLevel == 7 && vars.rainHeightMap[vars.posZ % chunksize * chunksize + vars.posX % chunksize] <= vars.posY)
			{
				extraFlags = 536870912;
			}
			vars.CalcBlockFaceLight(4, extIndex3d + moveUp);
			FastVec3f[] quadOffsetsUp = vars.blockFaceVertices[4];
			if (nSolid || eSolid || sSolid || wSolid)
			{
				float i = (nSolid ? 0.01f : 0f);
				float e = (eSolid ? 0.99f : 1f);
				float s = (sSolid ? 0.99f : 1f);
				float w = (wSolid ? 0.01f : 0f);
				upQuadOffsets[4] = new FastVec3f(e, 1f, s);
				upQuadOffsets[5] = new FastVec3f(w, 1f, s);
				upQuadOffsets[6] = new FastVec3f(e, 1f, i);
				upQuadOffsets[7] = new FastVec3f(w, 1f, i);
				quadOffsetsUp = upQuadOffsets;
			}
			DrawLiquidBlockFace(vars, 4, southEastLevel, southWestLevel, northEastLevel, northWestLevel, quadOffsetsUp, upFlowVectors, 16, 1f, 1f, meshPools);
			verts += 4;
			extraFlags = 0;
		}
		if (num4 && !nSolid)
		{
			vars.CalcBlockFaceLight(0, extIndex3d - moveSouth);
			DrawLiquidBlockFace(vars, 0, northEastLevel, northWestLevel, 0f, 0f, vars.blockFaceVertices[0], flowVectorsN, 0, northWestLevel, northEastLevel, meshPools);
			verts += 4;
		}
		if (renderE && !eSolid)
		{
			vars.CalcBlockFaceLight(1, extIndex3d + 1);
			DrawLiquidBlockFace(vars, 1, southEastLevel, northEastLevel, 0f, 0f, vars.blockFaceVertices[1], flowVectorsE, 4, northEastLevel, southEastLevel, meshPools);
			verts += 4;
		}
		if (renderW && !wSolid)
		{
			vars.CalcBlockFaceLight(3, extIndex3d - 1);
			DrawLiquidBlockFace(vars, 3, northWestLevel, southWestLevel, 0f, 0f, vars.blockFaceVertices[3], flowVectorsW, 12, northWestLevel, southWestLevel, meshPools);
			verts += 4;
		}
		if (renderS && !sSolid)
		{
			vars.CalcBlockFaceLight(2, extIndex3d + moveSouth);
			DrawLiquidBlockFace(vars, 2, southWestLevel, southEastLevel, 0f, 0f, vars.blockFaceVertices[2], flowVectorsS, 8, southWestLevel, southEastLevel, meshPools);
			verts += 4;
		}
	}

	private void DrawLiquidBlockFace(TCTCache vars, int tileSide, float northSouthLevel, float southWestLevel, float northEastLevel, float southEastLevel, FastVec3f[] quadOffsets, float[] flowVectors, int shouldWaveOffset, float texHeightLeftRel, float texHeightRightRel, MeshData[] meshPools)
	{
		int colorMapDataValue = vars.ColorMapData.Value;
		bool num = vars.RenderPass == EnumChunkRenderPass.Liquid;
		int textureSubId = vars.fastBlockTextureSubidsByFace[tileSide];
		TextureAtlasPosition texPos = vars.textureAtlasPositionsByTextureSubId[textureSubId];
		MeshData toreturn = meshPools[texPos.atlasNumber];
		int lastelement = toreturn.VerticesCount;
		float maxTexHeight = texPos.y2 - texPos.y1;
		int flags = vars.VertexFlags | BlockFacing.AllVertexFlagsNormals[tileSide] | extraFlags;
		float x = vars.lx;
		float y = vars.ly;
		float z = vars.lz;
		FastVec3f tmpv = quadOffsets[5];
		toreturn.AddVertexWithFlags(x + tmpv.X, y + tmpv.Y * southWestLevel, z + tmpv.Z, texPos.x2, texPos.y1, vars.CurrentLightRGBByCorner[1], flags);
		if (num)
		{
			toreturn.CustomInts.Add(shouldWave[shouldWaveOffset] | 0xFF00 | lavaFlag);
		}
		toreturn.CustomInts.Add(colorMapDataValue);
		tmpv = quadOffsets[4];
		toreturn.AddVertexWithFlags(x + tmpv.X, y + tmpv.Y * northSouthLevel, z + tmpv.Z, texPos.x1, texPos.y1, vars.CurrentLightRGBByCorner[0], flags);
		if (num)
		{
			toreturn.CustomInts.Add(shouldWave[shouldWaveOffset + 1] | lavaFlag);
		}
		toreturn.CustomInts.Add(colorMapDataValue);
		tmpv = quadOffsets[7];
		toreturn.AddVertexWithFlags(x + tmpv.X, y + tmpv.Y * southEastLevel, z + tmpv.Z, texPos.x2, texPos.y1 + maxTexHeight * texHeightLeftRel, vars.CurrentLightRGBByCorner[3], flags);
		if (num)
		{
			byte height2 = (byte)(texHeightLeftRel * 255f);
			toreturn.CustomInts.Add(shouldWave[shouldWaveOffset + 2] | 0xFF00 | (height2 << 16) | lavaFlag);
		}
		toreturn.CustomInts.Add(colorMapDataValue);
		tmpv = quadOffsets[6];
		toreturn.AddVertexWithFlags(x + tmpv.X, y + tmpv.Y * northEastLevel, z + tmpv.Z, texPos.x1, texPos.y1 + maxTexHeight * texHeightRightRel, vars.CurrentLightRGBByCorner[2], flags);
		if (num)
		{
			byte height = (byte)(texHeightRightRel * 255f);
			toreturn.CustomInts.Add(shouldWave[shouldWaveOffset + 3] | (height << 16) | lavaFlag);
			toreturn.CustomFloats.Add(flowVectors);
		}
		toreturn.CustomInts.Add(colorMapDataValue);
		toreturn.AddIndices(lastelement, lastelement + 1, lastelement + 2, lastelement + 1, lastelement + 3, lastelement + 2);
		vars.UpdateChunkMinMax(x, y, z);
		vars.UpdateChunkMinMax(x + 1f, y + 1f, z + 1f);
	}

	private float Cmp(float val1, float val2)
	{
		if (!(val1 > val2))
		{
			if (val2 != val1)
			{
				return -0.5f;
			}
			return 0f;
		}
		return 0.5f;
	}
}
