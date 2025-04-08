using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ClothSystemPacket
{
	public ClothSystem[] ClothSystems;
}
