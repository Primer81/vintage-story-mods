public class Packet_BlockTypeSerializer
{
	private const int field = 8;

	public static Packet_BlockType DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_BlockType instance = new Packet_BlockType();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_BlockType DeserializeBuffer(byte[] buffer, int length, Packet_BlockType instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_BlockType Deserialize(CitoMemoryStream stream, Packet_BlockType instance)
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
				instance.TextureCodesAdd(ProtocolParser.ReadString(stream));
				break;
			case 18:
				instance.CompositeTexturesAdd(Packet_CompositeTextureSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 26:
				instance.InventoryTextureCodesAdd(ProtocolParser.ReadString(stream));
				break;
			case 34:
				instance.InventoryCompositeTexturesAdd(Packet_CompositeTextureSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 40:
				instance.BlockId = ProtocolParser.ReadUInt32(stream);
				break;
			case 50:
				instance.Code = ProtocolParser.ReadString(stream);
				break;
			case 466:
				instance.EntityClass = ProtocolParser.ReadString(stream);
				break;
			case 58:
				instance.BehaviorsAdd(Packet_BehaviorSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 674:
				instance.EntityBehaviors = ProtocolParser.ReadString(stream);
				break;
			case 64:
				instance.RenderPass = ProtocolParser.ReadUInt32(stream);
				break;
			case 72:
				instance.DrawType = ProtocolParser.ReadUInt32(stream);
				break;
			case 80:
				instance.MatterState = ProtocolParser.ReadUInt32(stream);
				break;
			case 88:
				instance.WalkSpeedFloat = ProtocolParser.ReadUInt32(stream);
				break;
			case 96:
				instance.IsSlipperyWalk = ProtocolParser.ReadBool(stream);
				break;
			case 106:
				if (instance.Sounds == null)
				{
					instance.Sounds = Packet_BlockSoundSetSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_BlockSoundSetSerializer.DeserializeLengthDelimited(stream, instance.Sounds);
				}
				break;
			case 666:
				if (instance.HeldSounds == null)
				{
					instance.HeldSounds = Packet_HeldSoundSetSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_HeldSoundSetSerializer.DeserializeLengthDelimited(stream, instance.HeldSounds);
				}
				break;
			case 112:
				instance.LightHsvAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 408:
				instance.VertexFlags = ProtocolParser.ReadUInt32(stream);
				break;
			case 120:
				instance.Climbable = ProtocolParser.ReadUInt32(stream);
				break;
			case 130:
				instance.CreativeInventoryTabsAdd(ProtocolParser.ReadString(stream));
				break;
			case 138:
				instance.CreativeInventoryStacks = ProtocolParser.ReadBytes(stream);
				break;
			case 192:
				instance.SideOpaqueFlagsAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 184:
				instance.FaceCullMode = ProtocolParser.ReadUInt32(stream);
				break;
			case 368:
				instance.SideSolidFlagsAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 202:
				instance.SeasonColorMap = ProtocolParser.ReadString(stream);
				break;
			case 706:
				instance.ClimateColorMap = ProtocolParser.ReadString(stream);
				break;
			case 208:
				instance.CullFaces = ProtocolParser.ReadUInt32(stream);
				break;
			case 216:
				instance.Replacable = ProtocolParser.ReadUInt32(stream);
				break;
			case 232:
				instance.LightAbsorption = ProtocolParser.ReadUInt32(stream);
				break;
			case 240:
				instance.HardnessLevel = ProtocolParser.ReadUInt32(stream);
				break;
			case 248:
				instance.Resistance = ProtocolParser.ReadUInt32(stream);
				break;
			case 256:
				instance.BlockMaterial = ProtocolParser.ReadUInt32(stream);
				break;
			case 266:
				instance.Moddata = ProtocolParser.ReadBytes(stream);
				break;
			case 274:
				if (instance.Shape == null)
				{
					instance.Shape = Packet_CompositeShapeSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_CompositeShapeSerializer.DeserializeLengthDelimited(stream, instance.Shape);
				}
				break;
			case 282:
				if (instance.ShapeInventory == null)
				{
					instance.ShapeInventory = Packet_CompositeShapeSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_CompositeShapeSerializer.DeserializeLengthDelimited(stream, instance.ShapeInventory);
				}
				break;
			case 304:
				instance.Ambientocclusion = ProtocolParser.ReadUInt32(stream);
				break;
			case 314:
				instance.CollisionBoxesAdd(Packet_CubeSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 322:
				instance.SelectionBoxesAdd(Packet_CubeSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 730:
				instance.ParticleCollisionBoxesAdd(Packet_CubeSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 330:
				instance.Blockclass = ProtocolParser.ReadString(stream);
				break;
			case 338:
				if (instance.GuiTransform == null)
				{
					instance.GuiTransform = Packet_ModelTransformSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ModelTransformSerializer.DeserializeLengthDelimited(stream, instance.GuiTransform);
				}
				break;
			case 346:
				if (instance.FpHandTransform == null)
				{
					instance.FpHandTransform = Packet_ModelTransformSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ModelTransformSerializer.DeserializeLengthDelimited(stream, instance.FpHandTransform);
				}
				break;
			case 354:
				if (instance.TpHandTransform == null)
				{
					instance.TpHandTransform = Packet_ModelTransformSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ModelTransformSerializer.DeserializeLengthDelimited(stream, instance.TpHandTransform);
				}
				break;
			case 794:
				if (instance.TpOffHandTransform == null)
				{
					instance.TpOffHandTransform = Packet_ModelTransformSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ModelTransformSerializer.DeserializeLengthDelimited(stream, instance.TpOffHandTransform);
				}
				break;
			case 362:
				if (instance.GroundTransform == null)
				{
					instance.GroundTransform = Packet_ModelTransformSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ModelTransformSerializer.DeserializeLengthDelimited(stream, instance.GroundTransform);
				}
				break;
			case 376:
				instance.Fertility = ProtocolParser.ReadUInt32(stream);
				break;
			case 386:
				instance.ParticleProperties = ProtocolParser.ReadBytes(stream);
				break;
			case 392:
				instance.ParticlePropertiesQuantity = ProtocolParser.ReadUInt32(stream);
				break;
			case 400:
				instance.RandomDrawOffset = ProtocolParser.ReadUInt32(stream);
				break;
			case 552:
				instance.RandomizeAxes = ProtocolParser.ReadUInt32(stream);
				break;
			case 696:
				instance.RandomizeRotations = ProtocolParser.ReadUInt32(stream);
				break;
			case 418:
				instance.DropsAdd(Packet_BlockDropSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 424:
				instance.LiquidLevel = ProtocolParser.ReadUInt32(stream);
				break;
			case 434:
				instance.Attributes = ProtocolParser.ReadString(stream);
				break;
			case 442:
				if (instance.CombustibleProps == null)
				{
					instance.CombustibleProps = Packet_CombustiblePropertiesSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_CombustiblePropertiesSerializer.DeserializeLengthDelimited(stream, instance.CombustibleProps);
				}
				break;
			case 456:
				instance.SideAoAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 632:
				instance.NeighbourSideAo = ProtocolParser.ReadUInt32(stream);
				break;
			case 618:
				if (instance.GrindingProps == null)
				{
					instance.GrindingProps = Packet_GrindingPropertiesSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_GrindingPropertiesSerializer.DeserializeLengthDelimited(stream, instance.GrindingProps);
				}
				break;
			case 474:
				if (instance.NutritionProps == null)
				{
					instance.NutritionProps = Packet_NutritionPropertiesSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_NutritionPropertiesSerializer.DeserializeLengthDelimited(stream, instance.NutritionProps);
				}
				break;
			case 682:
				instance.TransitionablePropsAdd(Packet_TransitionablePropertiesSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 480:
				instance.MaxStackSize = ProtocolParser.ReadUInt32(stream);
				break;
			case 490:
				instance.CropProps = ProtocolParser.ReadBytes(stream);
				break;
			case 722:
				instance.CropPropBehaviorsAdd(ProtocolParser.ReadString(stream));
				break;
			case 496:
				instance.MaterialDensity = ProtocolParser.ReadUInt32(stream);
				break;
			case 504:
				instance.AttackPower = ProtocolParser.ReadUInt32(stream);
				break;
			case 560:
				instance.AttackRange = ProtocolParser.ReadUInt32(stream);
				break;
			case 512:
				instance.LiquidSelectable = ProtocolParser.ReadUInt32(stream);
				break;
			case 520:
				instance.MiningTier = ProtocolParser.ReadUInt32(stream);
				break;
			case 528:
				instance.RequiredMiningTier = ProtocolParser.ReadUInt32(stream);
				break;
			case 536:
				instance.MiningmaterialAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 608:
				instance.MiningmaterialspeedAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 544:
				instance.DragMultiplierFloat = ProtocolParser.ReadUInt32(stream);
				break;
			case 568:
				instance.StorageFlags = ProtocolParser.ReadUInt32(stream);
				break;
			case 576:
				instance.RenderAlphaTest = ProtocolParser.ReadUInt32(stream);
				break;
			case 586:
				instance.HeldTpHitAnimation = ProtocolParser.ReadString(stream);
				break;
			case 594:
				instance.HeldRightTpIdleAnimation = ProtocolParser.ReadString(stream);
				break;
			case 642:
				instance.HeldLeftTpIdleAnimation = ProtocolParser.ReadString(stream);
				break;
			case 602:
				instance.HeldTpUseAnimation = ProtocolParser.ReadString(stream);
				break;
			case 624:
				instance.RainPermeable = ProtocolParser.ReadUInt32(stream);
				break;
			case 650:
				instance.LiquidCode = ProtocolParser.ReadString(stream);
				break;
			case 658:
				instance.VariantAdd(Packet_VariantPartSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 690:
				if (instance.Lod0shape == null)
				{
					instance.Lod0shape = Packet_CompositeShapeSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_CompositeShapeSerializer.DeserializeLengthDelimited(stream, instance.Lod0shape);
				}
				break;
			case 712:
				instance.Frostable = ProtocolParser.ReadUInt32(stream);
				break;
			case 738:
				if (instance.CrushingProps == null)
				{
					instance.CrushingProps = Packet_CrushingPropertiesSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_CrushingPropertiesSerializer.DeserializeLengthDelimited(stream, instance.CrushingProps);
				}
				break;
			case 744:
				instance.RandomSizeAdjust = ProtocolParser.ReadUInt32(stream);
				break;
			case 754:
				if (instance.Lod2shape == null)
				{
					instance.Lod2shape = Packet_CompositeShapeSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_CompositeShapeSerializer.DeserializeLengthDelimited(stream, instance.Lod2shape);
				}
				break;
			case 760:
				instance.DoNotRenderAtLod2 = ProtocolParser.ReadUInt32(stream);
				break;
			case 768:
				instance.Width = ProtocolParser.ReadUInt32(stream);
				break;
			case 776:
				instance.Height = ProtocolParser.ReadUInt32(stream);
				break;
			case 784:
				instance.Length = ProtocolParser.ReadUInt32(stream);
				break;
			case 800:
				instance.IsMissing = ProtocolParser.ReadUInt32(stream);
				break;
			case 808:
				instance.Durability = ProtocolParser.ReadUInt32(stream);
				break;
			case 818:
				instance.HeldLeftReadyAnimation = ProtocolParser.ReadString(stream);
				break;
			case 826:
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

	public static Packet_BlockType DeserializeLengthDelimited(CitoMemoryStream stream, Packet_BlockType instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_BlockType result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_BlockType instance)
	{
		BoxedArray subBuffer = new BoxedArray();
		if (instance.TextureCodes != null)
		{
			for (int k15 = 0; k15 < instance.TextureCodesCount; k15++)
			{
				string i1 = instance.TextureCodes[k15];
				stream.WriteByte(10);
				ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(i1));
			}
		}
		if (instance.CompositeTextures != null)
		{
			for (int k14 = 0; k14 < instance.CompositeTexturesCount; k14++)
			{
				Packet_CompositeTexture i4 = instance.CompositeTextures[k14];
				stream.WriteByte(18);
				CitoMemoryStream ms14 = new CitoMemoryStream(subBuffer);
				Packet_CompositeTextureSerializer.Serialize(ms14, i4);
				int len24 = ms14.Position();
				ProtocolParser.WriteUInt32_(stream, len24);
				stream.Write(ms14.GetBuffer(), 0, len24);
			}
		}
		if (instance.InventoryTextureCodes != null)
		{
			for (int k13 = 0; k13 < instance.InventoryTextureCodesCount; k13++)
			{
				string i6 = instance.InventoryTextureCodes[k13];
				stream.WriteByte(26);
				ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(i6));
			}
		}
		if (instance.InventoryCompositeTextures != null)
		{
			for (int k12 = 0; k12 < instance.InventoryCompositeTexturesCount; k12++)
			{
				Packet_CompositeTexture i8 = instance.InventoryCompositeTextures[k12];
				stream.WriteByte(34);
				CitoMemoryStream ms18 = new CitoMemoryStream(subBuffer);
				Packet_CompositeTextureSerializer.Serialize(ms18, i8);
				int len23 = ms18.Position();
				ProtocolParser.WriteUInt32_(stream, len23);
				stream.Write(ms18.GetBuffer(), 0, len23);
			}
		}
		if (instance.BlockId != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.BlockId);
		}
		if (instance.Code != null)
		{
			stream.WriteByte(50);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Code));
		}
		if (instance.EntityClass != null)
		{
			stream.WriteKey(58, 2);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.EntityClass));
		}
		if (instance.Behaviors != null)
		{
			for (int k11 = 0; k11 < instance.BehaviorsCount; k11++)
			{
				Packet_Behavior i14 = instance.Behaviors[k11];
				stream.WriteByte(58);
				CitoMemoryStream ms27 = new CitoMemoryStream(subBuffer);
				Packet_BehaviorSerializer.Serialize(ms27, i14);
				int len22 = ms27.Position();
				ProtocolParser.WriteUInt32_(stream, len22);
				stream.Write(ms27.GetBuffer(), 0, len22);
			}
		}
		if (instance.EntityBehaviors != null)
		{
			stream.WriteKey(84, 2);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.EntityBehaviors));
		}
		if (instance.RenderPass != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.RenderPass);
		}
		if (instance.DrawType != 0)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt32(stream, instance.DrawType);
		}
		if (instance.MatterState != 0)
		{
			stream.WriteByte(80);
			ProtocolParser.WriteUInt32(stream, instance.MatterState);
		}
		if (instance.WalkSpeedFloat != 0)
		{
			stream.WriteByte(88);
			ProtocolParser.WriteUInt32(stream, instance.WalkSpeedFloat);
		}
		if (instance.IsSlipperyWalk)
		{
			stream.WriteByte(96);
			ProtocolParser.WriteBool(stream, instance.IsSlipperyWalk);
		}
		if (instance.Sounds != null)
		{
			stream.WriteByte(106);
			CitoMemoryStream ms13 = new CitoMemoryStream(subBuffer);
			Packet_BlockSoundSetSerializer.Serialize(ms13, instance.Sounds);
			int len21 = ms13.Position();
			ProtocolParser.WriteUInt32_(stream, len21);
			stream.Write(ms13.GetBuffer(), 0, len21);
		}
		if (instance.HeldSounds != null)
		{
			stream.WriteKey(83, 2);
			CitoMemoryStream ms30 = new CitoMemoryStream(subBuffer);
			Packet_HeldSoundSetSerializer.Serialize(ms30, instance.HeldSounds);
			int len20 = ms30.Position();
			ProtocolParser.WriteUInt32_(stream, len20);
			stream.Write(ms30.GetBuffer(), 0, len20);
		}
		if (instance.LightHsv != null)
		{
			for (int k10 = 0; k10 < instance.LightHsvCount; k10++)
			{
				int i2 = instance.LightHsv[k10];
				stream.WriteByte(112);
				ProtocolParser.WriteUInt32(stream, i2);
			}
		}
		if (instance.VertexFlags != 0)
		{
			stream.WriteKey(51, 0);
			ProtocolParser.WriteUInt32(stream, instance.VertexFlags);
		}
		if (instance.Climbable != 0)
		{
			stream.WriteByte(120);
			ProtocolParser.WriteUInt32(stream, instance.Climbable);
		}
		if (instance.CreativeInventoryTabs != null)
		{
			for (int k9 = 0; k9 < instance.CreativeInventoryTabsCount; k9++)
			{
				string i3 = instance.CreativeInventoryTabs[k9];
				stream.WriteKey(16, 2);
				ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(i3));
			}
		}
		if (instance.CreativeInventoryStacks != null)
		{
			stream.WriteKey(17, 2);
			ProtocolParser.WriteBytes(stream, instance.CreativeInventoryStacks);
		}
		if (instance.SideOpaqueFlags != null)
		{
			for (int k8 = 0; k8 < instance.SideOpaqueFlagsCount; k8++)
			{
				int i5 = instance.SideOpaqueFlags[k8];
				stream.WriteKey(24, 0);
				ProtocolParser.WriteUInt32(stream, i5);
			}
		}
		if (instance.FaceCullMode != 0)
		{
			stream.WriteKey(23, 0);
			ProtocolParser.WriteUInt32(stream, instance.FaceCullMode);
		}
		if (instance.SideSolidFlags != null)
		{
			for (int k7 = 0; k7 < instance.SideSolidFlagsCount; k7++)
			{
				int i10 = instance.SideSolidFlags[k7];
				stream.WriteKey(46, 0);
				ProtocolParser.WriteUInt32(stream, i10);
			}
		}
		if (instance.SeasonColorMap != null)
		{
			stream.WriteKey(25, 2);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.SeasonColorMap));
		}
		if (instance.ClimateColorMap != null)
		{
			stream.WriteKey(88, 2);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.ClimateColorMap));
		}
		if (instance.CullFaces != 0)
		{
			stream.WriteKey(26, 0);
			ProtocolParser.WriteUInt32(stream, instance.CullFaces);
		}
		if (instance.Replacable != 0)
		{
			stream.WriteKey(27, 0);
			ProtocolParser.WriteUInt32(stream, instance.Replacable);
		}
		if (instance.LightAbsorption != 0)
		{
			stream.WriteKey(29, 0);
			ProtocolParser.WriteUInt32(stream, instance.LightAbsorption);
		}
		if (instance.HardnessLevel != 0)
		{
			stream.WriteKey(30, 0);
			ProtocolParser.WriteUInt32(stream, instance.HardnessLevel);
		}
		if (instance.Resistance != 0)
		{
			stream.WriteKey(31, 0);
			ProtocolParser.WriteUInt32(stream, instance.Resistance);
		}
		if (instance.BlockMaterial != 0)
		{
			stream.WriteKey(32, 0);
			ProtocolParser.WriteUInt32(stream, instance.BlockMaterial);
		}
		if (instance.Moddata != null)
		{
			stream.WriteKey(33, 2);
			ProtocolParser.WriteBytes(stream, instance.Moddata);
		}
		if (instance.Shape != null)
		{
			stream.WriteKey(34, 2);
			CitoMemoryStream ms15 = new CitoMemoryStream(subBuffer);
			Packet_CompositeShapeSerializer.Serialize(ms15, instance.Shape);
			int len19 = ms15.Position();
			ProtocolParser.WriteUInt32_(stream, len19);
			stream.Write(ms15.GetBuffer(), 0, len19);
		}
		if (instance.ShapeInventory != null)
		{
			stream.WriteKey(35, 2);
			CitoMemoryStream ms16 = new CitoMemoryStream(subBuffer);
			Packet_CompositeShapeSerializer.Serialize(ms16, instance.ShapeInventory);
			int len18 = ms16.Position();
			ProtocolParser.WriteUInt32_(stream, len18);
			stream.Write(ms16.GetBuffer(), 0, len18);
		}
		if (instance.Ambientocclusion != 0)
		{
			stream.WriteKey(38, 0);
			ProtocolParser.WriteUInt32(stream, instance.Ambientocclusion);
		}
		if (instance.CollisionBoxes != null)
		{
			for (int k6 = 0; k6 < instance.CollisionBoxesCount; k6++)
			{
				Packet_Cube i7 = instance.CollisionBoxes[k6];
				stream.WriteKey(39, 2);
				CitoMemoryStream ms17 = new CitoMemoryStream(subBuffer);
				Packet_CubeSerializer.Serialize(ms17, i7);
				int len17 = ms17.Position();
				ProtocolParser.WriteUInt32_(stream, len17);
				stream.Write(ms17.GetBuffer(), 0, len17);
			}
		}
		if (instance.SelectionBoxes != null)
		{
			for (int k5 = 0; k5 < instance.SelectionBoxesCount; k5++)
			{
				Packet_Cube i9 = instance.SelectionBoxes[k5];
				stream.WriteKey(40, 2);
				CitoMemoryStream ms19 = new CitoMemoryStream(subBuffer);
				Packet_CubeSerializer.Serialize(ms19, i9);
				int len16 = ms19.Position();
				ProtocolParser.WriteUInt32_(stream, len16);
				stream.Write(ms19.GetBuffer(), 0, len16);
			}
		}
		if (instance.ParticleCollisionBoxes != null)
		{
			for (int k4 = 0; k4 < instance.ParticleCollisionBoxesCount; k4++)
			{
				Packet_Cube i19 = instance.ParticleCollisionBoxes[k4];
				stream.WriteKey(91, 2);
				CitoMemoryStream ms33 = new CitoMemoryStream(subBuffer);
				Packet_CubeSerializer.Serialize(ms33, i19);
				int len15 = ms33.Position();
				ProtocolParser.WriteUInt32_(stream, len15);
				stream.Write(ms33.GetBuffer(), 0, len15);
			}
		}
		if (instance.Blockclass != null)
		{
			stream.WriteKey(41, 2);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Blockclass));
		}
		if (instance.GuiTransform != null)
		{
			stream.WriteKey(42, 2);
			CitoMemoryStream ms20 = new CitoMemoryStream(subBuffer);
			Packet_ModelTransformSerializer.Serialize(ms20, instance.GuiTransform);
			int len14 = ms20.Position();
			ProtocolParser.WriteUInt32_(stream, len14);
			stream.Write(ms20.GetBuffer(), 0, len14);
		}
		if (instance.FpHandTransform != null)
		{
			stream.WriteKey(43, 2);
			CitoMemoryStream ms21 = new CitoMemoryStream(subBuffer);
			Packet_ModelTransformSerializer.Serialize(ms21, instance.FpHandTransform);
			int len13 = ms21.Position();
			ProtocolParser.WriteUInt32_(stream, len13);
			stream.Write(ms21.GetBuffer(), 0, len13);
		}
		if (instance.TpHandTransform != null)
		{
			stream.WriteKey(44, 2);
			CitoMemoryStream ms22 = new CitoMemoryStream(subBuffer);
			Packet_ModelTransformSerializer.Serialize(ms22, instance.TpHandTransform);
			int len12 = ms22.Position();
			ProtocolParser.WriteUInt32_(stream, len12);
			stream.Write(ms22.GetBuffer(), 0, len12);
		}
		if (instance.TpOffHandTransform != null)
		{
			stream.WriteKey(99, 2);
			CitoMemoryStream ms36 = new CitoMemoryStream(subBuffer);
			Packet_ModelTransformSerializer.Serialize(ms36, instance.TpOffHandTransform);
			int len11 = ms36.Position();
			ProtocolParser.WriteUInt32_(stream, len11);
			stream.Write(ms36.GetBuffer(), 0, len11);
		}
		if (instance.GroundTransform != null)
		{
			stream.WriteKey(45, 2);
			CitoMemoryStream ms23 = new CitoMemoryStream(subBuffer);
			Packet_ModelTransformSerializer.Serialize(ms23, instance.GroundTransform);
			int len10 = ms23.Position();
			ProtocolParser.WriteUInt32_(stream, len10);
			stream.Write(ms23.GetBuffer(), 0, len10);
		}
		if (instance.Fertility != 0)
		{
			stream.WriteKey(47, 0);
			ProtocolParser.WriteUInt32(stream, instance.Fertility);
		}
		if (instance.ParticleProperties != null)
		{
			stream.WriteKey(48, 2);
			ProtocolParser.WriteBytes(stream, instance.ParticleProperties);
		}
		if (instance.ParticlePropertiesQuantity != 0)
		{
			stream.WriteKey(49, 0);
			ProtocolParser.WriteUInt32(stream, instance.ParticlePropertiesQuantity);
		}
		if (instance.RandomDrawOffset != 0)
		{
			stream.WriteKey(50, 0);
			ProtocolParser.WriteUInt32(stream, instance.RandomDrawOffset);
		}
		if (instance.RandomizeAxes != 0)
		{
			stream.WriteKey(69, 0);
			ProtocolParser.WriteUInt32(stream, instance.RandomizeAxes);
		}
		if (instance.RandomizeRotations != 0)
		{
			stream.WriteKey(87, 0);
			ProtocolParser.WriteUInt32(stream, instance.RandomizeRotations);
		}
		if (instance.Drops != null)
		{
			for (int k3 = 0; k3 < instance.DropsCount; k3++)
			{
				Packet_BlockDrop i11 = instance.Drops[k3];
				stream.WriteKey(52, 2);
				CitoMemoryStream ms24 = new CitoMemoryStream(subBuffer);
				Packet_BlockDropSerializer.Serialize(ms24, i11);
				int len9 = ms24.Position();
				ProtocolParser.WriteUInt32_(stream, len9);
				stream.Write(ms24.GetBuffer(), 0, len9);
			}
		}
		if (instance.LiquidLevel != 0)
		{
			stream.WriteKey(53, 0);
			ProtocolParser.WriteUInt32(stream, instance.LiquidLevel);
		}
		if (instance.Attributes != null)
		{
			stream.WriteKey(54, 2);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Attributes));
		}
		if (instance.CombustibleProps != null)
		{
			stream.WriteKey(55, 2);
			CitoMemoryStream ms25 = new CitoMemoryStream(subBuffer);
			Packet_CombustiblePropertiesSerializer.Serialize(ms25, instance.CombustibleProps);
			int len8 = ms25.Position();
			ProtocolParser.WriteUInt32_(stream, len8);
			stream.Write(ms25.GetBuffer(), 0, len8);
		}
		if (instance.SideAo != null)
		{
			for (int k2 = 0; k2 < instance.SideAoCount; k2++)
			{
				int i12 = instance.SideAo[k2];
				stream.WriteKey(57, 0);
				ProtocolParser.WriteUInt32(stream, i12);
			}
		}
		if (instance.NeighbourSideAo != 0)
		{
			stream.WriteKey(79, 0);
			ProtocolParser.WriteUInt32(stream, instance.NeighbourSideAo);
		}
		if (instance.GrindingProps != null)
		{
			stream.WriteKey(77, 2);
			CitoMemoryStream ms28 = new CitoMemoryStream(subBuffer);
			Packet_GrindingPropertiesSerializer.Serialize(ms28, instance.GrindingProps);
			int len7 = ms28.Position();
			ProtocolParser.WriteUInt32_(stream, len7);
			stream.Write(ms28.GetBuffer(), 0, len7);
		}
		if (instance.NutritionProps != null)
		{
			stream.WriteKey(59, 2);
			CitoMemoryStream ms26 = new CitoMemoryStream(subBuffer);
			Packet_NutritionPropertiesSerializer.Serialize(ms26, instance.NutritionProps);
			int len6 = ms26.Position();
			ProtocolParser.WriteUInt32_(stream, len6);
			stream.Write(ms26.GetBuffer(), 0, len6);
		}
		if (instance.TransitionableProps != null)
		{
			for (int n = 0; n < instance.TransitionablePropsCount; n++)
			{
				Packet_TransitionableProperties i17 = instance.TransitionableProps[n];
				stream.WriteKey(85, 2);
				CitoMemoryStream ms31 = new CitoMemoryStream(subBuffer);
				Packet_TransitionablePropertiesSerializer.Serialize(ms31, i17);
				int len5 = ms31.Position();
				ProtocolParser.WriteUInt32_(stream, len5);
				stream.Write(ms31.GetBuffer(), 0, len5);
			}
		}
		if (instance.MaxStackSize != 0)
		{
			stream.WriteKey(60, 0);
			ProtocolParser.WriteUInt32(stream, instance.MaxStackSize);
		}
		if (instance.CropProps != null)
		{
			stream.WriteKey(61, 2);
			ProtocolParser.WriteBytes(stream, instance.CropProps);
		}
		if (instance.CropPropBehaviors != null)
		{
			for (int m = 0; m < instance.CropPropBehaviorsCount; m++)
			{
				string i18 = instance.CropPropBehaviors[m];
				stream.WriteKey(90, 2);
				ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(i18));
			}
		}
		if (instance.MaterialDensity != 0)
		{
			stream.WriteKey(62, 0);
			ProtocolParser.WriteUInt32(stream, instance.MaterialDensity);
		}
		if (instance.AttackPower != 0)
		{
			stream.WriteKey(63, 0);
			ProtocolParser.WriteUInt32(stream, instance.AttackPower);
		}
		if (instance.AttackRange != 0)
		{
			stream.WriteKey(70, 0);
			ProtocolParser.WriteUInt32(stream, instance.AttackRange);
		}
		if (instance.LiquidSelectable != 0)
		{
			stream.WriteKey(64, 0);
			ProtocolParser.WriteUInt32(stream, instance.LiquidSelectable);
		}
		if (instance.MiningTier != 0)
		{
			stream.WriteKey(65, 0);
			ProtocolParser.WriteUInt32(stream, instance.MiningTier);
		}
		if (instance.RequiredMiningTier != 0)
		{
			stream.WriteKey(66, 0);
			ProtocolParser.WriteUInt32(stream, instance.RequiredMiningTier);
		}
		if (instance.Miningmaterial != null)
		{
			for (int l = 0; l < instance.MiningmaterialCount; l++)
			{
				int i13 = instance.Miningmaterial[l];
				stream.WriteKey(67, 0);
				ProtocolParser.WriteUInt32(stream, i13);
			}
		}
		if (instance.Miningmaterialspeed != null)
		{
			for (int k = 0; k < instance.MiningmaterialspeedCount; k++)
			{
				int i15 = instance.Miningmaterialspeed[k];
				stream.WriteKey(76, 0);
				ProtocolParser.WriteUInt32(stream, i15);
			}
		}
		if (instance.DragMultiplierFloat != 0)
		{
			stream.WriteKey(68, 0);
			ProtocolParser.WriteUInt32(stream, instance.DragMultiplierFloat);
		}
		if (instance.StorageFlags != 0)
		{
			stream.WriteKey(71, 0);
			ProtocolParser.WriteUInt32(stream, instance.StorageFlags);
		}
		if (instance.RenderAlphaTest != 0)
		{
			stream.WriteKey(72, 0);
			ProtocolParser.WriteUInt32(stream, instance.RenderAlphaTest);
		}
		if (instance.HeldTpHitAnimation != null)
		{
			stream.WriteKey(73, 2);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.HeldTpHitAnimation));
		}
		if (instance.HeldRightTpIdleAnimation != null)
		{
			stream.WriteKey(74, 2);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.HeldRightTpIdleAnimation));
		}
		if (instance.HeldLeftTpIdleAnimation != null)
		{
			stream.WriteKey(80, 2);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.HeldLeftTpIdleAnimation));
		}
		if (instance.HeldTpUseAnimation != null)
		{
			stream.WriteKey(75, 2);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.HeldTpUseAnimation));
		}
		if (instance.RainPermeable != 0)
		{
			stream.WriteKey(78, 0);
			ProtocolParser.WriteUInt32(stream, instance.RainPermeable);
		}
		if (instance.LiquidCode != null)
		{
			stream.WriteKey(81, 2);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.LiquidCode));
		}
		if (instance.Variant != null)
		{
			for (int j = 0; j < instance.VariantCount; j++)
			{
				Packet_VariantPart i16 = instance.Variant[j];
				stream.WriteKey(82, 2);
				CitoMemoryStream ms29 = new CitoMemoryStream(subBuffer);
				Packet_VariantPartSerializer.Serialize(ms29, i16);
				int len4 = ms29.Position();
				ProtocolParser.WriteUInt32_(stream, len4);
				stream.Write(ms29.GetBuffer(), 0, len4);
			}
		}
		if (instance.Lod0shape != null)
		{
			stream.WriteKey(86, 2);
			CitoMemoryStream ms32 = new CitoMemoryStream(subBuffer);
			Packet_CompositeShapeSerializer.Serialize(ms32, instance.Lod0shape);
			int len3 = ms32.Position();
			ProtocolParser.WriteUInt32_(stream, len3);
			stream.Write(ms32.GetBuffer(), 0, len3);
		}
		if (instance.Frostable != 0)
		{
			stream.WriteKey(89, 0);
			ProtocolParser.WriteUInt32(stream, instance.Frostable);
		}
		if (instance.CrushingProps != null)
		{
			stream.WriteKey(92, 2);
			CitoMemoryStream ms34 = new CitoMemoryStream(subBuffer);
			Packet_CrushingPropertiesSerializer.Serialize(ms34, instance.CrushingProps);
			int len2 = ms34.Position();
			ProtocolParser.WriteUInt32_(stream, len2);
			stream.Write(ms34.GetBuffer(), 0, len2);
		}
		if (instance.RandomSizeAdjust != 0)
		{
			stream.WriteKey(93, 0);
			ProtocolParser.WriteUInt32(stream, instance.RandomSizeAdjust);
		}
		if (instance.Lod2shape != null)
		{
			stream.WriteKey(94, 2);
			CitoMemoryStream ms35 = new CitoMemoryStream(subBuffer);
			Packet_CompositeShapeSerializer.Serialize(ms35, instance.Lod2shape);
			int len = ms35.Position();
			ProtocolParser.WriteUInt32_(stream, len);
			stream.Write(ms35.GetBuffer(), 0, len);
		}
		if (instance.DoNotRenderAtLod2 != 0)
		{
			stream.WriteKey(95, 0);
			ProtocolParser.WriteUInt32(stream, instance.DoNotRenderAtLod2);
		}
		if (instance.Width != 0)
		{
			stream.WriteKey(96, 0);
			ProtocolParser.WriteUInt32(stream, instance.Width);
		}
		if (instance.Height != 0)
		{
			stream.WriteKey(97, 0);
			ProtocolParser.WriteUInt32(stream, instance.Height);
		}
		if (instance.Length != 0)
		{
			stream.WriteKey(98, 0);
			ProtocolParser.WriteUInt32(stream, instance.Length);
		}
		if (instance.IsMissing != 0)
		{
			stream.WriteKey(100, 0);
			ProtocolParser.WriteUInt32(stream, instance.IsMissing);
		}
		if (instance.Durability != 0)
		{
			stream.WriteKey(101, 0);
			ProtocolParser.WriteUInt32(stream, instance.Durability);
		}
		if (instance.HeldLeftReadyAnimation != null)
		{
			stream.WriteKey(102, 2);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.HeldLeftReadyAnimation));
		}
		if (instance.HeldRightReadyAnimation != null)
		{
			stream.WriteKey(103, 2);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.HeldRightReadyAnimation));
		}
	}

	public static byte[] SerializeToBytes(Packet_BlockType instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_BlockType instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
