using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.Client;

public class ClientPackets
{
	public static Packet_Client CreateIdentificationPacket(ClientPlatformAbstract platform, ServerConnectData connectData)
	{
		Packet_ClientIdentification p = new Packet_ClientIdentification
		{
			Playername = connectData.PlayerName,
			MdProtocolVersion = platform.GetGameVersion(),
			MpToken = connectData.MpToken,
			ServerPassword = ((connectData.ServerPassword == "") ? null : connectData.ServerPassword),
			PlayerUID = connectData.PlayerUID,
			ViewDistance = ClientSettings.ViewDistance,
			RenderMetaBlocks = (ClientSettings.RenderMetaBlocks ? 1 : 0),
			NetworkVersion = "1.20.8",
			ShortGameVersion = "1.20.7"
		};
		return new Packet_Client
		{
			Id = 1,
			Identification = p
		};
	}

	public static Packet_Client Chat(int groupid, string message, string data = null)
	{
		Packet_ChatLine p = new Packet_ChatLine();
		p.Message = message;
		p.Groupid = groupid;
		p.Data = data;
		return new Packet_Client
		{
			Id = 4,
			Chatline = p
		};
	}

	public static Packet_Client PingReply()
	{
		Packet_ClientPingReply p = new Packet_ClientPingReply();
		return new Packet_Client
		{
			Id = 2,
			PingReply = p
		};
	}

	public static Packet_Client EntityInteraction(int mouseButton, long entityId, BlockFacing onBlockFace, Vec3d hit, int seleboxIndex)
	{
		Packet_EntityInteraction p = new Packet_EntityInteraction
		{
			EntityId = entityId,
			MouseButton = mouseButton,
			SelectionBoxIndex = seleboxIndex,
			HitX = CollectibleNet.SerializeDouble(hit.X),
			HitY = CollectibleNet.SerializeDouble(hit.Y),
			HitZ = CollectibleNet.SerializeDouble(hit.Z),
			OnBlockFace = onBlockFace.Index
		};
		return new Packet_Client
		{
			Id = 17,
			EntityInteraction = p
		};
	}

	public static Packet_Client BlockInteraction(BlockSelection blockSel, int mode, int type)
	{
		Packet_ClientBlockPlaceOrBreak p = new Packet_ClientBlockPlaceOrBreak();
		p.X = blockSel.Position.X;
		p.Y = blockSel.Position.InternalY;
		p.Z = blockSel.Position.Z;
		p.Mode = mode;
		p.BlockType = type;
		p.OnBlockFace = blockSel.Face.Index;
		p.HitX = CollectibleNet.SerializeDouble(blockSel.HitPosition.X);
		p.HitY = CollectibleNet.SerializeDouble(blockSel.HitPosition.Y);
		p.HitZ = CollectibleNet.SerializeDouble(blockSel.HitPosition.Z);
		p.DidOffset = (blockSel.DidOffset ? 1 : 0);
		p.SelectionBoxIndex = blockSel.SelectionBoxIndex;
		return new Packet_Client
		{
			Id = 3,
			BlockPlaceOrBreak = p
		};
	}

	public static Packet_Client SpecialKeyRespawn()
	{
		Packet_Client packet_Client = new Packet_Client();
		packet_Client.Id = 12;
		packet_Client.SpecialKey_ = new Packet_ClientSpecialKey();
		packet_Client.SpecialKey_.Key_ = 0;
		return packet_Client;
	}

	public static Packet_Client RequestJoin()
	{
		Packet_Client packet_Client = new Packet_Client();
		packet_Client.Id = 11;
		packet_Client.RequestJoin = new Packet_ClientRequestJoin();
		packet_Client.RequestJoin.Language = ClientSettings.Language;
		return packet_Client;
	}

	public static Packet_Client Leave(int reason)
	{
		Packet_Client packet_Client = new Packet_Client();
		packet_Client.Id = 14;
		packet_Client.Leave = new Packet_ClientLeave();
		packet_Client.Leave.Reason = reason;
		return packet_Client;
	}

	public static Packet_Client SelectedHotbarSlot(int currentSlotId)
	{
		Packet_Client packet_Client = new Packet_Client();
		packet_Client.Id = 13;
		packet_Client.SelectedHotbarSlot = new Packet_SelectedHotbarSlot();
		packet_Client.SelectedHotbarSlot.SlotNumber = currentSlotId;
		return packet_Client;
	}
}
