public class Packet_ServerIdentificationSerializer
{
	private const int field = 8;

	public static Packet_ServerIdentification DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerIdentification instance = new Packet_ServerIdentification();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerIdentification DeserializeBuffer(byte[] buffer, int length, Packet_ServerIdentification instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerIdentification Deserialize(CitoMemoryStream stream, Packet_ServerIdentification instance)
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
			case 10:
				instance.NetworkVersion = ProtocolParser.ReadString(stream);
				break;
			case 138:
				instance.GameVersion = ProtocolParser.ReadString(stream);
				break;
			case 26:
				instance.ServerName = ProtocolParser.ReadString(stream);
				break;
			case 56:
				instance.MapSizeX = ProtocolParser.ReadUInt32(stream);
				break;
			case 64:
				instance.MapSizeY = ProtocolParser.ReadUInt32(stream);
				break;
			case 72:
				instance.MapSizeZ = ProtocolParser.ReadUInt32(stream);
				break;
			case 168:
				instance.RegionMapSizeX = ProtocolParser.ReadUInt32(stream);
				break;
			case 176:
				instance.RegionMapSizeY = ProtocolParser.ReadUInt32(stream);
				break;
			case 184:
				instance.RegionMapSizeZ = ProtocolParser.ReadUInt32(stream);
				break;
			case 88:
				instance.DisableShadows = ProtocolParser.ReadUInt32(stream);
				break;
			case 96:
				instance.PlayerAreaSize = ProtocolParser.ReadUInt32(stream);
				break;
			case 104:
				instance.Seed = ProtocolParser.ReadUInt32(stream);
				break;
			case 130:
				instance.PlayStyle = ProtocolParser.ReadString(stream);
				break;
			case 144:
				instance.RequireRemapping = ProtocolParser.ReadUInt32(stream);
				break;
			case 154:
				instance.ModsAdd(Packet_ModIdSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 162:
				instance.WorldConfiguration = ProtocolParser.ReadBytes(stream);
				break;
			case 194:
				instance.SavegameIdentifier = ProtocolParser.ReadString(stream);
				break;
			case 202:
				instance.PlayListCode = ProtocolParser.ReadString(stream);
				break;
			case 210:
				instance.ServerModIdBlackListAdd(ProtocolParser.ReadString(stream));
				break;
			case 218:
				instance.ServerModIdWhiteListAdd(ProtocolParser.ReadString(stream));
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

	public static Packet_ServerIdentification DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerIdentification instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerIdentification result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ServerIdentification instance)
	{
		BoxedArray subBuffer = new BoxedArray();
		if (instance.NetworkVersion != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.NetworkVersion));
		}
		if (instance.GameVersion != null)
		{
			stream.WriteKey(17, 2);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.GameVersion));
		}
		if (instance.ServerName != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.ServerName));
		}
		if (instance.MapSizeX != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.MapSizeX);
		}
		if (instance.MapSizeY != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.MapSizeY);
		}
		if (instance.MapSizeZ != 0)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt32(stream, instance.MapSizeZ);
		}
		if (instance.RegionMapSizeX != 0)
		{
			stream.WriteKey(21, 0);
			ProtocolParser.WriteUInt32(stream, instance.RegionMapSizeX);
		}
		if (instance.RegionMapSizeY != 0)
		{
			stream.WriteKey(22, 0);
			ProtocolParser.WriteUInt32(stream, instance.RegionMapSizeY);
		}
		if (instance.RegionMapSizeZ != 0)
		{
			stream.WriteKey(23, 0);
			ProtocolParser.WriteUInt32(stream, instance.RegionMapSizeZ);
		}
		if (instance.DisableShadows != 0)
		{
			stream.WriteByte(88);
			ProtocolParser.WriteUInt32(stream, instance.DisableShadows);
		}
		if (instance.PlayerAreaSize != 0)
		{
			stream.WriteByte(96);
			ProtocolParser.WriteUInt32(stream, instance.PlayerAreaSize);
		}
		if (instance.Seed != 0)
		{
			stream.WriteByte(104);
			ProtocolParser.WriteUInt32(stream, instance.Seed);
		}
		if (instance.PlayStyle != null)
		{
			stream.WriteKey(16, 2);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.PlayStyle));
		}
		if (instance.RequireRemapping != 0)
		{
			stream.WriteKey(18, 0);
			ProtocolParser.WriteUInt32(stream, instance.RequireRemapping);
		}
		if (instance.Mods != null)
		{
			for (int l = 0; l < instance.ModsCount; l++)
			{
				Packet_ModId i19 = instance.Mods[l];
				stream.WriteKey(19, 2);
				CitoMemoryStream ms19 = new CitoMemoryStream(subBuffer);
				Packet_ModIdSerializer.Serialize(ms19, i19);
				int len = ms19.Position();
				ProtocolParser.WriteUInt32_(stream, len);
				stream.Write(ms19.GetBuffer(), 0, len);
			}
		}
		if (instance.WorldConfiguration != null)
		{
			stream.WriteKey(20, 2);
			ProtocolParser.WriteBytes(stream, instance.WorldConfiguration);
		}
		if (instance.SavegameIdentifier != null)
		{
			stream.WriteKey(24, 2);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.SavegameIdentifier));
		}
		if (instance.PlayListCode != null)
		{
			stream.WriteKey(25, 2);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.PlayListCode));
		}
		if (instance.ServerModIdBlackList != null)
		{
			for (int k = 0; k < instance.ServerModIdBlackListCount; k++)
			{
				string i20 = instance.ServerModIdBlackList[k];
				stream.WriteKey(26, 2);
				ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(i20));
			}
		}
		if (instance.ServerModIdWhiteList != null)
		{
			for (int j = 0; j < instance.ServerModIdWhiteListCount; j++)
			{
				string i21 = instance.ServerModIdWhiteList[j];
				stream.WriteKey(27, 2);
				ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(i21));
			}
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerIdentification instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerIdentification instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
