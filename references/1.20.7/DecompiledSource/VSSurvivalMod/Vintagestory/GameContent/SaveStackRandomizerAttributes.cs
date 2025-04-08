using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class SaveStackRandomizerAttributes
{
	public string InventoryId;

	public int SlotId;

	public float TotalChance;
}
