using ProtoBuf;

namespace Vintagestory.ServerMods.WorldEdit;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class CopyToClipboardPacket
{
	public string Text;
}
