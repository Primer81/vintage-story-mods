using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class CloudTilesState
{
	public Vec3i CenterTilePos = new Vec3i();

	public int TileOffsetX;

	public int TileOffsetZ;

	public int WindTileOffsetX;

	public int WindTileOffsetZ;

	public void Set(CloudTilesState state)
	{
		TileOffsetX = state.TileOffsetX;
		TileOffsetZ = state.TileOffsetZ;
		WindTileOffsetX = state.WindTileOffsetX;
		WindTileOffsetZ = state.WindTileOffsetZ;
		CenterTilePos.X = state.CenterTilePos.X;
		CenterTilePos.Z = state.CenterTilePos.Z;
	}
}
