public class Packet_ServerCalendarSerializer
{
	private const int field = 8;

	public static Packet_ServerCalendar DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerCalendar instance = new Packet_ServerCalendar();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerCalendar DeserializeBuffer(byte[] buffer, int length, Packet_ServerCalendar instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerCalendar Deserialize(CitoMemoryStream stream, Packet_ServerCalendar instance)
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
				instance.TotalSeconds = ProtocolParser.ReadUInt64(stream);
				break;
			case 18:
				instance.TimeSpeedModifierNamesAdd(ProtocolParser.ReadString(stream));
				break;
			case 24:
				instance.TimeSpeedModifierSpeedsAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 32:
				instance.MoonOrbitDays = ProtocolParser.ReadUInt32(stream);
				break;
			case 40:
				instance.HoursPerDay = ProtocolParser.ReadUInt32(stream);
				break;
			case 48:
				instance.Running = ProtocolParser.ReadUInt32(stream);
				break;
			case 56:
				instance.CalendarSpeedMul = ProtocolParser.ReadUInt32(stream);
				break;
			case 64:
				instance.DaysPerMonth = ProtocolParser.ReadUInt32(stream);
				break;
			case 72:
				instance.TotalSecondsStart = ProtocolParser.ReadUInt64(stream);
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

	public static Packet_ServerCalendar DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerCalendar instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerCalendar result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ServerCalendar instance)
	{
		if (instance.TotalSeconds != 0L)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt64(stream, instance.TotalSeconds);
		}
		if (instance.TimeSpeedModifierNames != null)
		{
			for (int k = 0; k < instance.TimeSpeedModifierNamesCount; k++)
			{
				string i2 = instance.TimeSpeedModifierNames[k];
				stream.WriteByte(18);
				ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(i2));
			}
		}
		if (instance.TimeSpeedModifierSpeeds != null)
		{
			for (int j = 0; j < instance.TimeSpeedModifierSpeedsCount; j++)
			{
				int i3 = instance.TimeSpeedModifierSpeeds[j];
				stream.WriteByte(24);
				ProtocolParser.WriteUInt32(stream, i3);
			}
		}
		if (instance.MoonOrbitDays != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.MoonOrbitDays);
		}
		if (instance.HoursPerDay != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.HoursPerDay);
		}
		if (instance.Running != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.Running);
		}
		if (instance.CalendarSpeedMul != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.CalendarSpeedMul);
		}
		if (instance.DaysPerMonth != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.DaysPerMonth);
		}
		if (instance.TotalSecondsStart != 0L)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt64(stream, instance.TotalSecondsStart);
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerCalendar instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerCalendar instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
