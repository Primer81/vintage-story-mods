using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class ModelSphereUtil
{
	private static float GetPi()
	{
		return 3141592f / 1000000f;
	}

	public static MeshData GetSphereModelData(float radius, float height, int segments, int rings)
	{
		int i = 0;
		float[] xyz = new float[rings * segments * 3];
		float[] uv = new float[rings * segments * 2];
		byte[] rgba = new byte[rings * segments * 4];
		for (int y = 0; y < rings; y++)
		{
			float yFloat = y;
			float phiFloat = yFloat / (float)(rings - 1) * GetPi();
			for (int x = 0; x < segments; x++)
			{
				float num = x;
				float thetaFloat = num / (float)(segments - 1) * 2f * GetPi();
				float vxFloat = radius * GameMath.Sin(phiFloat) * GameMath.Cos(thetaFloat);
				float vyFloat = height * GameMath.Cos(phiFloat);
				float vzFloat = radius * GameMath.Sin(phiFloat) * GameMath.Sin(thetaFloat);
				float uFloat = num / (float)(segments - 1);
				float vFloat = yFloat / (float)(rings - 1);
				xyz[i * 3] = vxFloat;
				xyz[i * 3 + 1] = vyFloat;
				xyz[i * 3 + 2] = vzFloat;
				uv[i * 2] = uFloat;
				uv[i * 2 + 1] = vFloat;
				rgba[i * 4] = byte.MaxValue;
				rgba[i * 4 + 1] = byte.MaxValue;
				rgba[i * 4 + 2] = byte.MaxValue;
				rgba[i * 4 + 3] = byte.MaxValue;
				i++;
			}
		}
		MeshData meshData = new MeshData();
		meshData.SetVerticesCount(segments * rings);
		meshData.SetIndicesCount(segments * rings * 6);
		meshData.SetXyz(xyz);
		meshData.SetUv(uv);
		meshData.SetRgba(rgba);
		meshData.SetIndices(CalculateElements(radius, height, segments, rings));
		return meshData;
	}

	public static int[] CalculateElements(float radius, float height, int segments, int rings)
	{
		int i = 0;
		int[] data = new int[segments * rings * 6];
		for (int y = 0; y < rings - 1; y++)
		{
			for (int x = 0; x < segments - 1; x++)
			{
				data[i++] = y * segments + x;
				data[i++] = (y + 1) * segments + x;
				data[i++] = (y + 1) * segments + x + 1;
				data[i++] = (y + 1) * segments + x + 1;
				data[i++] = y * segments + x + 1;
				data[i++] = y * segments + x;
			}
		}
		return data;
	}
}
