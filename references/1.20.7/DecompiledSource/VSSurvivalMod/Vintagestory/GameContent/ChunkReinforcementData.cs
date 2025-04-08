using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ChunkReinforcementData
{
	public byte[] Data;

	public int chunkX;

	public int chunkY;

	public int chunkZ;
}
