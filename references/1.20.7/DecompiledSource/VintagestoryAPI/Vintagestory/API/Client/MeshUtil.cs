using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

public static class MeshUtil
{
	/// <summary>
	/// Sets given flag if vertex y &gt; WaveFlagMinY, otherwise it clears all wind mode bits
	/// </summary>
	/// <param name="sourceMesh"></param>
	/// <param name="waveFlagMinY"></param>
	/// <param name="flag">Default is EnumWindBitModeMask.NormalWind</param>
	public static void SetWindFlag(this MeshData sourceMesh, float waveFlagMinY = 0.5625f, int flag = 67108864)
	{
		int verticesCount = sourceMesh.VerticesCount;
		float[] sourceMeshXyz = sourceMesh.xyz;
		int[] sourceMeshFlags = sourceMesh.Flags;
		for (int i = 0; i < verticesCount; i++)
		{
			if (sourceMeshXyz[i * 3 + 1] > waveFlagMinY)
			{
				sourceMeshFlags[i] |= flag;
			}
			else
			{
				sourceMeshFlags[i] &= -503316481;
			}
		}
	}

	public static void ClearWindFlags(this MeshData sourceMesh)
	{
		int verticesCount = sourceMesh.VerticesCount;
		int[] sourceMeshFlags = sourceMesh.Flags;
		for (int i = 0; i < verticesCount; i++)
		{
			sourceMeshFlags[i] &= -503316481;
		}
	}

	public static void ToggleWindModeSetWindData(this MeshData sourceMesh, int leavesNoShearTileSide, bool enableWind, int groundOffsetTop)
	{
		int clearFlags = 33554431;
		int verticesCount = sourceMesh.VerticesCount;
		int[] sourceMeshFlags = sourceMesh.Flags;
		if (!enableWind)
		{
			for (int vertexNum = 0; vertexNum < verticesCount; vertexNum++)
			{
				sourceMeshFlags[vertexNum] &= clearFlags;
			}
			return;
		}
		float[] sourceMeshXyz = sourceMesh.xyz;
		for (int vertexNum2 = 0; vertexNum2 < verticesCount; vertexNum2++)
		{
			int y = (int)(sourceMesh.xyz[vertexNum2 * 3 + 1] - 1.5f) >> 1;
			if (leavesNoShearTileSide != 0)
			{
				int x = (int)(sourceMeshXyz[vertexNum2 * 3] - 1.5f) >> 1;
				int z = (int)(sourceMeshXyz[vertexNum2 * 3 + 2] - 1.5f) >> 1;
				int sidesToCheckMask = (1 << 4 - y) | (4 + z * 3) | (2 - x * 6);
				if ((leavesNoShearTileSide & sidesToCheckMask) != 0)
				{
					VertexFlags.ReplaceWindData(ref sourceMeshFlags[vertexNum2], 0);
					continue;
				}
			}
			int groundOffset = ((groundOffsetTop == 8) ? 7 : (groundOffsetTop + y));
			VertexFlags.ReplaceWindData(ref sourceMeshFlags[vertexNum2], groundOffset);
		}
	}
}
