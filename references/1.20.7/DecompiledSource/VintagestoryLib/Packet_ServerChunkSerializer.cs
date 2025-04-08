public class Packet_ServerChunkSerializer
{
	private const int field = 8;

	public static Packet_ServerChunk DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerChunk instance = new Packet_ServerChunk();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerChunk DeserializeBuffer(byte[] buffer, int length, Packet_ServerChunk instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerChunk Deserialize(CitoMemoryStream stream, Packet_ServerChunk instance)
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
				instance.Blocks = ProtocolParser.ReadBytes(stream);
				break;
			case 18:
				instance.Light = ProtocolParser.ReadBytes(stream);
				break;
			case 26:
				instance.LightSat = ProtocolParser.ReadBytes(stream);
				break;
			case 122:
				instance.Liquids = ProtocolParser.ReadBytes(stream);
				break;
			case 72:
				instance.LightPositionsAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 32:
				instance.X = ProtocolParser.ReadUInt32(stream);
				break;
			case 40:
				instance.Y = ProtocolParser.ReadUInt32(stream);
				break;
			case 48:
				instance.Z = ProtocolParser.ReadUInt32(stream);
				break;
			case 58:
				instance.EntitiesAdd(Packet_EntitySerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 66:
				instance.BlockEntitiesAdd(Packet_BlockEntitySerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 82:
				instance.Moddata = ProtocolParser.ReadBytes(stream);
				break;
			case 88:
				instance.Empty = ProtocolParser.ReadUInt32(stream);
				break;
			case 96:
				instance.DecorsPosAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 104:
				instance.DecorsIdsAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 112:
				instance.Compver = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_ServerChunk DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerChunk instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerChunk result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ServerChunk instance)
	{
		BoxedArray subBuffer = new BoxedArray();
		if (instance.Blocks != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, instance.Blocks);
		}
		if (instance.Light != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteBytes(stream, instance.Light);
		}
		if (instance.LightSat != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteBytes(stream, instance.LightSat);
		}
		if (instance.Liquids != null)
		{
			stream.WriteByte(122);
			ProtocolParser.WriteBytes(stream, instance.Liquids);
		}
		if (instance.LightPositions != null)
		{
			for (int n = 0; n < instance.LightPositionsCount; n++)
			{
				int i16 = instance.LightPositions[n];
				stream.WriteByte(72);
				ProtocolParser.WriteUInt32(stream, i16);
			}
		}
		if (instance.X != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.X);
		}
		if (instance.Y != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Y);
		}
		if (instance.Z != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.Z);
		}
		if (instance.Entities != null)
		{
			for (int m = 0; m < instance.EntitiesCount; m++)
			{
				Packet_Entity i14 = instance.Entities[m];
				stream.WriteByte(58);
				CitoMemoryStream ms7 = new CitoMemoryStream(subBuffer);
				Packet_EntitySerializer.Serialize(ms7, i14);
				int len2 = ms7.Position();
				ProtocolParser.WriteUInt32_(stream, len2);
				stream.Write(ms7.GetBuffer(), 0, len2);
			}
		}
		if (instance.BlockEntities != null)
		{
			for (int l = 0; l < instance.BlockEntitiesCount; l++)
			{
				Packet_BlockEntity i15 = instance.BlockEntities[l];
				stream.WriteByte(66);
				CitoMemoryStream ms8 = new CitoMemoryStream(subBuffer);
				Packet_BlockEntitySerializer.Serialize(ms8, i15);
				int len = ms8.Position();
				ProtocolParser.WriteUInt32_(stream, len);
				stream.Write(ms8.GetBuffer(), 0, len);
			}
		}
		if (instance.Moddata != null)
		{
			stream.WriteByte(82);
			ProtocolParser.WriteBytes(stream, instance.Moddata);
		}
		if (instance.Empty != 0)
		{
			stream.WriteByte(88);
			ProtocolParser.WriteUInt32(stream, instance.Empty);
		}
		if (instance.DecorsPos != null)
		{
			for (int k = 0; k < instance.DecorsPosCount; k++)
			{
				int i12 = instance.DecorsPos[k];
				stream.WriteByte(96);
				ProtocolParser.WriteUInt32(stream, i12);
			}
		}
		if (instance.DecorsIds != null)
		{
			for (int j = 0; j < instance.DecorsIdsCount; j++)
			{
				int i13 = instance.DecorsIds[j];
				stream.WriteByte(104);
				ProtocolParser.WriteUInt32(stream, i13);
			}
		}
		if (instance.Compver != 0)
		{
			stream.WriteByte(112);
			ProtocolParser.WriteUInt32(stream, instance.Compver);
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerChunk instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerChunk instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
