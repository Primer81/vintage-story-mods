using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

/// <summary>
/// Manager interface for Tesselators.
/// </summary>
public interface ITesselatorManager
{
	/// <summary>
	/// Returns the default block mesh that being used by the engine when tesselating a chunk. The alternate and inventory versions are seperate.
	/// </summary>
	/// <param name="block"></param>
	/// <returns></returns>
	MeshData GetDefaultBlockMesh(Block block);

	/// <summary>
	/// Returns the default block mesh ref that being used by the engine when rendering a block in the inventory. The alternate and inventory versions are seperate.
	/// </summary>
	/// <param name="block"></param>
	/// <returns></returns>
	MultiTextureMeshRef GetDefaultBlockMeshRef(Block block);

	/// <summary>
	/// Returns the default block mesh ref that being used by the engine when rendering an item in the inventory. The alternate and inventory versions are seperate.
	/// </summary>
	/// <param name="block"></param>
	/// <returns></returns>
	MultiTextureMeshRef GetDefaultItemMeshRef(Item block);

	Shape GetCachedShape(AssetLocation location);

	void ThreadDispose();
}
