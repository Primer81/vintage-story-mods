using Vintagestory.API.Common.Entities;

namespace Vintagestory.Client.NoObf;

public class CameraPoint
{
	internal double x;

	internal double y;

	internal double z;

	internal float pitch;

	internal float yaw;

	internal float roll;

	internal double distance;

	public static CameraPoint FromEntityPos(EntityPos pos)
	{
		return new CameraPoint
		{
			x = pos.X,
			y = pos.Y,
			z = pos.Z,
			pitch = pos.Pitch,
			yaw = pos.Yaw,
			roll = pos.Roll
		};
	}

	internal CameraPoint Clone()
	{
		return new CameraPoint
		{
			x = x,
			y = y,
			z = z,
			pitch = pitch,
			yaw = yaw,
			roll = roll
		};
	}

	internal CameraPoint ExtrapolateFrom(CameraPoint p, int direction)
	{
		double dx = p.x - x;
		double dy = p.y - y;
		double dz = p.z - z;
		float dpitch = p.pitch - pitch;
		float dyaw = p.yaw - yaw;
		float droll = p.roll - roll;
		return new CameraPoint
		{
			x = x - dx * (double)direction,
			y = y - dy * (double)direction,
			z = z - dz * (double)direction,
			pitch = pitch - dpitch * (float)direction,
			yaw = yaw - dyaw * (float)direction,
			roll = roll - droll * (float)direction
		};
	}

	internal bool PositionEquals(CameraPoint point)
	{
		if (point.x == x && point.y == y)
		{
			return point.z == z;
		}
		return false;
	}
}
