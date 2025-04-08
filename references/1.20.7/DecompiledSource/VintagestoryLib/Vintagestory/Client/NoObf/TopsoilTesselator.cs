using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class TopsoilTesselator : IBlockTesselator
{
	public void Tesselate(TCTCache vars)
	{
		int textureSubIdSecond = vars.fastBlockTextureSubidsByFace[6];
		int drawFaceFlags = vars.drawFaceFlags;
		int allFlags = vars.VertexFlags;
		int colorMapDataValue = vars.ColorMapData.Value;
		uint randomSelector = 0u;
		BakedCompositeTexture[][] textures = null;
		bool hasAlternates;
		if (hasAlternates = vars.block.HasAlternates)
		{
			textures = vars.block.FastTextureVariants;
			randomSelector = GameMath.oaatHashU(vars.posX, vars.posY, vars.posZ);
		}
		int verts = 0;
		Block aboveBlock = vars.tct.currentChunkBlocksExt[vars.extIndex3d + 1156];
		if ((aboveBlock.BlockMaterial == EnumBlockMaterial.Snow || aboveBlock.snowLevel > 0f) && vars.block.Textures.TryGetValue("snowed", out var ct))
		{
			textureSubIdSecond = ct.Baked.TextureSubId;
			colorMapDataValue = 0;
		}
		int randTop = GameMath.MurmurHash3Mod(vars.posX, vars.posY, vars.posZ, 4);
		MeshData[] meshPools = vars.tct.GetPoolForPass(vars.RenderPass, 1);
		for (int tileSide = 0; tileSide < 6; tileSide++)
		{
			if ((drawFaceFlags & TileSideEnum.ToFlags(tileSide)) == 0)
			{
				continue;
			}
			vars.CalcBlockFaceLight(tileSide, vars.extIndex3d + TileSideEnum.MoveIndex[tileSide]);
			int textureSubId;
			if (hasAlternates)
			{
				BakedCompositeTexture[] variants = textures[tileSide];
				if (variants != null)
				{
					textureSubId = variants[randomSelector % variants.Length].TextureSubId;
					goto IL_0149;
				}
			}
			textureSubId = vars.fastBlockTextureSubidsByFace[tileSide];
			goto IL_0149;
			IL_0149:
			int rotIndex = ((tileSide == 4) ? randTop : 0);
			DrawBlockFaceTopSoil(vars, allFlags | BlockFacing.ALLFACES[tileSide].NormalPackedFlags, vars.blockFaceVertices[tileSide], colorMapDataValue, textureSubId, textureSubIdSecond, meshPools, rotIndex);
			verts += 4;
		}
	}

	private void DrawBlockFaceTopSoil(TCTCache vars, int flags, FastVec3f[] quadOffsets, int colorMapDataValue, int textureSubId, int textureSubIdSecond, MeshData[] meshPools, int rotIndex)
	{
		TextureAtlasPosition texPos = vars.textureAtlasPositionsByTextureSubId[textureSubId];
		TextureAtlasPosition texPosSecond = vars.textureAtlasPositionsByTextureSubId[textureSubIdSecond];
		MeshData toreturn = meshPools[texPos.atlasNumber];
		int lastelement = toreturn.VerticesCount;
		float x = vars.lx;
		float y = vars.ly;
		float z = vars.lz;
		FastVec3f tmpv = quadOffsets[5];
		toreturn.AddVertexWithFlags(x + tmpv.X, y + tmpv.Y, z + tmpv.Z, texPos.x2, texPos.y1, vars.CurrentLightRGBByCorner[1], flags);
		tmpv = quadOffsets[4];
		toreturn.AddVertexWithFlags(x + tmpv.X, y + tmpv.Y, z + tmpv.Z, texPos.x1, texPos.y1, vars.CurrentLightRGBByCorner[0], flags);
		tmpv = quadOffsets[7];
		toreturn.AddVertexWithFlags(x + tmpv.X, y + tmpv.Y, z + tmpv.Z, texPos.x2, texPos.y2, vars.CurrentLightRGBByCorner[3], flags);
		tmpv = quadOffsets[6];
		toreturn.AddVertexWithFlags(x + tmpv.X, y + tmpv.Y, z + tmpv.Z, texPos.x1, texPos.y2, vars.CurrentLightRGBByCorner[2], flags);
		float tpsx1 = texPosSecond.x1;
		float tpsx2 = texPosSecond.x1 + (texPosSecond.x2 - texPosSecond.x1) / 2f;
		float tpsy1 = texPosSecond.y1;
		float tpsy2 = texPosSecond.y2;
		switch (rotIndex)
		{
		case 0:
			toreturn.CustomFloats.Add(tpsx2, tpsy1);
			toreturn.CustomFloats.Add(tpsx1, tpsy1);
			toreturn.CustomFloats.Add(tpsx2, tpsy2);
			toreturn.CustomFloats.Add(tpsx1, tpsy2);
			break;
		case 1:
			toreturn.CustomFloats.Add(tpsx1, tpsy1);
			toreturn.CustomFloats.Add(tpsx2, tpsy1);
			toreturn.CustomFloats.Add(tpsx1, tpsy2);
			toreturn.CustomFloats.Add(tpsx2, tpsy2);
			break;
		case 2:
			toreturn.CustomFloats.Add(tpsx2, tpsy2);
			toreturn.CustomFloats.Add(tpsx1, tpsy2);
			toreturn.CustomFloats.Add(tpsx2, tpsy1);
			toreturn.CustomFloats.Add(tpsx1, tpsy1);
			break;
		case 3:
			toreturn.CustomFloats.Add(tpsx1, tpsy2);
			toreturn.CustomFloats.Add(tpsx2, tpsy2);
			toreturn.CustomFloats.Add(tpsx1, tpsy1);
			toreturn.CustomFloats.Add(tpsx2, tpsy1);
			break;
		}
		toreturn.CustomInts.Add4(colorMapDataValue);
		toreturn.AddIndices(lastelement, lastelement + 1, lastelement + 2, lastelement + 1, lastelement + 3, lastelement + 2);
		vars.UpdateChunkMinMax(x, y, z);
		vars.UpdateChunkMinMax(x + 1f, y + 1f, z + 1f);
	}
}
