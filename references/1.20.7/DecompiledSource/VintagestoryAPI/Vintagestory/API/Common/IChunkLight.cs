namespace Vintagestory.API.Common;

public interface IChunkLight
{
	int GetSunlight(int index3d);

	void SetSunlight(int index3d, int sunlevel);

	void SetSunlight_Buffered(int index3d, int sunlevel);

	int GetBlocklight(int index3d);

	void SetBlocklight(int index3d, int lightlevel);

	void SetBlocklight_Buffered(int index3d, int lightlevel);

	void ClearWithSunlight(ushort sunLight);

	void FloodWithSunlight(ushort sunLight);

	void ClearLight();

	void ClearAllSunlight();
}
