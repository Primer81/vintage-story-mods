using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class Room
{
	public int ExitCount;

	public bool IsSmallRoom;

	public int SkylightCount;

	public int NonSkylightCount;

	public int CoolingWallCount;

	public int NonCoolingWallCount;

	public Cuboidi Location;

	public byte[] PosInRoom;

	public int AnyChunkUnloaded;

	public bool IsFullyLoaded(ChunkRooms roomsList)
	{
		if (AnyChunkUnloaded == 0)
		{
			return true;
		}
		if (++AnyChunkUnloaded > 10)
		{
			roomsList.RemoveRoom(this);
		}
		return false;
	}

	public bool Contains(BlockPos pos)
	{
		if (!Location.ContainsOrTouches(pos))
		{
			return false;
		}
		int sizez = Location.Z2 - Location.Z1 + 1;
		int sizex = Location.X2 - Location.X1 + 1;
		int dx = pos.X - Location.X1;
		int num = pos.Y - Location.Y1;
		int dz = pos.Z - Location.Z1;
		int index = (num * sizez + dz) * sizex + dx;
		return (PosInRoom[index / 8] & (1 << index % 8)) > 0;
	}
}
