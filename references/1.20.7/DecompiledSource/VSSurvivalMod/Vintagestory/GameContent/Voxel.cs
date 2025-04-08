using System;

namespace Vintagestory.GameContent;

public struct Voxel : IEquatable<Voxel>
{
	public byte x;

	public byte y;

	public byte z;

	public Voxel(byte x, byte y, byte z)
	{
		this.x = x;
		this.y = y;
		this.z = z;
	}

	public bool Equals(Voxel other)
	{
		if (x == other.x && y == other.y)
		{
			return z == other.z;
		}
		return false;
	}
}
