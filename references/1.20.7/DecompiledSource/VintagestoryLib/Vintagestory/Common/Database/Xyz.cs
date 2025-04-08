namespace Vintagestory.Common.Database;

public struct Xyz
{
	public int X;

	public int Y;

	public int Z;

	public Xyz(int x, int y, int z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	public override int GetHashCode()
	{
		return X ^ Y ^ Z;
	}

	public override bool Equals(object obj)
	{
		if (obj is Xyz other)
		{
			if (X == other.X && Y == other.Y)
			{
				return Z == other.Z;
			}
			return false;
		}
		return base.Equals(obj);
	}
}
