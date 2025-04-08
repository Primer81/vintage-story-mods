using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class AuctionActionPacket
{
	public EnumAuctionAction Action;

	public long AuctionId;

	public long AtAuctioneerEntityId;

	public int DurationWeeks;

	public int Price;

	public bool WithDelivery;
}
