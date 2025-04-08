using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class ModelCubeUtilExt : CubeMeshUtil
{
	public enum EnumShadeMode
	{
		Off,
		On,
		Gradient
	}

	private static int[] gradientNormalMixedFlags;

	static ModelCubeUtilExt()
	{
		gradientNormalMixedFlags = new int[6];
		for (int i = 0; i < 6; i++)
		{
			Vec3f vec = new Vec3f(0f, 1f, 0f).Mul(0.33f) + BlockFacing.ALLFACES[i].Normalf.Clone().Mul(0.66f);
			vec.Normalize();
			gradientNormalMixedFlags[i] = VertexFlags.PackNormal(vec);
		}
	}

	public static void AddFace(MeshData modeldata, BlockFacing face, Vec3f centerXyz, Vec3f sizeXyz, Vec2f originUv, Vec2f sizeUv, int textureId, int color, EnumShadeMode shade, int[] vertexFlags, float brightness = 1f, int uvRotation = 0, byte climateColorMapId = 0, byte seasonColorMapId = 0, short renderPass = -1)
	{
		int coordPos = face.Index * 12;
		int uvPos = face.Index * 8;
		int lastVertexNumber = modeldata.VerticesCount;
		int col = ColorUtil.ColorMultiply3(color, brightness);
		if (shade == EnumShadeMode.Gradient)
		{
			float half = sizeXyz.Y / 2f;
			int normalUpFlags = BlockFacing.UP.NormalPackedFlags;
			int normalDownFlags = gradientNormalMixedFlags[face.Index];
			for (int j = 0; j < 4; j++)
			{
				float x = centerXyz.X + sizeXyz.X * (float)CubeMeshUtil.CubeVertices[coordPos++] / 2f;
				float y = centerXyz.Y + sizeXyz.Y * (float)CubeMeshUtil.CubeVertices[coordPos++] / 2f;
				int uvIndex2 = 2 * ((uvRotation + j) % 4) + uvPos;
				modeldata.AddWithFlagsVertex(x, y, centerXyz.Z + sizeXyz.Z * (float)CubeMeshUtil.CubeVertices[coordPos++] / 2f, originUv.X + sizeUv.X * (float)CubeMeshUtil.CubeUvCoords[uvIndex2], originUv.Y + sizeUv.Y * (float)CubeMeshUtil.CubeUvCoords[uvIndex2 + 1], col, vertexFlags[j] | ((y > half) ? normalUpFlags : normalDownFlags));
			}
		}
		else
		{
			for (int i = 0; i < 4; i++)
			{
				int uvIndex = 2 * ((uvRotation + i) % 4) + uvPos;
				modeldata.AddWithFlagsVertex(centerXyz.X + sizeXyz.X * (float)CubeMeshUtil.CubeVertices[coordPos++] / 2f, centerXyz.Y + sizeXyz.Y * (float)CubeMeshUtil.CubeVertices[coordPos++] / 2f, centerXyz.Z + sizeXyz.Z * (float)CubeMeshUtil.CubeVertices[coordPos++] / 2f, originUv.X + sizeUv.X * (float)CubeMeshUtil.CubeUvCoords[uvIndex], originUv.Y + sizeUv.Y * (float)CubeMeshUtil.CubeUvCoords[uvIndex + 1], col, vertexFlags[i]);
			}
		}
		modeldata.AddIndex(lastVertexNumber);
		modeldata.AddIndex(lastVertexNumber + 1);
		modeldata.AddIndex(lastVertexNumber + 2);
		modeldata.AddIndex(lastVertexNumber);
		modeldata.AddIndex(lastVertexNumber + 2);
		modeldata.AddIndex(lastVertexNumber + 3);
		if (modeldata.XyzFacesCount >= modeldata.XyzFaces.Length)
		{
			Array.Resize(ref modeldata.XyzFaces, modeldata.XyzFaces.Length + 32);
		}
		if (modeldata.TextureIndicesCount >= modeldata.TextureIndices.Length)
		{
			Array.Resize(ref modeldata.TextureIndices, modeldata.TextureIndices.Length + 32);
		}
		modeldata.TextureIndices[modeldata.TextureIndicesCount++] = modeldata.getTextureIndex(textureId);
		modeldata.XyzFaces[modeldata.XyzFacesCount++] = (byte)((shade != 0) ? face.MeshDataIndex : 0);
		if (modeldata.ClimateColorMapIds != null)
		{
			if (modeldata.ColorMapIdsCount >= modeldata.ClimateColorMapIds.Length)
			{
				Array.Resize(ref modeldata.ClimateColorMapIds, modeldata.ClimateColorMapIds.Length + 32);
				Array.Resize(ref modeldata.SeasonColorMapIds, modeldata.SeasonColorMapIds.Length + 32);
			}
			modeldata.ClimateColorMapIds[modeldata.ColorMapIdsCount] = climateColorMapId;
			modeldata.SeasonColorMapIds[modeldata.ColorMapIdsCount++] = seasonColorMapId;
		}
		if (modeldata.RenderPassesAndExtraBits != null)
		{
			if (modeldata.RenderPassCount >= modeldata.RenderPassesAndExtraBits.Length)
			{
				Array.Resize(ref modeldata.RenderPassesAndExtraBits, modeldata.RenderPassesAndExtraBits.Length + 32);
			}
			modeldata.RenderPassesAndExtraBits[modeldata.RenderPassCount++] = renderPass;
		}
	}

	public static void AddFaceSkipTex(MeshData modeldata, BlockFacing face, Vec3f centerXyz, Vec3f sizeXyz, int color, float brightness = 1f)
	{
		int coordPos = face.Index * 12;
		int lastVertexNumber = modeldata.VerticesCount;
		for (int i = 0; i < 4; i++)
		{
			float[] pos = new float[3]
			{
				centerXyz.X + sizeXyz.X * (float)CubeMeshUtil.CubeVertices[coordPos++] / 2f,
				centerXyz.Y + sizeXyz.Y * (float)CubeMeshUtil.CubeVertices[coordPos++] / 2f,
				centerXyz.Z + sizeXyz.Z * (float)CubeMeshUtil.CubeVertices[coordPos++] / 2f
			};
			modeldata.AddVertexSkipTex(pos[0], pos[1], pos[2], ColorUtil.ColorMultiply3(color, brightness));
		}
		modeldata.AddIndex(lastVertexNumber);
		modeldata.AddIndex(lastVertexNumber + 1);
		modeldata.AddIndex(lastVertexNumber + 2);
		modeldata.AddIndex(lastVertexNumber);
		modeldata.AddIndex(lastVertexNumber + 2);
		modeldata.AddIndex(lastVertexNumber + 3);
	}
}
