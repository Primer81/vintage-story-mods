public class Packet_ItemTypeSerializer
{
	private const int field = 8;

	public static Packet_ItemType DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ItemType instance = new Packet_ItemType();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ItemType DeserializeBuffer(byte[] buffer, int length, Packet_ItemType instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ItemType Deserialize(CitoMemoryStream stream, Packet_ItemType instance)
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
				instance.ItemId = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.MaxStackSize = ProtocolParser.ReadUInt32(stream);
				break;
			case 26:
				instance.Code = ProtocolParser.ReadString(stream);
				break;
			case 314:
				instance.BehaviorsAdd(Packet_BehaviorSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 34:
				instance.CompositeTexturesAdd(Packet_CompositeTextureSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 40:
				instance.Durability = ProtocolParser.ReadUInt32(stream);
				break;
			case 48:
				instance.MiningmaterialAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 248:
				instance.MiningmaterialspeedAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 56:
				instance.DamagedbyAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 66:
				instance.CreativeInventoryStacks = ProtocolParser.ReadBytes(stream);
				break;
			case 74:
				instance.CreativeInventoryTabsAdd(ProtocolParser.ReadString(stream));
				break;
			case 82:
				if (instance.GuiTransform == null)
				{
					instance.GuiTransform = Packet_ModelTransformSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ModelTransformSerializer.DeserializeLengthDelimited(stream, instance.GuiTransform);
				}
				break;
			case 90:
				if (instance.FpHandTransform == null)
				{
					instance.FpHandTransform = Packet_ModelTransformSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ModelTransformSerializer.DeserializeLengthDelimited(stream, instance.FpHandTransform);
				}
				break;
			case 98:
				if (instance.TpHandTransform == null)
				{
					instance.TpHandTransform = Packet_ModelTransformSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ModelTransformSerializer.DeserializeLengthDelimited(stream, instance.TpHandTransform);
				}
				break;
			case 346:
				if (instance.TpOffHandTransform == null)
				{
					instance.TpOffHandTransform = Packet_ModelTransformSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ModelTransformSerializer.DeserializeLengthDelimited(stream, instance.TpOffHandTransform);
				}
				break;
			case 178:
				if (instance.GroundTransform == null)
				{
					instance.GroundTransform = Packet_ModelTransformSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ModelTransformSerializer.DeserializeLengthDelimited(stream, instance.GroundTransform);
				}
				break;
			case 106:
				instance.Attributes = ProtocolParser.ReadString(stream);
				break;
			case 114:
				if (instance.CombustibleProps == null)
				{
					instance.CombustibleProps = Packet_CombustiblePropertiesSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_CombustiblePropertiesSerializer.DeserializeLengthDelimited(stream, instance.CombustibleProps);
				}
				break;
			case 122:
				if (instance.NutritionProps == null)
				{
					instance.NutritionProps = Packet_NutritionPropertiesSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_NutritionPropertiesSerializer.DeserializeLengthDelimited(stream, instance.NutritionProps);
				}
				break;
			case 258:
				if (instance.GrindingProps == null)
				{
					instance.GrindingProps = Packet_GrindingPropertiesSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_GrindingPropertiesSerializer.DeserializeLengthDelimited(stream, instance.GrindingProps);
				}
				break;
			case 306:
				if (instance.CrushingProps == null)
				{
					instance.CrushingProps = Packet_CrushingPropertiesSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_CrushingPropertiesSerializer.DeserializeLengthDelimited(stream, instance.CrushingProps);
				}
				break;
			case 290:
				instance.TransitionablePropsAdd(Packet_TransitionablePropertiesSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 130:
				if (instance.Shape == null)
				{
					instance.Shape = Packet_CompositeShapeSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_CompositeShapeSerializer.DeserializeLengthDelimited(stream, instance.Shape);
				}
				break;
			case 138:
				instance.TextureCodesAdd(ProtocolParser.ReadString(stream));
				break;
			case 146:
				instance.ItemClass = ProtocolParser.ReadString(stream);
				break;
			case 152:
				instance.Tool = ProtocolParser.ReadUInt32(stream);
				break;
			case 160:
				instance.MaterialDensity = ProtocolParser.ReadUInt32(stream);
				break;
			case 168:
				instance.AttackPower = ProtocolParser.ReadUInt32(stream);
				break;
			case 200:
				instance.AttackRange = ProtocolParser.ReadUInt32(stream);
				break;
			case 184:
				instance.LiquidSelectable = ProtocolParser.ReadUInt32(stream);
				break;
			case 192:
				instance.MiningTier = ProtocolParser.ReadUInt32(stream);
				break;
			case 208:
				instance.StorageFlags = ProtocolParser.ReadUInt32(stream);
				break;
			case 216:
				instance.RenderAlphaTest = ProtocolParser.ReadUInt32(stream);
				break;
			case 226:
				instance.HeldTpHitAnimation = ProtocolParser.ReadString(stream);
				break;
			case 234:
				instance.HeldRightTpIdleAnimation = ProtocolParser.ReadString(stream);
				break;
			case 274:
				instance.HeldLeftTpIdleAnimation = ProtocolParser.ReadString(stream);
				break;
			case 242:
				instance.HeldTpUseAnimation = ProtocolParser.ReadString(stream);
				break;
			case 264:
				instance.MatterState = ProtocolParser.ReadUInt32(stream);
				break;
			case 282:
				instance.VariantAdd(Packet_VariantPartSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 298:
				if (instance.HeldSounds == null)
				{
					instance.HeldSounds = Packet_HeldSoundSetSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_HeldSoundSetSerializer.DeserializeLengthDelimited(stream, instance.HeldSounds);
				}
				break;
			case 320:
				instance.Width = ProtocolParser.ReadUInt32(stream);
				break;
			case 328:
				instance.Height = ProtocolParser.ReadUInt32(stream);
				break;
			case 336:
				instance.Length = ProtocolParser.ReadUInt32(stream);
				break;
			case 352:
				instance.LightHsvAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 360:
				instance.IsMissing = ProtocolParser.ReadUInt32(stream);
				break;
			case 370:
				instance.HeldLeftReadyAnimation = ProtocolParser.ReadString(stream);
				break;
			case 378:
				instance.HeldRightReadyAnimation = ProtocolParser.ReadString(stream);
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

	public static Packet_ItemType DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ItemType instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ItemType result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ItemType instance)
	{
		BoxedArray subBuffer = new BoxedArray();
		if (instance.ItemId != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.ItemId);
		}
		if (instance.MaxStackSize != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.MaxStackSize);
		}
		if (instance.Code != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Code));
		}
		if (instance.Behaviors != null)
		{
			for (int k6 = 0; k6 < instance.BehaviorsCount; k6++)
			{
				Packet_Behavior i21 = instance.Behaviors[k6];
				stream.WriteKey(39, 2);
				CitoMemoryStream ms22 = new CitoMemoryStream(subBuffer);
				Packet_BehaviorSerializer.Serialize(ms22, i21);
				int len15 = ms22.Position();
				ProtocolParser.WriteUInt32_(stream, len15);
				stream.Write(ms22.GetBuffer(), 0, len15);
			}
		}
		if (instance.CompositeTextures != null)
		{
			for (int k5 = 0; k5 < instance.CompositeTexturesCount; k5++)
			{
				Packet_CompositeTexture i22 = instance.CompositeTextures[k5];
				stream.WriteByte(34);
				CitoMemoryStream ms23 = new CitoMemoryStream(subBuffer);
				Packet_CompositeTextureSerializer.Serialize(ms23, i22);
				int len14 = ms23.Position();
				ProtocolParser.WriteUInt32_(stream, len14);
				stream.Write(ms23.GetBuffer(), 0, len14);
			}
		}
		if (instance.Durability != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Durability);
		}
		if (instance.Miningmaterial != null)
		{
			for (int k4 = 0; k4 < instance.MiningmaterialCount; k4++)
			{
				int i24 = instance.Miningmaterial[k4];
				stream.WriteByte(48);
				ProtocolParser.WriteUInt32(stream, i24);
			}
		}
		if (instance.Miningmaterialspeed != null)
		{
			for (int k3 = 0; k3 < instance.MiningmaterialspeedCount; k3++)
			{
				int i18 = instance.Miningmaterialspeed[k3];
				stream.WriteKey(31, 0);
				ProtocolParser.WriteUInt32(stream, i18);
			}
		}
		if (instance.Damagedby != null)
		{
			for (int k2 = 0; k2 < instance.DamagedbyCount; k2++)
			{
				int i25 = instance.Damagedby[k2];
				stream.WriteByte(56);
				ProtocolParser.WriteUInt32(stream, i25);
			}
		}
		if (instance.CreativeInventoryStacks != null)
		{
			stream.WriteByte(66);
			ProtocolParser.WriteBytes(stream, instance.CreativeInventoryStacks);
		}
		if (instance.CreativeInventoryTabs != null)
		{
			for (int n = 0; n < instance.CreativeInventoryTabsCount; n++)
			{
				string i26 = instance.CreativeInventoryTabs[n];
				stream.WriteByte(74);
				ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(i26));
			}
		}
		if (instance.GuiTransform != null)
		{
			stream.WriteByte(82);
			CitoMemoryStream ms10 = new CitoMemoryStream(subBuffer);
			Packet_ModelTransformSerializer.Serialize(ms10, instance.GuiTransform);
			int len13 = ms10.Position();
			ProtocolParser.WriteUInt32_(stream, len13);
			stream.Write(ms10.GetBuffer(), 0, len13);
		}
		if (instance.FpHandTransform != null)
		{
			stream.WriteByte(90);
			CitoMemoryStream ms11 = new CitoMemoryStream(subBuffer);
			Packet_ModelTransformSerializer.Serialize(ms11, instance.FpHandTransform);
			int len12 = ms11.Position();
			ProtocolParser.WriteUInt32_(stream, len12);
			stream.Write(ms11.GetBuffer(), 0, len12);
		}
		if (instance.TpHandTransform != null)
		{
			stream.WriteByte(98);
			CitoMemoryStream ms12 = new CitoMemoryStream(subBuffer);
			Packet_ModelTransformSerializer.Serialize(ms12, instance.TpHandTransform);
			int len11 = ms12.Position();
			ProtocolParser.WriteUInt32_(stream, len11);
			stream.Write(ms12.GetBuffer(), 0, len11);
		}
		if (instance.TpOffHandTransform != null)
		{
			stream.WriteKey(43, 2);
			CitoMemoryStream ms24 = new CitoMemoryStream(subBuffer);
			Packet_ModelTransformSerializer.Serialize(ms24, instance.TpOffHandTransform);
			int len10 = ms24.Position();
			ProtocolParser.WriteUInt32_(stream, len10);
			stream.Write(ms24.GetBuffer(), 0, len10);
		}
		if (instance.GroundTransform != null)
		{
			stream.WriteKey(22, 2);
			CitoMemoryStream ms16 = new CitoMemoryStream(subBuffer);
			Packet_ModelTransformSerializer.Serialize(ms16, instance.GroundTransform);
			int len9 = ms16.Position();
			ProtocolParser.WriteUInt32_(stream, len9);
			stream.Write(ms16.GetBuffer(), 0, len9);
		}
		if (instance.Attributes != null)
		{
			stream.WriteByte(106);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Attributes));
		}
		if (instance.CombustibleProps != null)
		{
			stream.WriteByte(114);
			CitoMemoryStream ms13 = new CitoMemoryStream(subBuffer);
			Packet_CombustiblePropertiesSerializer.Serialize(ms13, instance.CombustibleProps);
			int len8 = ms13.Position();
			ProtocolParser.WriteUInt32_(stream, len8);
			stream.Write(ms13.GetBuffer(), 0, len8);
		}
		if (instance.NutritionProps != null)
		{
			stream.WriteByte(122);
			CitoMemoryStream ms14 = new CitoMemoryStream(subBuffer);
			Packet_NutritionPropertiesSerializer.Serialize(ms14, instance.NutritionProps);
			int len7 = ms14.Position();
			ProtocolParser.WriteUInt32_(stream, len7);
			stream.Write(ms14.GetBuffer(), 0, len7);
		}
		if (instance.GrindingProps != null)
		{
			stream.WriteKey(32, 2);
			CitoMemoryStream ms17 = new CitoMemoryStream(subBuffer);
			Packet_GrindingPropertiesSerializer.Serialize(ms17, instance.GrindingProps);
			int len6 = ms17.Position();
			ProtocolParser.WriteUInt32_(stream, len6);
			stream.Write(ms17.GetBuffer(), 0, len6);
		}
		if (instance.CrushingProps != null)
		{
			stream.WriteKey(38, 2);
			CitoMemoryStream ms21 = new CitoMemoryStream(subBuffer);
			Packet_CrushingPropertiesSerializer.Serialize(ms21, instance.CrushingProps);
			int len5 = ms21.Position();
			ProtocolParser.WriteUInt32_(stream, len5);
			stream.Write(ms21.GetBuffer(), 0, len5);
		}
		if (instance.TransitionableProps != null)
		{
			for (int m = 0; m < instance.TransitionablePropsCount; m++)
			{
				Packet_TransitionableProperties i20 = instance.TransitionableProps[m];
				stream.WriteKey(36, 2);
				CitoMemoryStream ms19 = new CitoMemoryStream(subBuffer);
				Packet_TransitionablePropertiesSerializer.Serialize(ms19, i20);
				int len4 = ms19.Position();
				ProtocolParser.WriteUInt32_(stream, len4);
				stream.Write(ms19.GetBuffer(), 0, len4);
			}
		}
		if (instance.Shape != null)
		{
			stream.WriteKey(16, 2);
			CitoMemoryStream ms15 = new CitoMemoryStream(subBuffer);
			Packet_CompositeShapeSerializer.Serialize(ms15, instance.Shape);
			int len3 = ms15.Position();
			ProtocolParser.WriteUInt32_(stream, len3);
			stream.Write(ms15.GetBuffer(), 0, len3);
		}
		if (instance.TextureCodes != null)
		{
			for (int l = 0; l < instance.TextureCodesCount; l++)
			{
				string i17 = instance.TextureCodes[l];
				stream.WriteKey(17, 2);
				ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(i17));
			}
		}
		if (instance.ItemClass != null)
		{
			stream.WriteKey(18, 2);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.ItemClass));
		}
		if (instance.Tool != 0)
		{
			stream.WriteKey(19, 0);
			ProtocolParser.WriteUInt32(stream, instance.Tool);
		}
		if (instance.MaterialDensity != 0)
		{
			stream.WriteKey(20, 0);
			ProtocolParser.WriteUInt32(stream, instance.MaterialDensity);
		}
		if (instance.AttackPower != 0)
		{
			stream.WriteKey(21, 0);
			ProtocolParser.WriteUInt32(stream, instance.AttackPower);
		}
		if (instance.AttackRange != 0)
		{
			stream.WriteKey(25, 0);
			ProtocolParser.WriteUInt32(stream, instance.AttackRange);
		}
		if (instance.LiquidSelectable != 0)
		{
			stream.WriteKey(23, 0);
			ProtocolParser.WriteUInt32(stream, instance.LiquidSelectable);
		}
		if (instance.MiningTier != 0)
		{
			stream.WriteKey(24, 0);
			ProtocolParser.WriteUInt32(stream, instance.MiningTier);
		}
		if (instance.StorageFlags != 0)
		{
			stream.WriteKey(26, 0);
			ProtocolParser.WriteUInt32(stream, instance.StorageFlags);
		}
		if (instance.RenderAlphaTest != 0)
		{
			stream.WriteKey(27, 0);
			ProtocolParser.WriteUInt32(stream, instance.RenderAlphaTest);
		}
		if (instance.HeldTpHitAnimation != null)
		{
			stream.WriteKey(28, 2);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.HeldTpHitAnimation));
		}
		if (instance.HeldRightTpIdleAnimation != null)
		{
			stream.WriteKey(29, 2);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.HeldRightTpIdleAnimation));
		}
		if (instance.HeldLeftTpIdleAnimation != null)
		{
			stream.WriteKey(34, 2);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.HeldLeftTpIdleAnimation));
		}
		if (instance.HeldTpUseAnimation != null)
		{
			stream.WriteKey(30, 2);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.HeldTpUseAnimation));
		}
		if (instance.MatterState != 0)
		{
			stream.WriteKey(33, 0);
			ProtocolParser.WriteUInt32(stream, instance.MatterState);
		}
		if (instance.Variant != null)
		{
			for (int k = 0; k < instance.VariantCount; k++)
			{
				Packet_VariantPart i19 = instance.Variant[k];
				stream.WriteKey(35, 2);
				CitoMemoryStream ms18 = new CitoMemoryStream(subBuffer);
				Packet_VariantPartSerializer.Serialize(ms18, i19);
				int len2 = ms18.Position();
				ProtocolParser.WriteUInt32_(stream, len2);
				stream.Write(ms18.GetBuffer(), 0, len2);
			}
		}
		if (instance.HeldSounds != null)
		{
			stream.WriteKey(37, 2);
			CitoMemoryStream ms20 = new CitoMemoryStream(subBuffer);
			Packet_HeldSoundSetSerializer.Serialize(ms20, instance.HeldSounds);
			int len = ms20.Position();
			ProtocolParser.WriteUInt32_(stream, len);
			stream.Write(ms20.GetBuffer(), 0, len);
		}
		if (instance.Width != 0)
		{
			stream.WriteKey(40, 0);
			ProtocolParser.WriteUInt32(stream, instance.Width);
		}
		if (instance.Height != 0)
		{
			stream.WriteKey(41, 0);
			ProtocolParser.WriteUInt32(stream, instance.Height);
		}
		if (instance.Length != 0)
		{
			stream.WriteKey(42, 0);
			ProtocolParser.WriteUInt32(stream, instance.Length);
		}
		if (instance.LightHsv != null)
		{
			for (int j = 0; j < instance.LightHsvCount; j++)
			{
				int i23 = instance.LightHsv[j];
				stream.WriteKey(44, 0);
				ProtocolParser.WriteUInt32(stream, i23);
			}
		}
		if (instance.IsMissing != 0)
		{
			stream.WriteKey(45, 0);
			ProtocolParser.WriteUInt32(stream, instance.IsMissing);
		}
		if (instance.HeldLeftReadyAnimation != null)
		{
			stream.WriteKey(46, 2);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.HeldLeftReadyAnimation));
		}
		if (instance.HeldRightReadyAnimation != null)
		{
			stream.WriteKey(47, 2);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.HeldRightReadyAnimation));
		}
	}

	public static byte[] SerializeToBytes(Packet_ItemType instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ItemType instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
