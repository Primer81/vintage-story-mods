using System.Collections.Generic;

namespace Vintagestory.GameContent;

public class ChunkRooms
{
	public List<Room> Rooms = new List<Room>();

	public object roomsLock = new object();

	public void AddRoom(Room room)
	{
		lock (roomsLock)
		{
			Rooms.Add(room);
		}
	}

	public void RemoveRoom(Room room)
	{
		lock (roomsLock)
		{
			Rooms.Remove(room);
		}
	}
}
