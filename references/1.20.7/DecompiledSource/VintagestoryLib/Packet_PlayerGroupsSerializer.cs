public class Packet_PlayerGroupsSerializer
{
	private const int field = 8;

	public static Packet_PlayerGroups DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_PlayerGroups instance = new Packet_PlayerGroups();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_PlayerGroups DeserializeBuffer(byte[] buffer, int length, Packet_PlayerGroups instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_PlayerGroups Deserialize(CitoMemoryStream stream, Packet_PlayerGroups instance)
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
				instance.GroupsAdd(Packet_PlayerGroupSerializer.DeserializeLengthDelimitedNew(stream));
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

	public static Packet_PlayerGroups DeserializeLengthDelimited(CitoMemoryStream stream, Packet_PlayerGroups instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_PlayerGroups result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_PlayerGroups instance)
	{
		BoxedArray subBuffer = new BoxedArray();
		if (instance.Groups != null)
		{
			for (int j = 0; j < instance.GroupsCount; j++)
			{
				Packet_PlayerGroup i1 = instance.Groups[j];
				stream.WriteByte(10);
				CitoMemoryStream ms1 = new CitoMemoryStream(subBuffer);
				Packet_PlayerGroupSerializer.Serialize(ms1, i1);
				int len = ms1.Position();
				ProtocolParser.WriteUInt32_(stream, len);
				stream.Write(ms1.GetBuffer(), 0, len);
			}
		}
	}

	public static byte[] SerializeToBytes(Packet_PlayerGroups instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_PlayerGroups instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
