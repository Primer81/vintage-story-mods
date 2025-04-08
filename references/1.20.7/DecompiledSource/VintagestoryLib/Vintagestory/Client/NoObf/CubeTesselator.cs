using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class CubeTesselator : IBlockTesselator
{
	private float blockHeight = 1f;

	public CubeTesselator(float blockHeight)
	{
		this.blockHeight = blockHeight;
	}

	public void Tesselate(TCTCache vars)
	{
		float blockHeight = this.blockHeight;
		int verts = 0;
		TextureAtlasPosition[] textureAtlasPositionsByTextureSubId = vars.textureAtlasPositionsByTextureSubId;
		int[] fastBlockTextureSubidsByFace = vars.fastBlockTextureSubidsByFace;
		bool hasAlternates = vars.block.HasAlternates;
		bool hasTiles = vars.block.HasTiles;
		int colorMapDataValue = vars.ColorMapData.Value;
		int drawFaceFlags = vars.drawFaceFlags;
		int extIndex3d = vars.extIndex3d;
		int allFlags = vars.VertexFlags;
		FastVec3f[][] blockFaceVertices = vars.blockFaceVertices;
		BakedCompositeTexture[][] textures = null;
		int randomSelector = 0;
		int positionSelector = 0;
		if (hasAlternates || hasTiles)
		{
			textures = vars.block.FastTextureVariants;
			randomSelector = GameMath.MurmurHash3(vars.posX, vars.posY, vars.posZ);
		}
		MeshData[] meshPools = vars.tct.GetPoolForPass(vars.RenderPass, 1);
		for (int tileSide = 0; tileSide < 6; tileSide++)
		{
			if ((drawFaceFlags & (1 << tileSide)) == 0)
			{
				continue;
			}
			vars.CalcBlockFaceLight(tileSide, extIndex3d + TileSideEnum.MoveIndex[tileSide]);
			int textureSubId;
			if (hasTiles)
			{
				BakedCompositeTexture[] tiles = textures[tileSide];
				if (tiles != null)
				{
					int width = tiles[0].TilesWidth;
					int height = tiles.Length / tiles[0].TilesWidth;
					string name = tiles[0].BakedName.Path;
					int index = name.IndexOf('@');
					int rotation = 0;
					if (index > 0)
					{
						int num = index + 1;
						int.TryParse(name.Substring(num, name.Length - num), out rotation);
						rotation /= 90;
					}
					int x = 0;
					int y = 0;
					int z = 0;
					switch (tileSide)
					{
					case 0:
						x = vars.posX;
						y = vars.posZ;
						z = vars.posY;
						break;
					case 1:
						x = vars.posZ;
						y = -vars.posX;
						z = vars.posY;
						break;
					case 2:
						x = -vars.posX;
						y = vars.posZ;
						z = vars.posY;
						break;
					case 3:
						x = -vars.posZ;
						y = -vars.posX;
						z = vars.posY;
						break;
					case 4:
						x = vars.posX;
						y = vars.posY;
						z = vars.posZ;
						break;
					case 5:
						x = vars.posX;
						y = vars.posY;
						z = -vars.posZ;
						break;
					}
					switch (rotation)
					{
					case 0:
						positionSelector = GameMath.Mod(-x + y, width) + width * GameMath.Mod(-z, height);
						break;
					case 1:
						positionSelector = GameMath.Mod(z + y, width) + width * GameMath.Mod(x, height);
						break;
					case 2:
						positionSelector = GameMath.Mod(x + y, width) + width * GameMath.Mod(z, height);
						break;
					case 3:
						positionSelector = GameMath.Mod(-z + y, width) + width * GameMath.Mod(-x, height);
						break;
					}
					textureSubId = tiles[GameMath.Mod(positionSelector, tiles.Length)].TextureSubId;
					goto IL_02ee;
				}
			}
			if (hasAlternates)
			{
				BakedCompositeTexture[] variants = textures[tileSide];
				if (variants != null)
				{
					textureSubId = variants[GameMath.Mod(randomSelector, variants.Length)].TextureSubId;
					goto IL_02ee;
				}
			}
			textureSubId = fastBlockTextureSubidsByFace[tileSide];
			goto IL_02ee;
			IL_02ee:
			DrawBlockFace(vars, tileSide, blockFaceVertices[tileSide], textureAtlasPositionsByTextureSubId[textureSubId], allFlags | BlockFacing.ALLFACES[tileSide].NormalPackedFlags, colorMapDataValue, meshPools, blockHeight);
			verts += 4;
		}
	}

	public static void DrawBlockFace(TCTCache vars, int tileSide, FastVec3f[] quadOffsets, TextureAtlasPosition texPos, int flags, int colorMapDataValue, MeshData[] meshPools, float blockHeight = 1f)
	{
		float texHeight = ((tileSide <= 3) ? blockHeight : 1f);
		MeshData obj = meshPools[texPos.atlasNumber];
		int lastelement = obj.VerticesCount;
		int[] currentLightRGBByCorner = vars.CurrentLightRGBByCorner;
		float y3 = texPos.y2;
		float y2 = y3 + (texPos.y1 - y3) * texHeight;
		float x = vars.finalX;
		float y = vars.finalY;
		float z = vars.finalZ;
		FastVec3f tmpv = quadOffsets[5];
		obj.AddVertexWithFlags(x + tmpv.X, y + tmpv.Y * blockHeight, z + tmpv.Z, texPos.x2, y2, currentLightRGBByCorner[1], flags);
		tmpv = quadOffsets[4];
		obj.AddVertexWithFlags(x + tmpv.X, y + tmpv.Y * blockHeight, z + tmpv.Z, texPos.x1, y2, currentLightRGBByCorner[0], flags);
		tmpv = quadOffsets[7];
		obj.AddVertexWithFlags(x + tmpv.X, y + tmpv.Y * blockHeight, z + tmpv.Z, texPos.x2, y3, currentLightRGBByCorner[3], flags);
		tmpv = quadOffsets[6];
		obj.AddVertexWithFlags(x + tmpv.X, y + tmpv.Y * blockHeight, z + tmpv.Z, texPos.x1, y3, currentLightRGBByCorner[2], flags);
		obj.CustomInts.Add4(colorMapDataValue);
		obj.AddIndices(lastelement, lastelement + 1, lastelement + 2, lastelement + 1, lastelement + 3, lastelement + 2);
		vars.UpdateChunkMinMax(x, y, z);
		vars.UpdateChunkMinMax(x + 1f, y + blockHeight, z + 1f);
	}
}
