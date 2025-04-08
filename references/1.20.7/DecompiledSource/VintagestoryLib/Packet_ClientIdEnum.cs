public class Packet_ClientIdEnum
{
	public const int LoginTokenQuery = 33;

	public const int PlayerIdentification = 1;

	public const int PingReply = 2;

	public const int BlockPlaceOrBreak = 3;

	public const int ChatLine = 4;

	public const int ActivateInventorySlot = 7;

	public const int MoveItemstack = 8;

	public const int FlipItemstacks = 9;

	public const int CreateItemstack = 10;

	public const int RequestJoin = 11;

	public const int SpecialKey = 12;

	public const int SelectedHotbarSlot = 13;

	public const int Leave = 14;

	public const int ServerQuery = 15;

	public const int EntityInteraction = 17;

	public const int RequestModeChange = 20;

	public const int MoveKeyChange = 21;

	public const int BlockEntityPacket = 22;

	public const int EntityPacket = 31;

	public const int CustomPacket = 23;

	public const int HandInteraction = 25;

	public const int ClientLoaded = 26;

	public const int SetToolMode = 27;

	public const int BlockDamage = 28;

	public const int ClientPlaying = 29;

	public const int InvOpenClose = 30;

	public const int RuntimeSetting = 32;

	public const int RequestPositionTCP = 34;

	public const int UdpPacket = 35;
}
