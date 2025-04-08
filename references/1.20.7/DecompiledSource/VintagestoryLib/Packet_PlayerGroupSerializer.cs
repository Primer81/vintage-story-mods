public class Packet_PlayerGroupSerializer
{
	private const int field = 8;

	public static Packet_PlayerGroup DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_PlayerGroup instance = new Packet_PlayerGroup();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_PlayerGroup DeserializeBuffer(byte[] buffer, int length, Packet_PlayerGroup instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_PlayerGroup Deserialize(CitoMemoryStream stream, Packet_PlayerGroup instance)
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
				instance.Uid = ProtocolParser.ReadUInt32(stream);
				break;
			case 18:
				instance.Owneruid = ProtocolParser.ReadString(stream);
				break;
			case 26:
				instance.Name = ProtocolParser.ReadString(stream);
				break;
			case 34:
				instance.ChathistoryAdd(Packet_ChatLineSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 40:
				instance.Createdbyprivatemessage = ProtocolParser.ReadUInt32(stream);
				break;
			case 48:
				instance.Membership = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_PlayerGroup DeserializeLengthDelimited(CitoMemoryStream stream, Packet_PlayerGroup instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_PlayerGroup result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_PlayerGroup instance)
	{
		BoxedArray subBuffer = new BoxedArray();
		if (instance.Uid != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.Uid);
		}
		if (instance.Owneruid != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Owneruid));
		}
		if (instance.Name != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Name));
		}
		if (instance.Chathistory != null)
		{
			for (int j = 0; j < instance.ChathistoryCount; j++)
			{
				Packet_ChatLine i4 = instance.Chathistory[j];
				stream.WriteByte(34);
				CitoMemoryStream ms4 = new CitoMemoryStream(subBuffer);
				Packet_ChatLineSerializer.Serialize(ms4, i4);
				int len = ms4.Position();
				ProtocolParser.WriteUInt32_(stream, len);
				stream.Write(ms4.GetBuffer(), 0, len);
			}
		}
		if (instance.Createdbyprivatemessage != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Createdbyprivatemessage);
		}
		if (instance.Membership != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.Membership);
		}
	}

	public static byte[] SerializeToBytes(Packet_PlayerGroup instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_PlayerGroup instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
