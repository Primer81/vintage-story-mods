using ProtoBuf;

namespace Vintagestory.Common.Network.Packets;

[ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
public class BulkAnimationPacket
{
	public AnimationPacket[] Packets;
}
