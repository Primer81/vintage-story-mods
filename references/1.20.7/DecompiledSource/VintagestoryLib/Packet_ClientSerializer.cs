public class Packet_ClientSerializer
{
	private const int field = 8;

	public static Packet_Client DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_Client instance = new Packet_Client();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_Client DeserializeBuffer(byte[] buffer, int length, Packet_Client instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_Client Deserialize(CitoMemoryStream stream, Packet_Client instance)
	{
		instance.InitializeValues();
		int keyInt;
		while (true)
		{
			keyInt = stream.ReadByte();
			if (((uint)keyInt & 0x80u) != 0)
			{
				keyInt = ProtocolParser.ReadKeyAsInt(keyInt, stream);
				if (((uint)keyInt & 0x4000u) != 0)
				{
					break;
				}
			}
			switch (keyInt)
			{
			case 0:
				return null;
			case 266:
				if (instance.LoginTokenQuery == null)
				{
					instance.LoginTokenQuery = Packet_LoginTokenQuerySerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_LoginTokenQuerySerializer.DeserializeLengthDelimited(stream, instance.LoginTokenQuery);
				}
				break;
			case 8:
				instance.Id = ProtocolParser.ReadUInt32(stream);
				break;
			case 18:
				if (instance.Identification == null)
				{
					instance.Identification = Packet_ClientIdentificationSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ClientIdentificationSerializer.DeserializeLengthDelimited(stream, instance.Identification);
				}
				break;
			case 26:
				if (instance.BlockPlaceOrBreak == null)
				{
					instance.BlockPlaceOrBreak = Packet_ClientBlockPlaceOrBreakSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ClientBlockPlaceOrBreakSerializer.DeserializeLengthDelimited(stream, instance.BlockPlaceOrBreak);
				}
				break;
			case 34:
				if (instance.Chatline == null)
				{
					instance.Chatline = Packet_ChatLineSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ChatLineSerializer.DeserializeLengthDelimited(stream, instance.Chatline);
				}
				break;
			case 42:
				if (instance.RequestJoin == null)
				{
					instance.RequestJoin = Packet_ClientRequestJoinSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ClientRequestJoinSerializer.DeserializeLengthDelimited(stream, instance.RequestJoin);
				}
				break;
			case 50:
				if (instance.PingReply == null)
				{
					instance.PingReply = Packet_ClientPingReplySerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ClientPingReplySerializer.DeserializeLengthDelimited(stream, instance.PingReply);
				}
				break;
			case 58:
				if (instance.SpecialKey_ == null)
				{
					instance.SpecialKey_ = Packet_ClientSpecialKeySerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ClientSpecialKeySerializer.DeserializeLengthDelimited(stream, instance.SpecialKey_);
				}
				break;
			case 66:
				if (instance.SelectedHotbarSlot == null)
				{
					instance.SelectedHotbarSlot = Packet_SelectedHotbarSlotSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_SelectedHotbarSlotSerializer.DeserializeLengthDelimited(stream, instance.SelectedHotbarSlot);
				}
				break;
			case 74:
				if (instance.Leave == null)
				{
					instance.Leave = Packet_ClientLeaveSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ClientLeaveSerializer.DeserializeLengthDelimited(stream, instance.Leave);
				}
				break;
			case 82:
				if (instance.Query == null)
				{
					instance.Query = Packet_ClientServerQuerySerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ClientServerQuerySerializer.DeserializeLengthDelimited(stream, instance.Query);
				}
				break;
			case 114:
				if (instance.MoveItemstack == null)
				{
					instance.MoveItemstack = Packet_MoveItemstackSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_MoveItemstackSerializer.DeserializeLengthDelimited(stream, instance.MoveItemstack);
				}
				break;
			case 122:
				if (instance.Flipitemstacks == null)
				{
					instance.Flipitemstacks = Packet_FlipItemstacksSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_FlipItemstacksSerializer.DeserializeLengthDelimited(stream, instance.Flipitemstacks);
				}
				break;
			case 130:
				if (instance.EntityInteraction == null)
				{
					instance.EntityInteraction = Packet_EntityInteractionSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_EntityInteractionSerializer.DeserializeLengthDelimited(stream, instance.EntityInteraction);
				}
				break;
			case 146:
				if (instance.EntityPosition == null)
				{
					instance.EntityPosition = Packet_EntityPositionSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_EntityPositionSerializer.DeserializeLengthDelimited(stream, instance.EntityPosition);
				}
				break;
			case 154:
				if (instance.ActivateInventorySlot == null)
				{
					instance.ActivateInventorySlot = Packet_ActivateInventorySlotSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ActivateInventorySlotSerializer.DeserializeLengthDelimited(stream, instance.ActivateInventorySlot);
				}
				break;
			case 162:
				if (instance.CreateItemstack == null)
				{
					instance.CreateItemstack = Packet_CreateItemstackSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_CreateItemstackSerializer.DeserializeLengthDelimited(stream, instance.CreateItemstack);
				}
				break;
			case 170:
				if (instance.RequestModeChange == null)
				{
					instance.RequestModeChange = Packet_PlayerModeSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_PlayerModeSerializer.DeserializeLengthDelimited(stream, instance.RequestModeChange);
				}
				break;
			case 178:
				if (instance.MoveKeyChange == null)
				{
					instance.MoveKeyChange = Packet_MoveKeyChangeSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_MoveKeyChangeSerializer.DeserializeLengthDelimited(stream, instance.MoveKeyChange);
				}
				break;
			case 186:
				if (instance.BlockEntityPacket == null)
				{
					instance.BlockEntityPacket = Packet_BlockEntityPacketSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_BlockEntityPacketSerializer.DeserializeLengthDelimited(stream, instance.BlockEntityPacket);
				}
				break;
			case 250:
				if (instance.EntityPacket == null)
				{
					instance.EntityPacket = Packet_EntityPacketSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_EntityPacketSerializer.DeserializeLengthDelimited(stream, instance.EntityPacket);
				}
				break;
			case 194:
				if (instance.CustomPacket == null)
				{
					instance.CustomPacket = Packet_CustomPacketSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_CustomPacketSerializer.DeserializeLengthDelimited(stream, instance.CustomPacket);
				}
				break;
			case 202:
				if (instance.HandInteraction == null)
				{
					instance.HandInteraction = Packet_ClientHandInteractionSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ClientHandInteractionSerializer.DeserializeLengthDelimited(stream, instance.HandInteraction);
				}
				break;
			case 210:
				if (instance.ToolMode == null)
				{
					instance.ToolMode = Packet_ToolModeSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ToolModeSerializer.DeserializeLengthDelimited(stream, instance.ToolMode);
				}
				break;
			case 218:
				if (instance.BlockDamage == null)
				{
					instance.BlockDamage = Packet_BlockDamageSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_BlockDamageSerializer.DeserializeLengthDelimited(stream, instance.BlockDamage);
				}
				break;
			case 226:
				if (instance.ClientPlaying == null)
				{
					instance.ClientPlaying = Packet_ClientPlayingSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ClientPlayingSerializer.DeserializeLengthDelimited(stream, instance.ClientPlaying);
				}
				break;
			case 242:
				if (instance.InvOpenedClosed == null)
				{
					instance.InvOpenedClosed = Packet_InvOpenCloseSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_InvOpenCloseSerializer.DeserializeLengthDelimited(stream, instance.InvOpenedClosed);
				}
				break;
			case 258:
				if (instance.RuntimeSetting == null)
				{
					instance.RuntimeSetting = Packet_RuntimeSettingSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_RuntimeSettingSerializer.DeserializeLengthDelimited(stream, instance.RuntimeSetting);
				}
				break;
			case 274:
				if (instance.UdpPacket == null)
				{
					instance.UdpPacket = Packet_UdpPacketSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_UdpPacketSerializer.DeserializeLengthDelimited(stream, instance.UdpPacket);
				}
				break;
			default:
				ProtocolParser.SkipKey(stream, Key.Create(keyInt));
				break;
			}
		}
		if (keyInt >= 0)
		{
			return null;
		}
		return instance;
	}

	public static Packet_Client DeserializeLengthDelimited(CitoMemoryStream stream, Packet_Client instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_Client result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_Client instance)
	{
		BoxedArray subBuffer = new BoxedArray();
		if (instance.LoginTokenQuery != null)
		{
			stream.WriteKey(33, 2);
			CitoMemoryStream ms30 = new CitoMemoryStream(subBuffer);
			Packet_LoginTokenQuerySerializer.Serialize(ms30, instance.LoginTokenQuery);
			int len28 = ms30.Position();
			ProtocolParser.WriteUInt32_(stream, len28);
			stream.Write(ms30.GetBuffer(), 0, len28);
		}
		if (instance.Id != 1)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.Id);
		}
		if (instance.Identification != null)
		{
			stream.WriteByte(18);
			CitoMemoryStream ms16 = new CitoMemoryStream(subBuffer);
			Packet_ClientIdentificationSerializer.Serialize(ms16, instance.Identification);
			int len27 = ms16.Position();
			ProtocolParser.WriteUInt32_(stream, len27);
			stream.Write(ms16.GetBuffer(), 0, len27);
		}
		if (instance.BlockPlaceOrBreak != null)
		{
			stream.WriteByte(26);
			CitoMemoryStream ms26 = new CitoMemoryStream(subBuffer);
			Packet_ClientBlockPlaceOrBreakSerializer.Serialize(ms26, instance.BlockPlaceOrBreak);
			int len26 = ms26.Position();
			ProtocolParser.WriteUInt32_(stream, len26);
			stream.Write(ms26.GetBuffer(), 0, len26);
		}
		if (instance.Chatline != null)
		{
			stream.WriteByte(34);
			CitoMemoryStream ms32 = new CitoMemoryStream(subBuffer);
			Packet_ChatLineSerializer.Serialize(ms32, instance.Chatline);
			int len25 = ms32.Position();
			ProtocolParser.WriteUInt32_(stream, len25);
			stream.Write(ms32.GetBuffer(), 0, len25);
		}
		if (instance.RequestJoin != null)
		{
			stream.WriteByte(42);
			CitoMemoryStream ms33 = new CitoMemoryStream(subBuffer);
			Packet_ClientRequestJoinSerializer.Serialize(ms33, instance.RequestJoin);
			int len24 = ms33.Position();
			ProtocolParser.WriteUInt32_(stream, len24);
			stream.Write(ms33.GetBuffer(), 0, len24);
		}
		if (instance.PingReply != null)
		{
			stream.WriteByte(50);
			CitoMemoryStream ms34 = new CitoMemoryStream(subBuffer);
			Packet_ClientPingReplySerializer.Serialize(ms34, instance.PingReply);
			int len23 = ms34.Position();
			ProtocolParser.WriteUInt32_(stream, len23);
			stream.Write(ms34.GetBuffer(), 0, len23);
		}
		if (instance.SpecialKey_ != null)
		{
			stream.WriteByte(58);
			CitoMemoryStream ms35 = new CitoMemoryStream(subBuffer);
			Packet_ClientSpecialKeySerializer.Serialize(ms35, instance.SpecialKey_);
			int len22 = ms35.Position();
			ProtocolParser.WriteUInt32_(stream, len22);
			stream.Write(ms35.GetBuffer(), 0, len22);
		}
		if (instance.SelectedHotbarSlot != null)
		{
			stream.WriteByte(66);
			CitoMemoryStream ms36 = new CitoMemoryStream(subBuffer);
			Packet_SelectedHotbarSlotSerializer.Serialize(ms36, instance.SelectedHotbarSlot);
			int len21 = ms36.Position();
			ProtocolParser.WriteUInt32_(stream, len21);
			stream.Write(ms36.GetBuffer(), 0, len21);
		}
		if (instance.Leave != null)
		{
			stream.WriteByte(74);
			CitoMemoryStream ms37 = new CitoMemoryStream(subBuffer);
			Packet_ClientLeaveSerializer.Serialize(ms37, instance.Leave);
			int len20 = ms37.Position();
			ProtocolParser.WriteUInt32_(stream, len20);
			stream.Write(ms37.GetBuffer(), 0, len20);
		}
		if (instance.Query != null)
		{
			stream.WriteByte(82);
			CitoMemoryStream ms10 = new CitoMemoryStream(subBuffer);
			Packet_ClientServerQuerySerializer.Serialize(ms10, instance.Query);
			int len19 = ms10.Position();
			ProtocolParser.WriteUInt32_(stream, len19);
			stream.Write(ms10.GetBuffer(), 0, len19);
		}
		if (instance.MoveItemstack != null)
		{
			stream.WriteByte(114);
			CitoMemoryStream ms11 = new CitoMemoryStream(subBuffer);
			Packet_MoveItemstackSerializer.Serialize(ms11, instance.MoveItemstack);
			int len18 = ms11.Position();
			ProtocolParser.WriteUInt32_(stream, len18);
			stream.Write(ms11.GetBuffer(), 0, len18);
		}
		if (instance.Flipitemstacks != null)
		{
			stream.WriteByte(122);
			CitoMemoryStream ms12 = new CitoMemoryStream(subBuffer);
			Packet_FlipItemstacksSerializer.Serialize(ms12, instance.Flipitemstacks);
			int len17 = ms12.Position();
			ProtocolParser.WriteUInt32_(stream, len17);
			stream.Write(ms12.GetBuffer(), 0, len17);
		}
		if (instance.EntityInteraction != null)
		{
			stream.WriteKey(16, 2);
			CitoMemoryStream ms13 = new CitoMemoryStream(subBuffer);
			Packet_EntityInteractionSerializer.Serialize(ms13, instance.EntityInteraction);
			int len16 = ms13.Position();
			ProtocolParser.WriteUInt32_(stream, len16);
			stream.Write(ms13.GetBuffer(), 0, len16);
		}
		if (instance.EntityPosition != null)
		{
			stream.WriteKey(18, 2);
			CitoMemoryStream ms14 = new CitoMemoryStream(subBuffer);
			Packet_EntityPositionSerializer.Serialize(ms14, instance.EntityPosition);
			int len15 = ms14.Position();
			ProtocolParser.WriteUInt32_(stream, len15);
			stream.Write(ms14.GetBuffer(), 0, len15);
		}
		if (instance.ActivateInventorySlot != null)
		{
			stream.WriteKey(19, 2);
			CitoMemoryStream ms15 = new CitoMemoryStream(subBuffer);
			Packet_ActivateInventorySlotSerializer.Serialize(ms15, instance.ActivateInventorySlot);
			int len14 = ms15.Position();
			ProtocolParser.WriteUInt32_(stream, len14);
			stream.Write(ms15.GetBuffer(), 0, len14);
		}
		if (instance.CreateItemstack != null)
		{
			stream.WriteKey(20, 2);
			CitoMemoryStream ms17 = new CitoMemoryStream(subBuffer);
			Packet_CreateItemstackSerializer.Serialize(ms17, instance.CreateItemstack);
			int len13 = ms17.Position();
			ProtocolParser.WriteUInt32_(stream, len13);
			stream.Write(ms17.GetBuffer(), 0, len13);
		}
		if (instance.RequestModeChange != null)
		{
			stream.WriteKey(21, 2);
			CitoMemoryStream ms18 = new CitoMemoryStream(subBuffer);
			Packet_PlayerModeSerializer.Serialize(ms18, instance.RequestModeChange);
			int len12 = ms18.Position();
			ProtocolParser.WriteUInt32_(stream, len12);
			stream.Write(ms18.GetBuffer(), 0, len12);
		}
		if (instance.MoveKeyChange != null)
		{
			stream.WriteKey(22, 2);
			CitoMemoryStream ms19 = new CitoMemoryStream(subBuffer);
			Packet_MoveKeyChangeSerializer.Serialize(ms19, instance.MoveKeyChange);
			int len11 = ms19.Position();
			ProtocolParser.WriteUInt32_(stream, len11);
			stream.Write(ms19.GetBuffer(), 0, len11);
		}
		if (instance.BlockEntityPacket != null)
		{
			stream.WriteKey(23, 2);
			CitoMemoryStream ms20 = new CitoMemoryStream(subBuffer);
			Packet_BlockEntityPacketSerializer.Serialize(ms20, instance.BlockEntityPacket);
			int len10 = ms20.Position();
			ProtocolParser.WriteUInt32_(stream, len10);
			stream.Write(ms20.GetBuffer(), 0, len10);
		}
		if (instance.EntityPacket != null)
		{
			stream.WriteKey(31, 2);
			CitoMemoryStream ms28 = new CitoMemoryStream(subBuffer);
			Packet_EntityPacketSerializer.Serialize(ms28, instance.EntityPacket);
			int len9 = ms28.Position();
			ProtocolParser.WriteUInt32_(stream, len9);
			stream.Write(ms28.GetBuffer(), 0, len9);
		}
		if (instance.CustomPacket != null)
		{
			stream.WriteKey(24, 2);
			CitoMemoryStream ms21 = new CitoMemoryStream(subBuffer);
			Packet_CustomPacketSerializer.Serialize(ms21, instance.CustomPacket);
			int len8 = ms21.Position();
			ProtocolParser.WriteUInt32_(stream, len8);
			stream.Write(ms21.GetBuffer(), 0, len8);
		}
		if (instance.HandInteraction != null)
		{
			stream.WriteKey(25, 2);
			CitoMemoryStream ms22 = new CitoMemoryStream(subBuffer);
			Packet_ClientHandInteractionSerializer.Serialize(ms22, instance.HandInteraction);
			int len7 = ms22.Position();
			ProtocolParser.WriteUInt32_(stream, len7);
			stream.Write(ms22.GetBuffer(), 0, len7);
		}
		if (instance.ToolMode != null)
		{
			stream.WriteKey(26, 2);
			CitoMemoryStream ms23 = new CitoMemoryStream(subBuffer);
			Packet_ToolModeSerializer.Serialize(ms23, instance.ToolMode);
			int len6 = ms23.Position();
			ProtocolParser.WriteUInt32_(stream, len6);
			stream.Write(ms23.GetBuffer(), 0, len6);
		}
		if (instance.BlockDamage != null)
		{
			stream.WriteKey(27, 2);
			CitoMemoryStream ms24 = new CitoMemoryStream(subBuffer);
			Packet_BlockDamageSerializer.Serialize(ms24, instance.BlockDamage);
			int len5 = ms24.Position();
			ProtocolParser.WriteUInt32_(stream, len5);
			stream.Write(ms24.GetBuffer(), 0, len5);
		}
		if (instance.ClientPlaying != null)
		{
			stream.WriteKey(28, 2);
			CitoMemoryStream ms25 = new CitoMemoryStream(subBuffer);
			Packet_ClientPlayingSerializer.Serialize(ms25, instance.ClientPlaying);
			int len4 = ms25.Position();
			ProtocolParser.WriteUInt32_(stream, len4);
			stream.Write(ms25.GetBuffer(), 0, len4);
		}
		if (instance.InvOpenedClosed != null)
		{
			stream.WriteKey(30, 2);
			CitoMemoryStream ms27 = new CitoMemoryStream(subBuffer);
			Packet_InvOpenCloseSerializer.Serialize(ms27, instance.InvOpenedClosed);
			int len3 = ms27.Position();
			ProtocolParser.WriteUInt32_(stream, len3);
			stream.Write(ms27.GetBuffer(), 0, len3);
		}
		if (instance.RuntimeSetting != null)
		{
			stream.WriteKey(32, 2);
			CitoMemoryStream ms29 = new CitoMemoryStream(subBuffer);
			Packet_RuntimeSettingSerializer.Serialize(ms29, instance.RuntimeSetting);
			int len2 = ms29.Position();
			ProtocolParser.WriteUInt32_(stream, len2);
			stream.Write(ms29.GetBuffer(), 0, len2);
		}
		if (instance.UdpPacket != null)
		{
			stream.WriteKey(34, 2);
			CitoMemoryStream ms31 = new CitoMemoryStream(subBuffer);
			Packet_UdpPacketSerializer.Serialize(ms31, instance.UdpPacket);
			int len = ms31.Position();
			ProtocolParser.WriteUInt32_(stream, len);
			stream.Write(ms31.GetBuffer(), 0, len);
		}
	}

	public static byte[] SerializeToBytes(Packet_Client instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_Client instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
