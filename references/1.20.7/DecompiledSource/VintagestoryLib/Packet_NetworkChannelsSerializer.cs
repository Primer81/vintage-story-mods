public class Packet_NetworkChannelsSerializer
{
	private const int field = 8;

	public static Packet_NetworkChannels DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_NetworkChannels instance = new Packet_NetworkChannels();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_NetworkChannels DeserializeBuffer(byte[] buffer, int length, Packet_NetworkChannels instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_NetworkChannels Deserialize(CitoMemoryStream stream, Packet_NetworkChannels instance)
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
				instance.ChannelIdsAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 18:
				instance.ChannelNamesAdd(ProtocolParser.ReadString(stream));
				break;
			case 24:
				instance.ChannelUdpIdsAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 34:
				instance.ChannelUdpNamesAdd(ProtocolParser.ReadString(stream));
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

	public static Packet_NetworkChannels DeserializeLengthDelimited(CitoMemoryStream stream, Packet_NetworkChannels instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_NetworkChannels result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_NetworkChannels instance)
	{
		if (instance.ChannelIds != null)
		{
			for (int m = 0; m < instance.ChannelIdsCount; m++)
			{
				int i1 = instance.ChannelIds[m];
				stream.WriteByte(8);
				ProtocolParser.WriteUInt32(stream, i1);
			}
		}
		if (instance.ChannelNames != null)
		{
			for (int l = 0; l < instance.ChannelNamesCount; l++)
			{
				string i2 = instance.ChannelNames[l];
				stream.WriteByte(18);
				ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(i2));
			}
		}
		if (instance.ChannelUdpIds != null)
		{
			for (int k = 0; k < instance.ChannelUdpIdsCount; k++)
			{
				int i3 = instance.ChannelUdpIds[k];
				stream.WriteByte(24);
				ProtocolParser.WriteUInt32(stream, i3);
			}
		}
		if (instance.ChannelUdpNames != null)
		{
			for (int j = 0; j < instance.ChannelUdpNamesCount; j++)
			{
				string i4 = instance.ChannelUdpNames[j];
				stream.WriteByte(34);
				ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(i4));
			}
		}
	}

	public static byte[] SerializeToBytes(Packet_NetworkChannels instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_NetworkChannels instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
