using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class SaveLootRandomizerAttributes
{
	public string InventoryId;

	public int SlotId;

	public byte[] attributes;
}
