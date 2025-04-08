using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf;

public class QuadMeshUtilExt
{
	private static int[] quadVertices = new int[12]
	{
		-1, -1, 0, 1, -1, 0, 1, 1, 0, -1,
		1, 0
	};

	private static int[] quadTextureCoords = new int[8] { 0, 0, 1, 0, 1, 1, 0, 1 };

	private static int[] quadVertexIndices = new int[6] { 0, 1, 2, 0, 2, 3 };

	public static MeshData GetQuadModelData()
	{
		MeshData k = new MeshData(4, 6, withNormals: false, withUv: true, withRgba: false, withFlags: false);
		for (int j = 0; j < 4; j++)
		{
			k.AddVertex(quadVertices[j * 3], quadVertices[j * 3 + 1], quadVertices[j * 3 + 2], quadTextureCoords[j * 2], quadTextureCoords[j * 2 + 1]);
		}
		for (int i = 0; i < 6; i++)
		{
			k.AddIndex(quadVertexIndices[i]);
		}
		return k;
	}

	public static MeshData GetCustomQuadModelData(float x, float y, float z, float dw, float dh, byte r, byte g, byte b, byte a, int textureId = 0)
	{
		MeshData k = new MeshData(4, 6, withNormals: false, withUv: true, withRgba: true, withFlags: false);
		for (int j = 0; j < 4; j++)
		{
			k.AddVertex(x + ((quadVertices[j * 3] > 0) ? dw : 0f), y + ((quadVertices[j * 3 + 1] > 0) ? dh : 0f), z, quadTextureCoords[j * 2], quadTextureCoords[j * 2 + 1], new byte[4] { r, g, b, a });
		}
		k.AddTextureId(textureId);
		for (int i = 0; i < 6; i++)
		{
			k.AddIndex(quadVertexIndices[i]);
		}
		return k;
	}

	public static MeshData GetCustomQuadModelDataHorizontal(float x, float y, float z, float dw, float dl, byte r, byte g, byte b, byte a)
	{
		MeshData k = new MeshData(4, 6, withNormals: false, withUv: true, withRgba: true, withFlags: false);
		for (int j = 0; j < 4; j++)
		{
			k.AddVertex(x + ((quadVertices[j * 3] > 0) ? dw : 0f), y + 0f, z + ((quadVertices[j * 3 + 2] > 0) ? dl : 0f), quadTextureCoords[j * 2], quadTextureCoords[j * 2 + 1], new byte[4] { r, g, b, a });
		}
		for (int i = 0; i < 6; i++)
		{
			k.AddIndex(quadVertexIndices[i]);
		}
		return k;
	}

	public static MeshData GetCustomQuadModelData(float u, float v, float uWidth, float vHeight, float x, float y, float dw, float dh, byte r, byte g, byte b, byte a)
	{
		MeshData k = new MeshData(4, 6, withNormals: false, withUv: true, withRgba: true, withFlags: false);
		for (int j = 0; j < 4; j++)
		{
			k.AddVertex(x + ((quadVertices[j * 3] > 0) ? dw : 0f), y + ((quadVertices[j * 3 + 1] > 0) ? dh : 0f), 0f, quadTextureCoords[j * 2], quadTextureCoords[j * 2 + 1], new byte[4] { r, g, b, a });
		}
		for (int i = 0; i < 6; i++)
		{
			k.AddIndex(quadVertexIndices[i]);
		}
		k.Uv[0] = u;
		k.Uv[1] = v;
		k.Uv[2] = u + uWidth;
		k.Uv[3] = v;
		k.Uv[4] = u + uWidth;
		k.Uv[5] = v + vHeight;
		k.Uv[6] = u;
		k.Uv[7] = v + vHeight;
		return k;
	}
}
