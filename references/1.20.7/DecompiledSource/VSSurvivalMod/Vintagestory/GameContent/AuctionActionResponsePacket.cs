using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class AuctionActionResponsePacket
{
	public string ErrorCode;

	public EnumAuctionAction Action;

	public long AuctionId;

	public long AtAuctioneerEntityId;

	public bool MoneyReceived;

	public int Price;
}
