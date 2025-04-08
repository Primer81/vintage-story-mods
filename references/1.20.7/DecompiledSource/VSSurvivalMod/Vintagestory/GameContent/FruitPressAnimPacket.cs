using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class FruitPressAnimPacket
{
	public EnumFruitPressAnimState AnimationState;

	public float AnimationSpeed;

	public float CurrentFrame;
}
