public class Packet_EntityDespawnSerializer
{
	private const int field = 8;

	public static Packet_EntityDespawn DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_EntityDespawn instance = new Packet_EntityDespawn();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_EntityDespawn DeserializeBuffer(byte[] buffer, int length, Packet_EntityDespawn instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_EntityDespawn Deserialize(CitoMemoryStream stream, Packet_EntityDespawn instance)
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
				instance.EntityIdAdd(ProtocolParser.ReadUInt64(stream));
				break;
			case 16:
				instance.DespawnReasonAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 24:
				instance.DeathDamageSourceAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 32:
				instance.ByEntityIdAdd(ProtocolParser.ReadUInt64(stream));
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

	public static Packet_EntityDespawn DeserializeLengthDelimited(CitoMemoryStream stream, Packet_EntityDespawn instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_EntityDespawn result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_EntityDespawn instance)
	{
		if (instance.EntityId != null)
		{
			for (int m = 0; m < instance.EntityIdCount; m++)
			{
				long i1 = instance.EntityId[m];
				stream.WriteByte(8);
				ProtocolParser.WriteUInt64(stream, i1);
			}
		}
		if (instance.DespawnReason != null)
		{
			for (int l = 0; l < instance.DespawnReasonCount; l++)
			{
				int i2 = instance.DespawnReason[l];
				stream.WriteByte(16);
				ProtocolParser.WriteUInt32(stream, i2);
			}
		}
		if (instance.DeathDamageSource != null)
		{
			for (int k = 0; k < instance.DeathDamageSourceCount; k++)
			{
				int i3 = instance.DeathDamageSource[k];
				stream.WriteByte(24);
				ProtocolParser.WriteUInt32(stream, i3);
			}
		}
		if (instance.ByEntityId != null)
		{
			for (int j = 0; j < instance.ByEntityIdCount; j++)
			{
				long i4 = instance.ByEntityId[j];
				stream.WriteByte(32);
				ProtocolParser.WriteUInt64(stream, i4);
			}
		}
	}

	public static byte[] SerializeToBytes(Packet_EntityDespawn instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_EntityDespawn instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
