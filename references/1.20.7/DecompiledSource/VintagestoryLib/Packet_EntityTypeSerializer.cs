public class Packet_EntityTypeSerializer
{
	private const int field = 8;

	public static Packet_EntityType DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_EntityType instance = new Packet_EntityType();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_EntityType DeserializeBuffer(byte[] buffer, int length, Packet_EntityType instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_EntityType Deserialize(CitoMemoryStream stream, Packet_EntityType instance)
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
				instance.Code = ProtocolParser.ReadString(stream);
				break;
			case 18:
				instance.Class = ProtocolParser.ReadString(stream);
				break;
			case 26:
				instance.Renderer = ProtocolParser.ReadString(stream);
				break;
			case 32:
				instance.Habitat = ProtocolParser.ReadUInt32(stream);
				break;
			case 202:
				instance.Drops = ProtocolParser.ReadBytes(stream);
				break;
			case 90:
				if (instance.Shape == null)
				{
					instance.Shape = Packet_CompositeShapeSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_CompositeShapeSerializer.DeserializeLengthDelimited(stream, instance.Shape);
				}
				break;
			case 42:
				instance.BehaviorsAdd(Packet_BehaviorSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 48:
				instance.CollisionBoxLength = ProtocolParser.ReadUInt32(stream);
				break;
			case 56:
				instance.CollisionBoxHeight = ProtocolParser.ReadUInt32(stream);
				break;
			case 208:
				instance.DeadCollisionBoxLength = ProtocolParser.ReadUInt32(stream);
				break;
			case 216:
				instance.DeadCollisionBoxHeight = ProtocolParser.ReadUInt32(stream);
				break;
			case 256:
				instance.SelectionBoxLength = ProtocolParser.ReadUInt32(stream);
				break;
			case 264:
				instance.SelectionBoxHeight = ProtocolParser.ReadUInt32(stream);
				break;
			case 272:
				instance.DeadSelectionBoxLength = ProtocolParser.ReadUInt32(stream);
				break;
			case 280:
				instance.DeadSelectionBoxHeight = ProtocolParser.ReadUInt32(stream);
				break;
			case 66:
				instance.Attributes = ProtocolParser.ReadString(stream);
				break;
			case 74:
				instance.SoundKeysAdd(ProtocolParser.ReadString(stream));
				break;
			case 82:
				instance.SoundNamesAdd(ProtocolParser.ReadString(stream));
				break;
			case 112:
				instance.IdleSoundChance = ProtocolParser.ReadUInt32(stream);
				break;
			case 296:
				instance.IdleSoundRange = ProtocolParser.ReadUInt32(stream);
				break;
			case 98:
				instance.TextureCodesAdd(ProtocolParser.ReadString(stream));
				break;
			case 106:
				instance.CompositeTexturesAdd(Packet_CompositeTextureSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 120:
				instance.Size = ProtocolParser.ReadUInt32(stream);
				break;
			case 128:
				instance.EyeHeight = ProtocolParser.ReadUInt32(stream);
				break;
			case 288:
				instance.SwimmingEyeHeight = ProtocolParser.ReadUInt32(stream);
				break;
			case 232:
				instance.Weight = ProtocolParser.ReadUInt32(stream);
				break;
			case 136:
				instance.CanClimb = ProtocolParser.ReadUInt32(stream);
				break;
			case 146:
				instance.AnimationMetaData = ProtocolParser.ReadBytes(stream);
				break;
			case 152:
				instance.KnockbackResistance = ProtocolParser.ReadUInt32(stream);
				break;
			case 160:
				instance.GlowLevel = ProtocolParser.ReadUInt32(stream);
				break;
			case 168:
				instance.CanClimbAnywhere = ProtocolParser.ReadUInt32(stream);
				break;
			case 176:
				instance.ClimbTouchDistance = ProtocolParser.ReadUInt32(stream);
				break;
			case 184:
				instance.RotateModelOnClimb = ProtocolParser.ReadUInt32(stream);
				break;
			case 192:
				instance.FallDamage = ProtocolParser.ReadUInt32(stream);
				break;
			case 312:
				instance.FallDamageMultiplier = ProtocolParser.ReadUInt32(stream);
				break;
			case 226:
				instance.VariantAdd(Packet_VariantPartSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 240:
				instance.SizeGrowthFactor = ProtocolParser.ReadUInt32(stream);
				break;
			case 248:
				instance.PitchStep = ProtocolParser.ReadUInt32(stream);
				break;
			case 306:
				instance.Color = ProtocolParser.ReadString(stream);
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

	public static Packet_EntityType DeserializeLengthDelimited(CitoMemoryStream stream, Packet_EntityType instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_EntityType result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_EntityType instance)
	{
		BoxedArray subBuffer = new BoxedArray();
		if (instance.Code != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Code));
		}
		if (instance.Class != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Class));
		}
		if (instance.Renderer != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Renderer));
		}
		if (instance.Habitat != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.Habitat);
		}
		if (instance.Drops != null)
		{
			stream.WriteKey(25, 2);
			ProtocolParser.WriteBytes(stream, instance.Drops);
		}
		if (instance.Shape != null)
		{
			stream.WriteByte(90);
			CitoMemoryStream ms11 = new CitoMemoryStream(subBuffer);
			Packet_CompositeShapeSerializer.Serialize(ms11, instance.Shape);
			int len4 = ms11.Position();
			ProtocolParser.WriteUInt32_(stream, len4);
			stream.Write(ms11.GetBuffer(), 0, len4);
		}
		if (instance.Behaviors != null)
		{
			for (int k2 = 0; k2 < instance.BehaviorsCount; k2++)
			{
				Packet_Behavior i14 = instance.Behaviors[k2];
				stream.WriteByte(42);
				CitoMemoryStream ms14 = new CitoMemoryStream(subBuffer);
				Packet_BehaviorSerializer.Serialize(ms14, i14);
				int len3 = ms14.Position();
				ProtocolParser.WriteUInt32_(stream, len3);
				stream.Write(ms14.GetBuffer(), 0, len3);
			}
		}
		if (instance.CollisionBoxLength != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.CollisionBoxLength);
		}
		if (instance.CollisionBoxHeight != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.CollisionBoxHeight);
		}
		if (instance.DeadCollisionBoxLength != 0)
		{
			stream.WriteKey(26, 0);
			ProtocolParser.WriteUInt32(stream, instance.DeadCollisionBoxLength);
		}
		if (instance.DeadCollisionBoxHeight != 0)
		{
			stream.WriteKey(27, 0);
			ProtocolParser.WriteUInt32(stream, instance.DeadCollisionBoxHeight);
		}
		if (instance.SelectionBoxLength != 0)
		{
			stream.WriteKey(32, 0);
			ProtocolParser.WriteUInt32(stream, instance.SelectionBoxLength);
		}
		if (instance.SelectionBoxHeight != 0)
		{
			stream.WriteKey(33, 0);
			ProtocolParser.WriteUInt32(stream, instance.SelectionBoxHeight);
		}
		if (instance.DeadSelectionBoxLength != 0)
		{
			stream.WriteKey(34, 0);
			ProtocolParser.WriteUInt32(stream, instance.DeadSelectionBoxLength);
		}
		if (instance.DeadSelectionBoxHeight != 0)
		{
			stream.WriteKey(35, 0);
			ProtocolParser.WriteUInt32(stream, instance.DeadSelectionBoxHeight);
		}
		if (instance.Attributes != null)
		{
			stream.WriteByte(66);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Attributes));
		}
		if (instance.SoundKeys != null)
		{
			for (int n = 0; n < instance.SoundKeysCount; n++)
			{
				string i15 = instance.SoundKeys[n];
				stream.WriteByte(74);
				ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(i15));
			}
		}
		if (instance.SoundNames != null)
		{
			for (int m = 0; m < instance.SoundNamesCount; m++)
			{
				string i10 = instance.SoundNames[m];
				stream.WriteByte(82);
				ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(i10));
			}
		}
		if (instance.IdleSoundChance != 0)
		{
			stream.WriteByte(112);
			ProtocolParser.WriteUInt32(stream, instance.IdleSoundChance);
		}
		if (instance.IdleSoundRange != 0)
		{
			stream.WriteKey(37, 0);
			ProtocolParser.WriteUInt32(stream, instance.IdleSoundRange);
		}
		if (instance.TextureCodes != null)
		{
			for (int l = 0; l < instance.TextureCodesCount; l++)
			{
				string i11 = instance.TextureCodes[l];
				stream.WriteByte(98);
				ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(i11));
			}
		}
		if (instance.CompositeTextures != null)
		{
			for (int k = 0; k < instance.CompositeTexturesCount; k++)
			{
				Packet_CompositeTexture i12 = instance.CompositeTextures[k];
				stream.WriteByte(106);
				CitoMemoryStream ms12 = new CitoMemoryStream(subBuffer);
				Packet_CompositeTextureSerializer.Serialize(ms12, i12);
				int len2 = ms12.Position();
				ProtocolParser.WriteUInt32_(stream, len2);
				stream.Write(ms12.GetBuffer(), 0, len2);
			}
		}
		if (instance.Size != 0)
		{
			stream.WriteByte(120);
			ProtocolParser.WriteUInt32(stream, instance.Size);
		}
		if (instance.EyeHeight != 0)
		{
			stream.WriteKey(16, 0);
			ProtocolParser.WriteUInt32(stream, instance.EyeHeight);
		}
		if (instance.SwimmingEyeHeight != 0)
		{
			stream.WriteKey(36, 0);
			ProtocolParser.WriteUInt32(stream, instance.SwimmingEyeHeight);
		}
		if (instance.Weight != 0)
		{
			stream.WriteKey(29, 0);
			ProtocolParser.WriteUInt32(stream, instance.Weight);
		}
		if (instance.CanClimb != 0)
		{
			stream.WriteKey(17, 0);
			ProtocolParser.WriteUInt32(stream, instance.CanClimb);
		}
		if (instance.AnimationMetaData != null)
		{
			stream.WriteKey(18, 2);
			ProtocolParser.WriteBytes(stream, instance.AnimationMetaData);
		}
		if (instance.KnockbackResistance != 0)
		{
			stream.WriteKey(19, 0);
			ProtocolParser.WriteUInt32(stream, instance.KnockbackResistance);
		}
		if (instance.GlowLevel != 0)
		{
			stream.WriteKey(20, 0);
			ProtocolParser.WriteUInt32(stream, instance.GlowLevel);
		}
		if (instance.CanClimbAnywhere != 0)
		{
			stream.WriteKey(21, 0);
			ProtocolParser.WriteUInt32(stream, instance.CanClimbAnywhere);
		}
		if (instance.ClimbTouchDistance != 0)
		{
			stream.WriteKey(22, 0);
			ProtocolParser.WriteUInt32(stream, instance.ClimbTouchDistance);
		}
		if (instance.RotateModelOnClimb != 0)
		{
			stream.WriteKey(23, 0);
			ProtocolParser.WriteUInt32(stream, instance.RotateModelOnClimb);
		}
		if (instance.FallDamage != 0)
		{
			stream.WriteKey(24, 0);
			ProtocolParser.WriteUInt32(stream, instance.FallDamage);
		}
		if (instance.FallDamageMultiplier != 0)
		{
			stream.WriteKey(39, 0);
			ProtocolParser.WriteUInt32(stream, instance.FallDamageMultiplier);
		}
		if (instance.Variant != null)
		{
			for (int j = 0; j < instance.VariantCount; j++)
			{
				Packet_VariantPart i13 = instance.Variant[j];
				stream.WriteKey(28, 2);
				CitoMemoryStream ms13 = new CitoMemoryStream(subBuffer);
				Packet_VariantPartSerializer.Serialize(ms13, i13);
				int len = ms13.Position();
				ProtocolParser.WriteUInt32_(stream, len);
				stream.Write(ms13.GetBuffer(), 0, len);
			}
		}
		if (instance.SizeGrowthFactor != 0)
		{
			stream.WriteKey(30, 0);
			ProtocolParser.WriteUInt32(stream, instance.SizeGrowthFactor);
		}
		if (instance.PitchStep != 0)
		{
			stream.WriteKey(31, 0);
			ProtocolParser.WriteUInt32(stream, instance.PitchStep);
		}
		if (instance.Color != null)
		{
			stream.WriteKey(38, 2);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Color));
		}
	}

	public static byte[] SerializeToBytes(Packet_EntityType instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_EntityType instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
