using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf;

public class TerrainMesherHelper : ITerrainMeshPool, IMeshPoolSupplier
{
	internal TCTCache vars;

	internal JsonTesselator tess;

	public void AddMeshData(MeshData sourceMesh, float[] transformationMatrix, int lodLevel = 1)
	{
		if (sourceMesh != null)
		{
			tess.AddJsonModelDataToMesh(sourceMesh, lodLevel, vars, this, transformationMatrix);
		}
	}

	public void AddMeshData(MeshData sourceMesh, int lodLevel = 1)
	{
		if (sourceMesh != null)
		{
			tess.AddJsonModelDataToMesh(sourceMesh, lodLevel, vars, this, null);
		}
	}

	public void AddMeshData(MeshData sourceMesh, ColorMapData colorMapData, int lodlevel = 1)
	{
		vars.ColorMapData = colorMapData;
		AddMeshData(sourceMesh, lodlevel);
	}

	public MeshData GetMeshPoolForPass(int textureId, EnumChunkRenderPass forRenderPass, int lodLevel)
	{
		return vars.tct.GetMeshPoolForPass(textureId, forRenderPass, lodLevel);
	}
}
