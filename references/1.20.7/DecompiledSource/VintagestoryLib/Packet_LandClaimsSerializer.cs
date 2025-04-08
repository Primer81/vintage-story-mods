public class Packet_LandClaimsSerializer
{
	private const int field = 8;

	public static Packet_LandClaims DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_LandClaims instance = new Packet_LandClaims();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_LandClaims DeserializeBuffer(byte[] buffer, int length, Packet_LandClaims instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_LandClaims Deserialize(CitoMemoryStream stream, Packet_LandClaims instance)
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
				instance.AllclaimsAdd(Packet_LandClaimSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 18:
				instance.AddclaimsAdd(Packet_LandClaimSerializer.DeserializeLengthDelimitedNew(stream));
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

	public static Packet_LandClaims DeserializeLengthDelimited(CitoMemoryStream stream, Packet_LandClaims instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_LandClaims result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_LandClaims instance)
	{
		BoxedArray subBuffer = new BoxedArray();
		if (instance.Allclaims != null)
		{
			for (int k = 0; k < instance.AllclaimsCount; k++)
			{
				Packet_LandClaim i1 = instance.Allclaims[k];
				stream.WriteByte(10);
				CitoMemoryStream ms1 = new CitoMemoryStream(subBuffer);
				Packet_LandClaimSerializer.Serialize(ms1, i1);
				int len2 = ms1.Position();
				ProtocolParser.WriteUInt32_(stream, len2);
				stream.Write(ms1.GetBuffer(), 0, len2);
			}
		}
		if (instance.Addclaims != null)
		{
			for (int j = 0; j < instance.AddclaimsCount; j++)
			{
				Packet_LandClaim i2 = instance.Addclaims[j];
				stream.WriteByte(18);
				CitoMemoryStream ms2 = new CitoMemoryStream(subBuffer);
				Packet_LandClaimSerializer.Serialize(ms2, i2);
				int len = ms2.Position();
				ProtocolParser.WriteUInt32_(stream, len);
				stream.Write(ms2.GetBuffer(), 0, len);
			}
		}
	}

	public static byte[] SerializeToBytes(Packet_LandClaims instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_LandClaims instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
