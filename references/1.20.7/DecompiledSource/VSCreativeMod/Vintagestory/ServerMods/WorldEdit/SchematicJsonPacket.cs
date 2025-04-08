using ProtoBuf;

namespace Vintagestory.ServerMods.WorldEdit;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class SchematicJsonPacket
{
	public string Filename;

	public string JsonCode;
}
