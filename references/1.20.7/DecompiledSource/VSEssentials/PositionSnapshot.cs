using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

public struct PositionSnapshot
{
	public double x;

	public double y;

	public double z;

	public float interval;

	public bool isTeleport;

	public PositionSnapshot(Vec3d pos, float interval, bool isTeleport)
	{
		x = pos.X;
		y = pos.Y;
		z = pos.Z;
		this.interval = interval;
		this.isTeleport = isTeleport;
	}

	public PositionSnapshot(EntityPos pos, float interval, bool isTeleport)
	{
		x = pos.X;
		y = pos.Y;
		z = pos.Z;
		this.interval = interval;
		this.isTeleport = isTeleport;
	}
}
