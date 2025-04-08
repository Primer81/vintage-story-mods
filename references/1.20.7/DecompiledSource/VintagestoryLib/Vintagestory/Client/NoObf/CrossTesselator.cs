using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class CrossTesselator : IBlockTesselator
{
	private static Vec3f startRot = new Vec3f();

	private static Vec3f endRot = new Vec3f();

	public void Tesselate(TCTCache vars)
	{
		vars.drawFaceFlags = 3;
		DrawCross(vars, 1.41f);
	}

	public static void DrawCross(TCTCache vars, float vScaleY)
	{
		Block block = vars.block;
		TextureAtlasPosition[] textureAtlasPositionsByTextureSubId = vars.textureAtlasPositionsByTextureSubId;
		int[] fastBlockTextureSubidsByFace = vars.fastBlockTextureSubidsByFace;
		bool hasAlternates = block.HasAlternates;
		bool randomizeRotations = block.RandomizeRotations;
		int colorMapDataValue = vars.ColorMapData.Value;
		int downRenderFlags = block.VertexFlags.All & -503316481;
		int upRenderFlags = block.VertexFlags.All;
		downRenderFlags |= BlockFacing.UP.NormalPackedFlags;
		upRenderFlags |= BlockFacing.UP.NormalPackedFlags;
		int blockRgb = vars.tct.currentChunkRgbsExt[vars.extIndex3d];
		BakedCompositeTexture[][] textures = null;
		int randomSelector = 0;
		if (hasAlternates || randomizeRotations)
		{
			if (hasAlternates)
			{
				textures = block.FastTextureVariants;
			}
			randomSelector = GameMath.MurmurHash3(vars.posX, (block.RandomizeAxes == EnumRandomizeAxes.XYZ) ? vars.posY : 0, vars.posZ);
		}
		MeshData[] meshPool = vars.tct.GetPoolForPass(block.RenderPass, (!vars.block.DoNotRenderAtLod2) ? 1 : 2);
		for (int i = 0; i < 2; i++)
		{
			int tileSide = i * 2;
			int textureSubId;
			if (hasAlternates)
			{
				BakedCompositeTexture[] variants = textures[tileSide];
				if (variants != null)
				{
					textureSubId = variants[GameMath.Mod(randomSelector, variants.Length)].TextureSubId;
					goto IL_011b;
				}
			}
			textureSubId = fastBlockTextureSubidsByFace[tileSide];
			goto IL_011b;
			IL_011b:
			TextureAtlasPosition texPos = textureAtlasPositionsByTextureSubId[textureSubId];
			MeshData obj = meshPool[texPos.atlasNumber];
			int lastelement = obj.VerticesCount;
			float xPos = vars.finalX;
			float yPos = vars.finalY;
			float zPos = vars.finalZ;
			float xPosEnd;
			float yPosEnd;
			float zPosEnd;
			if (randomizeRotations)
			{
				float[] matrix = TesselationMetaData.randomRotMatrices[GameMath.Mod(randomSelector, TesselationMetaData.randomRotations.Length)];
				Mat4f.MulWithVec3_Position(matrix, i, 0f, 0f, startRot);
				Mat4f.MulWithVec3_Position(matrix, 1f - (float)i, vScaleY, 1f, endRot);
				xPosEnd = endRot.X + xPos;
				xPos += startRot.X;
				yPosEnd = endRot.Y + yPos;
				yPos += startRot.Y;
				zPosEnd = endRot.Z + zPos;
				zPos += startRot.Z;
			}
			else
			{
				xPosEnd = xPos + (1f - (float)i);
				yPosEnd = yPos + vScaleY;
				zPosEnd = zPos + 1f;
				xPos += (float)i;
			}
			obj.AddVertexWithFlags(xPos, yPos, zPos, texPos.x1, texPos.y2, blockRgb, downRenderFlags);
			obj.AddVertexWithFlags(xPosEnd, yPos, zPosEnd, texPos.x2, texPos.y2, blockRgb, downRenderFlags);
			obj.AddVertexWithFlags(xPosEnd, yPosEnd, zPosEnd, texPos.x2, texPos.y1, blockRgb, upRenderFlags);
			obj.AddVertexWithFlags(xPos, yPosEnd, zPos, texPos.x1, texPos.y1, blockRgb, upRenderFlags);
			vars.UpdateChunkMinMax(xPos, yPos, zPos);
			vars.UpdateChunkMinMax(xPosEnd, yPosEnd, zPosEnd);
			obj.CustomInts.Add4(colorMapDataValue);
			obj.AddIndices(lastelement, lastelement + 1, lastelement + 2, lastelement, lastelement + 2, lastelement + 3);
		}
	}
}
