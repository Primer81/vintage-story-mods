using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class JsonTesselator : IBlockTesselator
{
	public const int DisableRandomsFlag = 1024;

	public const long Darkness = 4934475L;

	public TerrainMesherHelper helper = new TerrainMesherHelper();

	private BlockPos pos = new BlockPos();

	private int[] jsonLightRGB = new int[25];

	public static float[] reusableIdentityMatrix = Mat4f.Create();

	private float[] floatpool = new float[3];

	private int[] windFlagsMask = new int[64];

	private int[] windFlagsSet = new int[64];

	private static int[] faceCoordLookup = new int[6] { 2, 0, 2, 0, 1, 1 };

	private float[] tmpCoords = new float[3];

	private static int[][] axesByFacingLookup = new int[6][]
	{
		new int[2] { 0, 1 },
		new int[2] { 1, 2 },
		new int[2] { 0, 1 },
		new int[2] { 1, 2 },
		new int[2] { 0, 2 },
		new int[2] { 0, 2 }
	};

	private static int[][] indexesByFacingLookup = new int[6][]
	{
		new int[4] { 3, 2, 1, 0 },
		new int[4] { 3, 1, 2, 0 },
		new int[4] { 2, 3, 0, 1 },
		new int[4] { 2, 0, 3, 1 },
		new int[4] { 3, 2, 1, 0 },
		new int[4] { 1, 0, 3, 2 }
	};

	[ThreadStatic]
	private static float[] reusableFloatArray;

	[ThreadStatic]
	private static int[] reusableIntArray;

	public JsonTesselator()
	{
		helper.tess = this;
	}

	public long SetUpLightRGBs(TCTCache vars)
	{
		int extIndex3d = vars.extIndex3d;
		int[] currentLightRGBByCorner = vars.CurrentLightRGBByCorner;
		int lightRGBindex = 0;
		long totalLight = 0L;
		for (int tileSide = 0; tileSide < 6; tileSide++)
		{
			totalLight += vars.CalcBlockFaceLight(tileSide, extIndex3d + TileSideEnum.MoveIndex[tileSide]);
			jsonLightRGB[lightRGBindex++] = currentLightRGBByCorner[0];
			jsonLightRGB[lightRGBindex++] = currentLightRGBByCorner[1];
			jsonLightRGB[lightRGBindex++] = currentLightRGBByCorner[2];
			jsonLightRGB[lightRGBindex++] = currentLightRGBByCorner[3];
		}
		return totalLight + (jsonLightRGB[24] = vars.tct.currentChunkRgbsExt[extIndex3d]);
	}

	public void Tesselate(TCTCache vars)
	{
		int extIndex3d = vars.extIndex3d;
		long totalLight = SetUpLightRGBs(vars);
		helper.vars = vars;
		BlockEntity be = null;
		pos.SetDimension(vars.dimension);
		pos.Set(vars.posX, vars.posY, vars.posZ);
		Dictionary<BlockPos, BlockEntity> blockEntitiesOfChunk = vars.blockEntitiesOfChunk;
		if (blockEntitiesOfChunk != null && blockEntitiesOfChunk.TryGetValue(pos, out be))
		{
			try
			{
				if (be != null && be.OnTesselation(helper, vars.tct.offthreadTesselator))
				{
					return;
				}
			}
			catch (Exception e)
			{
				vars.tct.game.Logger.Error("Exception thrown during OnTesselation() of block entity {0}@{1}/{2}/{3}. Block will probably not be rendered as intended.", be, vars.posX, vars.posY, vars.posZ);
				vars.tct.game.Logger.Error(e);
			}
		}
		bool doNotRenderAtLod2 = vars.block.DoNotRenderAtLod2;
		if (vars.block.Lod0Shape != null)
		{
			if (NotSurrounded(vars, extIndex3d))
			{
				doMesh(vars, vars.block.Lod0Mesh, 0);
			}
			else
			{
				doNotRenderAtLod2 = true;
			}
		}
		if (totalLight == 4934475)
		{
			doMesh(vars, vars.shapes.blockModelDatas[vars.blockId], 0);
			return;
		}
		if (vars.block.Lod2Mesh == null)
		{
			doMesh(vars, vars.shapes.blockModelDatas[vars.blockId], (!doNotRenderAtLod2) ? 1 : 2);
			return;
		}
		doMesh(vars, vars.shapes.blockModelDatas[vars.blockId], 2);
		doMesh(vars, vars.block.Lod2Mesh, 3);
	}

	public static bool NotSurrounded(TCTCache vars, int extIndex3d)
	{
		if (vars.block.FaceCullMode != EnumFaceCullMode.CollapseMaterial)
		{
			return true;
		}
		for (int tileSide = 0; tileSide < 5; tileSide++)
		{
			Block nblock = vars.tct.currentChunkBlocksExt[extIndex3d + TileSideEnum.MoveIndex[tileSide]];
			if (nblock.BlockMaterial != vars.block.BlockMaterial && !nblock.SideOpaque[TileSideEnum.GetOpposite(tileSide)])
			{
				return true;
			}
		}
		return false;
	}

	public void doMesh(TCTCache vars, MeshData sourceMesh, int lodLevel)
	{
		if (sourceMesh.VerticesCount == 0)
		{
			return;
		}
		if (sourceMesh == null)
		{
			vars.block.DrawType = EnumDrawType.Cube;
			return;
		}
		MeshData[] altModels = (((lodLevel + 1) / 2 == 1) ? vars.shapes.altblockModelDatasLod1[vars.blockId] : ((lodLevel == 0) ? vars.shapes.altblockModelDatasLod0[vars.blockId] : vars.shapes.altblockModelDatasLod2[vars.blockId]));
		if (altModels != null)
		{
			int rnd = GameMath.MurmurHash3Mod(vars.posX, (vars.block.RandomizeAxes == EnumRandomizeAxes.XYZ) ? vars.posY : 0, vars.posZ, altModels.Length);
			sourceMesh = altModels[rnd];
		}
		vars.block.OnJsonTesselation(ref sourceMesh, ref jsonLightRGB, pos, vars.tct.currentChunkBlocksExt, vars.extIndex3d);
		if (vars.preRotationMatrix != null)
		{
			AddJsonModelDataToMesh(sourceMesh, lodLevel, vars, helper, vars.preRotationMatrix);
		}
		else if (vars.block.RandomizeRotations || vars.block.RandomSizeAdjust != 0f)
		{
			float[] matrix = (vars.block.RandomizeRotations ? TesselationMetaData.randomRotMatrices[GameMath.MurmurHash3Mod(-vars.posX, (vars.block.RandomizeAxes == EnumRandomizeAxes.XYZ) ? vars.posY : 0, vars.posZ, TesselationMetaData.randomRotations.Length)] : reusableIdentityMatrix);
			AddJsonModelDataToMesh(sourceMesh, lodLevel, vars, helper, matrix);
		}
		else
		{
			AddJsonModelDataToMesh(sourceMesh, lodLevel, vars, helper, null);
		}
	}

	public void AddJsonModelDataToMesh(MeshData sourceMesh, int lodlevel, TCTCache vars, IMeshPoolSupplier poolSupplier, float[] tfMatrix)
	{
		if (sourceMesh.VerticesCount > windFlagsMask.Length)
		{
			windFlagsMask = new int[sourceMesh.VerticesCount];
			windFlagsSet = new int[sourceMesh.VerticesCount];
		}
		for (int i = 0; i < sourceMesh.VerticesCount; i++)
		{
			windFlagsMask[i] = -1;
			windFlagsSet[i] = 0;
		}
		Block fluidBlock = vars.tct.currentChunkFluidBlocksExt[vars.extIndex3d + vars.block.IceCheckOffset * 34 * 34];
		if (fluidBlock.BlockId != 0)
		{
			AdjustWindWaveForFluids(sourceMesh, fluidBlock);
		}
		float xOff = vars.finalX;
		float yOff = vars.finalY;
		float zOff = vars.finalZ;
		int baseRenderFlags = vars.VertexFlags;
		int drawFaceFlags = vars.drawFaceFlags;
		bool frostable = vars.block.Frostable;
		byte colorLightA = 0;
		byte colorLightB = 0;
		byte colorLightG = 0;
		byte colorLightR = 0;
		float aoBright = 1f;
		int di = 0;
		byte temperature = vars.ColorMapData.Temperature;
		byte rainfall = vars.ColorMapData.Rainfall;
		int prevRenderPass = -2;
		int prevTextureId = -1;
		MeshData targetMesh = null;
		int colorTopLeft = 0;
		int colorTopRight = 0;
		int colorBottomLeft = 0;
		int colorBottomRight = 0;
		float[] tmpCoords = this.tmpCoords;
		float[] sourceMeshXYZ = sourceMesh.xyz;
		float[] sourceMeshUV = sourceMesh.Uv;
		int[] sourceMeshFlags = sourceMesh.Flags;
		int[] sourceMeshIndices = sourceMesh.Indices;
		float[] targetMeshXYZ = null;
		float[] targetMeshUV = null;
		byte[] targetMeshRgba = null;
		int[] targetMeshFlags = null;
		CustomMeshDataPartInt targetMeshCustomInts = null;
		float[] rotatedXyz = sourceMesh.xyz;
		int[] rotatedFlags = sourceMesh.Flags;
		if (poolSupplier == null)
		{
			poolSupplier = vars.tct;
		}
		int vertPerFace = sourceMesh.VerticesPerFace;
		int indPerFace = sourceMesh.IndicesPerFace;
		if (tfMatrix != null)
		{
			int sourceMeshArrayCount = sourceMesh.VerticesCount * 3;
			rotatedXyz = NewFloatArray(sourceMeshArrayCount);
			float randomScalefactor = vars.block.RandomSizeAdjust;
			bool doRandomYScale = false;
			if (randomScalefactor != 0f)
			{
				if (randomScalefactor < 0f)
				{
					randomScalefactor = 0f - randomScalefactor;
				}
				else
				{
					doRandomYScale = true;
				}
				int elementHeight = (int)randomScalefactor;
				randomScalefactor = randomScalefactor % 1f * ((float)GameMath.MurmurHash3Mod(-vars.posX, 0, vars.posZ, 2000) / 1000f - 1f);
				yOff += (float)elementHeight * randomScalefactor;
			}
			for (int l = 0; l < sourceMeshArrayCount; l += 3)
			{
				if (randomScalefactor == 0f)
				{
					Mat4f.MulWithVec3_Position(tfMatrix, sourceMeshXYZ, rotatedXyz, l);
				}
				else if (doRandomYScale)
				{
					Mat4f.MulWithVec3_Position_AndScale(tfMatrix, sourceMeshXYZ, rotatedXyz, l, 1f + randomScalefactor);
				}
				else
				{
					Mat4f.MulWithVec3_Position_AndScaleXY(tfMatrix, sourceMeshXYZ, rotatedXyz, l, 1f + randomScalefactor);
				}
			}
			sourceMeshArrayCount = sourceMesh.FlagsCount;
			rotatedFlags = sourceMeshFlags;
			if (rotatedFlags != null && tfMatrix != reusableIdentityMatrix)
			{
				rotatedFlags = NewIntArray(sourceMeshArrayCount);
				for (int k = 0; k < sourceMeshArrayCount; k++)
				{
					int flagOrig = sourceMeshFlags[k];
					VertexFlags.UnpackNormal(flagOrig, floatpool);
					Mat4f.MulWithVec3(tfMatrix, floatpool, floatpool);
					float len = GameMath.RootSumOfSquares(floatpool[0], floatpool[1], floatpool[2]);
					rotatedFlags[k] = (flagOrig & -33546241) | VertexFlags.PackNormal(floatpool[0] / len, floatpool[1] / len, floatpool[2] / len);
				}
			}
		}
		int sourceMeshXyzFacesCount = sourceMesh.XyzFacesCount;
		short[] renderPassesAndExtraBits = sourceMesh.RenderPassesAndExtraBits;
		bool haveRenderPasses = renderPassesAndExtraBits != null && renderPassesAndExtraBits.Length != 0;
		bool prevRotatedSource = tfMatrix == null;
		for (int j = 0; j < sourceMeshXyzFacesCount; j++)
		{
			int textureid = sourceMesh.TextureIds[sourceMesh.TextureIndices[j * vertPerFace / sourceMesh.VerticesPerFace]];
			float currentXOff = xOff;
			float currentZOff = zOff;
			bool rotatedSource = true;
			int renderpass;
			if (haveRenderPasses)
			{
				renderpass = sourceMesh.RenderPassesAndExtraBits[j];
				if (renderpass >= 1024)
				{
					renderpass &= 0x3FF;
					rotatedSource = false;
					currentXOff = (int)(xOff + 0.5f);
					currentZOff = (int)(zOff + 0.5f);
				}
			}
			else
			{
				renderpass = -1;
			}
			if (rotatedSource != prevRotatedSource)
			{
				prevRotatedSource = rotatedSource;
				sourceMeshXYZ = (rotatedSource ? rotatedXyz : sourceMesh.xyz);
				sourceMeshFlags = (rotatedSource ? rotatedFlags : sourceMesh.Flags);
			}
			int currentFace = sourceMesh.XyzFaces[j] - 1;
			if (currentFace >= 0)
			{
				if (tfMatrix != null && rotatedSource && tfMatrix != reusableIdentityMatrix)
				{
					currentFace = Mat4f.MulWithVec3_BlockFacing(tfMatrix, BlockFacing.ALLFACES[currentFace].Normalf).Index;
				}
				if (((1 << currentFace) & drawFaceFlags) == 0)
				{
					int centerIndex = j * 4 * 3 + faceCoordLookup[currentFace];
					float centerCoord = sourceMeshXYZ[centerIndex];
					if (currentFace == 1 || currentFace == 2 || currentFace == 4)
					{
						centerCoord = 1f - centerCoord;
					}
					if (centerCoord <= 0.01f)
					{
						centerCoord = sourceMeshXYZ[centerIndex + 6];
						if (currentFace == 1 || currentFace == 2 || currentFace == 4)
						{
							centerCoord = 1f - centerCoord;
						}
						if (centerCoord <= 0.01f)
						{
							bool withinBlockBounds = true;
							for (int m = 0; m < 9; m++)
							{
								if (m == 3)
								{
									m += 3;
								}
								int ix = j * 4 * 3 + m;
								if (ix != centerIndex && ix != centerIndex + 6)
								{
									centerCoord = sourceMeshXYZ[ix];
									if (centerCoord < -0.0001f || centerCoord > 1.0001f)
									{
										withinBlockBounds = false;
										break;
									}
								}
							}
							if (withinBlockBounds)
							{
								di += indPerFace;
								continue;
							}
						}
					}
				}
			}
			bool isLiquid = renderpass == 4;
			bool istopsoil = renderpass == 5;
			if (prevRenderPass != renderpass || prevTextureId != textureid)
			{
				targetMesh = poolSupplier.GetMeshPoolForPass(textureid, (renderpass >= 0) ? ((EnumChunkRenderPass)renderpass) : vars.RenderPass, lodlevel);
				targetMeshXYZ = targetMesh.xyz;
				targetMeshUV = targetMesh.Uv;
				targetMeshRgba = targetMesh.Rgba;
				targetMeshCustomInts = targetMesh.CustomInts;
				targetMeshFlags = targetMesh.Flags;
			}
			prevRenderPass = renderpass;
			prevTextureId = textureid;
			int skippedIndices = (j - di / 6) * vertPerFace + 2 * di / 3;
			int colorMapDataCalculated = ColorMapData.FromValues(sourceMesh.SeasonColorMapIds[j], sourceMesh.ClimateColorMapIds[j], temperature, rainfall, (sourceMesh.FrostableBits != null) ? sourceMesh.FrostableBits[j] : frostable, vars.block.ExtraColorBits);
			int[] axesByFacing = null;
			float textureVOffset = 0f;
			if (currentFace < 0)
			{
				int num = jsonLightRGB[24];
				colorLightA = (byte)((float)(num & 0xFF) * aoBright);
				colorLightB = (byte)(num >> 8);
				colorLightG = (byte)(num >> 16);
				colorLightR = (byte)(num >> 24);
				aoBright = 1f;
			}
			else
			{
				axesByFacing = axesByFacingLookup[currentFace];
				int[] indexes = indexesByFacingLookup[currentFace];
				int baseIndex = currentFace * 4;
				colorTopLeft = jsonLightRGB[baseIndex + indexes[0]];
				colorTopRight = jsonLightRGB[baseIndex + indexes[1]];
				colorBottomLeft = jsonLightRGB[baseIndex + indexes[2]];
				colorBottomRight = jsonLightRGB[baseIndex + indexes[3]];
				if (vars.textureVOffset != 0f && ((1 << currentFace) & vars.block.alternatingVOffsetFaces) == 0)
				{
					textureVOffset = vars.textureVOffset / ((float)ClientSettings.MaxTextureAtlasHeight / 32f);
				}
			}
			int sourceVertexNum = j * vertPerFace;
			int targetVertexNum = targetMesh.VerticesCount;
			int lastelement = targetVertexNum - skippedIndices;
			int targetVertexIndex = targetVertexNum * 3;
			int sourceVertexIndex = sourceVertexNum * 3;
			int targetUVIndex = targetVertexNum * 2;
			int sourceUVIndex = sourceVertexNum * 2;
			int targetRGBAIndex = targetVertexNum * 4;
			int n = vertPerFace;
			do
			{
				if (targetVertexNum >= targetMesh.VerticesMax)
				{
					targetMesh.VerticesCount = targetVertexNum;
					targetMesh.GrowVertexBuffer();
					targetMesh.GrowNormalsBuffer();
					targetMeshXYZ = targetMesh.xyz;
					targetMeshUV = targetMesh.Uv;
					targetMeshRgba = targetMesh.Rgba;
					targetMeshCustomInts = targetMesh.CustomInts;
					targetMeshFlags = targetMesh.Flags;
				}
				vars.UpdateChunkMinMax(targetMeshXYZ[targetVertexIndex++] = (tmpCoords[0] = sourceMeshXYZ[sourceVertexIndex++]) + currentXOff, targetMeshXYZ[targetVertexIndex++] = (tmpCoords[1] = sourceMeshXYZ[sourceVertexIndex++]) + yOff, targetMeshXYZ[targetVertexIndex++] = (tmpCoords[2] = sourceMeshXYZ[sourceVertexIndex++]) + currentZOff);
				targetMeshUV[targetUVIndex++] = sourceMeshUV[sourceUVIndex++];
				targetMeshUV[targetUVIndex++] = sourceMeshUV[sourceUVIndex++] + textureVOffset;
				float f = 1f;
				if (vars.block.DrawType == EnumDrawType.JSONAndWater && tmpCoords[1] < 1f)
				{
					f = Math.Max(0.6f, tmpCoords[1]) - 0.1f;
				}
				if (currentFace >= 0)
				{
					float lx = GameMath.Clamp(tmpCoords[axesByFacing[0]], 0f, 1f);
					float dy = GameMath.Clamp(tmpCoords[axesByFacing[1]], 0f, 1f);
					int num2 = GameMath.BiLerpRgbaColor(lx, dy, colorTopLeft, colorTopRight, colorBottomLeft, colorBottomRight);
					colorLightA = (byte)((uint)num2 & 0xFFu);
					colorLightB = (byte)(num2 >> 8);
					colorLightG = (byte)(num2 >> 16);
					colorLightR = (byte)((float)(num2 >> 24) * aoBright);
				}
				if (isLiquid)
				{
					if (sourceMesh.CustomFloats == null)
					{
						targetMeshCustomInts.Add(67108864);
						targetMeshCustomInts.Add(colorMapDataCalculated);
						targetMesh.CustomFloats.Add(0f);
						targetMesh.CustomFloats.Add(0f);
					}
					else
					{
						targetMeshCustomInts.Add(sourceMesh.CustomInts.Values[sourceVertexNum]);
						targetMeshCustomInts.Add(colorMapDataCalculated);
						targetMesh.CustomFloats.Add(sourceMesh.CustomFloats.Values[2 * sourceVertexNum]);
						targetMesh.CustomFloats.Add(sourceMesh.CustomFloats.Values[2 * sourceVertexNum + 1]);
					}
				}
				else
				{
					targetMeshCustomInts.Add(colorMapDataCalculated);
					if (istopsoil)
					{
						targetMesh.CustomFloats.Add(sourceMesh.CustomFloats.Values[2 * sourceVertexNum]);
						targetMesh.CustomFloats.Add(sourceMesh.CustomFloats.Values[2 * sourceVertexNum + 1]);
					}
				}
				targetMeshRgba[targetRGBAIndex++] = (byte)((float)(int)colorLightA * f);
				targetMeshRgba[targetRGBAIndex++] = (byte)((float)(int)colorLightB * f);
				targetMeshRgba[targetRGBAIndex++] = (byte)((float)(int)colorLightG * f);
				targetMeshRgba[targetRGBAIndex++] = (byte)((float)(int)colorLightR * f);
				targetMeshFlags[targetVertexNum++] = (baseRenderFlags | sourceMeshFlags[sourceVertexNum] | windFlagsSet[sourceVertexNum]) & windFlagsMask[sourceVertexNum];
				sourceVertexNum++;
			}
			while (--n > 0);
			targetMesh.VerticesCount = targetVertexNum;
			if (indPerFace == 6)
			{
				int indexNum = j * indPerFace;
				targetMesh.AddIndices(lastelement + sourceMeshIndices[indexNum++], lastelement + sourceMeshIndices[indexNum++], lastelement + sourceMeshIndices[indexNum++], lastelement + sourceMeshIndices[indexNum++], lastelement + sourceMeshIndices[indexNum++], lastelement + sourceMeshIndices[indexNum]);
				continue;
			}
			int indexNum2 = j * indPerFace;
			for (int l2 = 0; l2 < indPerFace; l2++)
			{
				targetMesh.AddIndex(lastelement + sourceMeshIndices[indexNum2++]);
			}
		}
	}

	private void AdjustWindWaveForFluids(MeshData sourceMesh, Block fluidBlock)
	{
		int clearFlags = -503316481;
		int verticesCount = sourceMesh.VerticesCount;
		int[] windFlagsMask = this.windFlagsMask;
		if (fluidBlock.SideSolid.Any)
		{
			for (int vertexNum = 0; vertexNum < verticesCount; vertexNum++)
			{
				windFlagsMask[vertexNum] = clearFlags;
			}
			return;
		}
		int[] windFlagsSet = this.windFlagsSet;
		int[] sourceMeshFlags = sourceMesh.Flags;
		for (int vertexNum2 = 0; vertexNum2 < verticesCount; vertexNum2++)
		{
			int flags = sourceMeshFlags[vertexNum2] & 0x1E000000;
			if (flags == 33554432 || flags == 100663296)
			{
				windFlagsSet[vertexNum2] = 738197504;
				windFlagsMask[vertexNum2] = -1073741825;
			}
		}
	}

	private static float[] NewFloatArray(int size)
	{
		if (reusableFloatArray == null || reusableFloatArray.Length < size)
		{
			reusableFloatArray = new float[size];
		}
		return reusableFloatArray;
	}

	private static int[] NewIntArray(int size)
	{
		if (reusableIntArray == null || reusableIntArray.Length < size)
		{
			reusableIntArray = new int[size];
		}
		return reusableIntArray;
	}
}
