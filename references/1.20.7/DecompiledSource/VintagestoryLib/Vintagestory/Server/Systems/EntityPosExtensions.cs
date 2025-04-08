using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.Common;

namespace Vintagestory.Server.Systems;

public static class EntityPosExtensions
{
	public static void SetFromPacket(this EntityPos pos, Packet_EntityPosition packet, Entity entity)
	{
		pos.X = CollectibleNet.DeserializeDoublePrecise(packet.X);
		pos.Y = CollectibleNet.DeserializeDoublePrecise(packet.Y);
		pos.Z = CollectibleNet.DeserializeDoublePrecise(packet.Z);
		pos.Yaw = CollectibleNet.DeserializeFloatPrecise(packet.Yaw);
		pos.Pitch = CollectibleNet.DeserializeFloatPrecise(packet.Pitch);
		pos.Roll = CollectibleNet.DeserializeFloatPrecise(packet.Roll);
		pos.HeadYaw = CollectibleNet.DeserializeFloatPrecise(packet.HeadYaw);
		pos.HeadPitch = CollectibleNet.DeserializeFloatPrecise(packet.HeadPitch);
		pos.Motion.X = CollectibleNet.DeserializeDoublePrecise(packet.MotionX);
		pos.Motion.Y = CollectibleNet.DeserializeDoublePrecise(packet.MotionY);
		pos.Motion.Z = CollectibleNet.DeserializeDoublePrecise(packet.MotionZ);
		if (entity is EntityAgent agent)
		{
			agent.BodyYawServer = CollectibleNet.DeserializeFloatPrecise(packet.BodyYaw);
		}
	}
}
