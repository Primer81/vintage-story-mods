public class Packet_PlayerDataSerializer
{
	private const int field = 8;

	public static Packet_PlayerData DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_PlayerData instance = new Packet_PlayerData();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_PlayerData DeserializeBuffer(byte[] buffer, int length, Packet_PlayerData instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_PlayerData Deserialize(CitoMemoryStream stream, Packet_PlayerData instance)
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
			case 8:
				instance.ClientId = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.EntityId = ProtocolParser.ReadUInt64(stream);
				break;
			case 24:
				instance.GameMode = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.MoveSpeed = ProtocolParser.ReadUInt32(stream);
				break;
			case 40:
				instance.FreeMove = ProtocolParser.ReadUInt32(stream);
				break;
			case 48:
				instance.NoClip = ProtocolParser.ReadUInt32(stream);
				break;
			case 58:
				instance.InventoryContentsAdd(Packet_InventoryContentsSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 66:
				instance.PlayerUID = ProtocolParser.ReadString(stream);
				break;
			case 72:
				instance.PickingRange = ProtocolParser.ReadUInt32(stream);
				break;
			case 80:
				instance.FreeMovePlaneLock = ProtocolParser.ReadUInt32(stream);
				break;
			case 88:
				instance.AreaSelectionMode = ProtocolParser.ReadUInt32(stream);
				break;
			case 98:
				instance.PrivilegesAdd(ProtocolParser.ReadString(stream));
				break;
			case 106:
				instance.PlayerName = ProtocolParser.ReadString(stream);
				break;
			case 114:
				instance.Entitlements = ProtocolParser.ReadString(stream);
				break;
			case 120:
				instance.HotbarSlotId = ProtocolParser.ReadUInt32(stream);
				break;
			case 128:
				instance.Deaths = ProtocolParser.ReadUInt32(stream);
				break;
			case 136:
				instance.Spawnx = ProtocolParser.ReadUInt32(stream);
				break;
			case 144:
				instance.Spawny = ProtocolParser.ReadUInt32(stream);
				break;
			case 152:
				instance.Spawnz = ProtocolParser.ReadUInt32(stream);
				break;
			case 162:
				instance.RoleCode = ProtocolParser.ReadString(stream);
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

	public static Packet_PlayerData DeserializeLengthDelimited(CitoMemoryStream stream, Packet_PlayerData instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_PlayerData result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_PlayerData instance)
	{
		BoxedArray subBuffer = new BoxedArray();
		if (instance.ClientId != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.ClientId);
		}
		if (instance.EntityId != 0L)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt64(stream, instance.EntityId);
		}
		if (instance.GameMode != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.GameMode);
		}
		if (instance.MoveSpeed != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.MoveSpeed);
		}
		if (instance.FreeMove != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.FreeMove);
		}
		if (instance.NoClip != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.NoClip);
		}
		if (instance.InventoryContents != null)
		{
			for (int k = 0; k < instance.InventoryContentsCount; k++)
			{
				Packet_InventoryContents i13 = instance.InventoryContents[k];
				stream.WriteByte(58);
				CitoMemoryStream ms7 = new CitoMemoryStream(subBuffer);
				Packet_InventoryContentsSerializer.Serialize(ms7, i13);
				int len = ms7.Position();
				ProtocolParser.WriteUInt32_(stream, len);
				stream.Write(ms7.GetBuffer(), 0, len);
			}
		}
		if (instance.PlayerUID != null)
		{
			stream.WriteByte(66);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.PlayerUID));
		}
		if (instance.PickingRange != 0)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt32(stream, instance.PickingRange);
		}
		if (instance.FreeMovePlaneLock != 0)
		{
			stream.WriteByte(80);
			ProtocolParser.WriteUInt32(stream, instance.FreeMovePlaneLock);
		}
		if (instance.AreaSelectionMode != 0)
		{
			stream.WriteByte(88);
			ProtocolParser.WriteUInt32(stream, instance.AreaSelectionMode);
		}
		if (instance.Privileges != null)
		{
			for (int j = 0; j < instance.PrivilegesCount; j++)
			{
				string i12 = instance.Privileges[j];
				stream.WriteByte(98);
				ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(i12));
			}
		}
		if (instance.PlayerName != null)
		{
			stream.WriteByte(106);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.PlayerName));
		}
		if (instance.Entitlements != null)
		{
			stream.WriteByte(114);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Entitlements));
		}
		if (instance.HotbarSlotId != 0)
		{
			stream.WriteByte(120);
			ProtocolParser.WriteUInt32(stream, instance.HotbarSlotId);
		}
		if (instance.Deaths != 0)
		{
			stream.WriteKey(16, 0);
			ProtocolParser.WriteUInt32(stream, instance.Deaths);
		}
		if (instance.Spawnx != 0)
		{
			stream.WriteKey(17, 0);
			ProtocolParser.WriteUInt32(stream, instance.Spawnx);
		}
		if (instance.Spawny != 0)
		{
			stream.WriteKey(18, 0);
			ProtocolParser.WriteUInt32(stream, instance.Spawny);
		}
		if (instance.Spawnz != 0)
		{
			stream.WriteKey(19, 0);
			ProtocolParser.WriteUInt32(stream, instance.Spawnz);
		}
		if (instance.RoleCode != null)
		{
			stream.WriteKey(20, 2);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.RoleCode));
		}
	}

	public static byte[] SerializeToBytes(Packet_PlayerData instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_PlayerData instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
