namespace Vintagestory.API.Client;

/// <summary>
/// Utility class for simple quad meshes
/// </summary>
public class QuadMeshUtil
{
	private static int[] quadVertices = new int[12]
	{
		-1, -1, 0, 1, -1, 0, 1, 1, 0, -1,
		1, 0
	};

	private static int[] quadTextureCoords = new int[8] { 0, 0, 1, 0, 1, 1, 0, 1 };

	private static int[] quadVertexIndices = new int[6] { 0, 1, 2, 0, 2, 3 };

	/// <summary>
	/// Returns a single vertical quad mesh of with vertices going from -1/-1 to 1/1
	/// With UV, without RGBA
	/// </summary>
	/// <returns></returns>
	public static MeshData GetQuad()
	{
		MeshData k = new MeshData();
		float[] xyz = new float[12];
		for (int j = 0; j < 12; j++)
		{
			xyz[j] = quadVertices[j];
		}
		k.SetXyz(xyz);
		float[] uv = new float[8];
		for (int i = 0; i < uv.Length; i++)
		{
			uv[i] = quadTextureCoords[i];
		}
		k.SetUv(uv);
		k.SetVerticesCount(4);
		k.SetIndices(quadVertexIndices);
		k.SetIndicesCount(6);
		return k;
	}

	/// <summary>
	/// Quad without rgba, with uv
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <param name="dw"></param>
	/// <param name="dh"></param>
	/// <returns></returns>
	public static MeshData GetCustomQuadModelData(float x, float y, float z, float dw, float dh)
	{
		MeshData k = new MeshData(4, 6, withNormals: false, withUv: true, withRgba: false, withFlags: false);
		for (int j = 0; j < 4; j++)
		{
			k.AddVertex(x + ((quadVertices[j * 3] > 0) ? dw : 0f), y + ((quadVertices[j * 3 + 1] > 0) ? dh : 0f), z, quadTextureCoords[j * 2], quadTextureCoords[j * 2 + 1]);
		}
		for (int i = 0; i < 6; i++)
		{
			k.AddIndex(quadVertexIndices[i]);
		}
		return k;
	}

	/// <summary>
	/// Returns a single vertical  quad mesh at given position, size and color
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <param name="width"></param>
	/// <param name="height"></param>
	/// <param name="r"></param>
	/// <param name="g"></param>
	/// <param name="b"></param>
	/// <param name="a"></param>
	/// <returns></returns>
	public static MeshData GetCustomQuad(float x, float y, float z, float width, float height, byte r, byte g, byte b, byte a)
	{
		MeshData k = new MeshData();
		k.SetXyz(new float[12]
		{
			x,
			y,
			z,
			x + width,
			y,
			z,
			x + width,
			y + height,
			z,
			x,
			y + height,
			z
		});
		float[] uv = new float[8];
		for (int j = 0; j < uv.Length; j += 2)
		{
			uv[j] = (float)quadTextureCoords[j] * width;
			uv[j + 1] = (float)quadTextureCoords[j + 1] * height;
		}
		k.SetUv(uv);
		byte[] rgba = new byte[16];
		for (int i = 0; i < 4; i++)
		{
			rgba[i * 4] = r;
			rgba[i * 4 + 1] = g;
			rgba[i * 4 + 2] = b;
			rgba[i * 4 + 3] = a;
		}
		k.SetRgba(rgba);
		k.SetVerticesCount(4);
		k.SetIndices(quadVertexIndices);
		k.SetIndicesCount(6);
		return k;
	}

	/// <summary>
	/// Returns a single horziontal quad mesh with given params
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <param name="width"></param>
	/// <param name="length"></param>
	/// <param name="r"></param>
	/// <param name="g"></param>
	/// <param name="b"></param>
	/// <param name="a"></param>
	/// <returns></returns>
	public static MeshData GetCustomQuadHorizontal(float x, float y, float z, float width, float length, byte r, byte g, byte b, byte a)
	{
		MeshData k = new MeshData();
		k.SetXyz(new float[12]
		{
			x,
			y,
			z,
			x + width,
			y,
			z,
			x + width,
			y,
			z + length,
			x,
			y,
			z + length
		});
		float[] uv = new float[8];
		for (int j = 0; j < uv.Length; j += 2)
		{
			uv[j] = (float)quadTextureCoords[j] * width;
			uv[j + 1] = (float)quadTextureCoords[j + 1] * length;
		}
		k.SetUv(uv);
		byte[] rgba = new byte[16];
		for (int i = 0; i < 4; i++)
		{
			rgba[i * 4] = r;
			rgba[i * 4 + 1] = g;
			rgba[i * 4 + 2] = b;
			rgba[i * 4 + 3] = a;
		}
		k.SetRgba(rgba);
		k.SetVerticesCount(4);
		k.SetIndices(quadVertexIndices);
		k.SetIndicesCount(6);
		return k;
	}

	/// <summary>
	/// Returns a custom quad mesh with the given params.
	/// </summary>
	/// <param name="u"></param>
	/// <param name="v"></param>
	/// <param name="u2"></param>
	/// <param name="v2"></param>
	/// <param name="dx"></param>
	/// <param name="dy"></param>
	/// <param name="dw"></param>
	/// <param name="dh"></param>
	/// <param name="r"></param>
	/// <param name="g"></param>
	/// <param name="b"></param>
	/// <param name="a"></param>
	/// <returns></returns>
	public static MeshData GetCustomQuadModelData(float u, float v, float u2, float v2, float dx, float dy, float dw, float dh, byte r, byte g, byte b, byte a)
	{
		MeshData j = new MeshData();
		j.SetXyz(new float[12]
		{
			dx,
			dy,
			0f,
			dx + dw,
			dy,
			0f,
			dx + dw,
			dy + dh,
			0f,
			dx,
			dy + dh,
			0f
		});
		j.SetUv(new float[8] { u, v, u2, v, u2, v2, u, v2 });
		byte[] rgba = new byte[16];
		for (int i = 0; i < 4; i++)
		{
			rgba[i * 4] = r;
			rgba[i * 4 + 1] = g;
			rgba[i * 4 + 2] = b;
			rgba[i * 4 + 3] = a;
		}
		j.SetRgba(rgba);
		j.SetVerticesCount(4);
		j.SetIndices(quadVertexIndices);
		j.SetIndicesCount(6);
		return j;
	}
}
