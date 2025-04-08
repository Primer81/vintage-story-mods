using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class AuctionlistPacket
{
	public bool IsFullUpdate;

	public Auction[] NewAuctions;

	public long[] RemovedAuctions;

	public float TraderDebt;
}
