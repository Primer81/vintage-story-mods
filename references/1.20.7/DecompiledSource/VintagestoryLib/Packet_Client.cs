public class Packet_Client
{
	public Packet_LoginTokenQuery LoginTokenQuery;

	public int Id;

	public Packet_ClientIdentification Identification;

	public Packet_ClientBlockPlaceOrBreak BlockPlaceOrBreak;

	public Packet_ChatLine Chatline;

	public Packet_ClientRequestJoin RequestJoin;

	public Packet_ClientPingReply PingReply;

	public Packet_ClientSpecialKey SpecialKey_;

	public Packet_SelectedHotbarSlot SelectedHotbarSlot;

	public Packet_ClientLeave Leave;

	public Packet_ClientServerQuery Query;

	public Packet_MoveItemstack MoveItemstack;

	public Packet_FlipItemstacks Flipitemstacks;

	public Packet_EntityInteraction EntityInteraction;

	public Packet_EntityPosition EntityPosition;

	public Packet_ActivateInventorySlot ActivateInventorySlot;

	public Packet_CreateItemstack CreateItemstack;

	public Packet_PlayerMode RequestModeChange;

	public Packet_MoveKeyChange MoveKeyChange;

	public Packet_BlockEntityPacket BlockEntityPacket;

	public Packet_EntityPacket EntityPacket;

	public Packet_CustomPacket CustomPacket;

	public Packet_ClientHandInteraction HandInteraction;

	public Packet_ToolMode ToolMode;

	public Packet_BlockDamage BlockDamage;

	public Packet_ClientPlaying ClientPlaying;

	public Packet_InvOpenClose InvOpenedClosed;

	public Packet_RuntimeSetting RuntimeSetting;

	public Packet_UdpPacket UdpPacket;

	public const int LoginTokenQueryFieldID = 33;

	public const int IdFieldID = 1;

	public const int IdentificationFieldID = 2;

	public const int BlockPlaceOrBreakFieldID = 3;

	public const int ChatlineFieldID = 4;

	public const int RequestJoinFieldID = 5;

	public const int PingReplyFieldID = 6;

	public const int SpecialKey_FieldID = 7;

	public const int SelectedHotbarSlotFieldID = 8;

	public const int LeaveFieldID = 9;

	public const int QueryFieldID = 10;

	public const int MoveItemstackFieldID = 14;

	public const int FlipitemstacksFieldID = 15;

	public const int EntityInteractionFieldID = 16;

	public const int EntityPositionFieldID = 18;

	public const int ActivateInventorySlotFieldID = 19;

	public const int CreateItemstackFieldID = 20;

	public const int RequestModeChangeFieldID = 21;

	public const int MoveKeyChangeFieldID = 22;

	public const int BlockEntityPacketFieldID = 23;

	public const int EntityPacketFieldID = 31;

	public const int CustomPacketFieldID = 24;

	public const int HandInteractionFieldID = 25;

	public const int ToolModeFieldID = 26;

	public const int BlockDamageFieldID = 27;

	public const int ClientPlayingFieldID = 28;

	public const int InvOpenedClosedFieldID = 30;

	public const int RuntimeSettingFieldID = 32;

	public const int UdpPacketFieldID = 34;

	public void SetLoginTokenQuery(Packet_LoginTokenQuery value)
	{
		LoginTokenQuery = value;
	}

	public void SetId(int value)
	{
		Id = value;
	}

	public void SetIdentification(Packet_ClientIdentification value)
	{
		Identification = value;
	}

	public void SetBlockPlaceOrBreak(Packet_ClientBlockPlaceOrBreak value)
	{
		BlockPlaceOrBreak = value;
	}

	public void SetChatline(Packet_ChatLine value)
	{
		Chatline = value;
	}

	public void SetRequestJoin(Packet_ClientRequestJoin value)
	{
		RequestJoin = value;
	}

	public void SetPingReply(Packet_ClientPingReply value)
	{
		PingReply = value;
	}

	public void SetSpecialKey_(Packet_ClientSpecialKey value)
	{
		SpecialKey_ = value;
	}

	public void SetSelectedHotbarSlot(Packet_SelectedHotbarSlot value)
	{
		SelectedHotbarSlot = value;
	}

	public void SetLeave(Packet_ClientLeave value)
	{
		Leave = value;
	}

	public void SetQuery(Packet_ClientServerQuery value)
	{
		Query = value;
	}

	public void SetMoveItemstack(Packet_MoveItemstack value)
	{
		MoveItemstack = value;
	}

	public void SetFlipitemstacks(Packet_FlipItemstacks value)
	{
		Flipitemstacks = value;
	}

	public void SetEntityInteraction(Packet_EntityInteraction value)
	{
		EntityInteraction = value;
	}

	public void SetEntityPosition(Packet_EntityPosition value)
	{
		EntityPosition = value;
	}

	public void SetActivateInventorySlot(Packet_ActivateInventorySlot value)
	{
		ActivateInventorySlot = value;
	}

	public void SetCreateItemstack(Packet_CreateItemstack value)
	{
		CreateItemstack = value;
	}

	public void SetRequestModeChange(Packet_PlayerMode value)
	{
		RequestModeChange = value;
	}

	public void SetMoveKeyChange(Packet_MoveKeyChange value)
	{
		MoveKeyChange = value;
	}

	public void SetBlockEntityPacket(Packet_BlockEntityPacket value)
	{
		BlockEntityPacket = value;
	}

	public void SetEntityPacket(Packet_EntityPacket value)
	{
		EntityPacket = value;
	}

	public void SetCustomPacket(Packet_CustomPacket value)
	{
		CustomPacket = value;
	}

	public void SetHandInteraction(Packet_ClientHandInteraction value)
	{
		HandInteraction = value;
	}

	public void SetToolMode(Packet_ToolMode value)
	{
		ToolMode = value;
	}

	public void SetBlockDamage(Packet_BlockDamage value)
	{
		BlockDamage = value;
	}

	public void SetClientPlaying(Packet_ClientPlaying value)
	{
		ClientPlaying = value;
	}

	public void SetInvOpenedClosed(Packet_InvOpenClose value)
	{
		InvOpenedClosed = value;
	}

	public void SetRuntimeSetting(Packet_RuntimeSetting value)
	{
		RuntimeSetting = value;
	}

	public void SetUdpPacket(Packet_UdpPacket value)
	{
		UdpPacket = value;
	}

	internal void InitializeValues()
	{
		Id = 1;
	}
}
