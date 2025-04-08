using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.Common.Network.Packets;

[ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
public class PositionPacket
{
	public int EntityId;

	public double X;

	public double Y;

	public double Z;

	public float Yaw;

	public float Pitch;

	public float Roll;

	public float HeadYaw;

	public float HeadPitch;

	public float BodyYaw;

	public int Controls;

	public int Tick;

	public int PositionVersion;

	public float MotionX;

	public float MotionY;

	public float MotionZ;

	public bool Teleport;

	public PositionPacket()
	{
	}

	public PositionPacket(Entity entity, int tick)
	{
		PositionVersion = entity.WatchedAttributes.GetInt("positionVersionNumber");
		EntityId = (int)entity.EntityId;
		EntityPos pos = entity.SidedPos;
		_ = entity.PreviousServerPos;
		X = pos.X;
		Y = pos.Y;
		Z = pos.Z;
		Yaw = pos.Yaw;
		Pitch = pos.Pitch;
		Roll = pos.Roll;
		MotionX = (float)pos.Motion.X;
		MotionY = (float)pos.Motion.Y;
		MotionZ = (float)pos.Motion.Z;
		Teleport = entity.IsTeleport;
		if (entity is EntityAgent eagent)
		{
			HeadYaw = pos.HeadYaw;
			HeadPitch = pos.HeadPitch;
			BodyYaw = eagent.BodyYawServer;
			Controls = eagent.Controls.ToInt();
		}
		Tick = tick;
	}
}
