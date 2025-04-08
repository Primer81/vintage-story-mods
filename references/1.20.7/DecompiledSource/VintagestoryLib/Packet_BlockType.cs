public class Packet_BlockType
{
	public string[] TextureCodes;

	public int TextureCodesCount;

	public int TextureCodesLength;

	public Packet_CompositeTexture[] CompositeTextures;

	public int CompositeTexturesCount;

	public int CompositeTexturesLength;

	public string[] InventoryTextureCodes;

	public int InventoryTextureCodesCount;

	public int InventoryTextureCodesLength;

	public Packet_CompositeTexture[] InventoryCompositeTextures;

	public int InventoryCompositeTexturesCount;

	public int InventoryCompositeTexturesLength;

	public int BlockId;

	public string Code;

	public string EntityClass;

	public Packet_Behavior[] Behaviors;

	public int BehaviorsCount;

	public int BehaviorsLength;

	public string EntityBehaviors;

	public int RenderPass;

	public int DrawType;

	public int MatterState;

	public int WalkSpeedFloat;

	public bool IsSlipperyWalk;

	public Packet_BlockSoundSet Sounds;

	public Packet_HeldSoundSet HeldSounds;

	public int[] LightHsv;

	public int LightHsvCount;

	public int LightHsvLength;

	public int VertexFlags;

	public int Climbable;

	public string[] CreativeInventoryTabs;

	public int CreativeInventoryTabsCount;

	public int CreativeInventoryTabsLength;

	public byte[] CreativeInventoryStacks;

	public int[] SideOpaqueFlags;

	public int SideOpaqueFlagsCount;

	public int SideOpaqueFlagsLength;

	public int FaceCullMode;

	public int[] SideSolidFlags;

	public int SideSolidFlagsCount;

	public int SideSolidFlagsLength;

	public string SeasonColorMap;

	public string ClimateColorMap;

	public int CullFaces;

	public int Replacable;

	public int LightAbsorption;

	public int HardnessLevel;

	public int Resistance;

	public int BlockMaterial;

	public byte[] Moddata;

	public Packet_CompositeShape Shape;

	public Packet_CompositeShape ShapeInventory;

	public int Ambientocclusion;

	public Packet_Cube[] CollisionBoxes;

	public int CollisionBoxesCount;

	public int CollisionBoxesLength;

	public Packet_Cube[] SelectionBoxes;

	public int SelectionBoxesCount;

	public int SelectionBoxesLength;

	public Packet_Cube[] ParticleCollisionBoxes;

	public int ParticleCollisionBoxesCount;

	public int ParticleCollisionBoxesLength;

	public string Blockclass;

	public Packet_ModelTransform GuiTransform;

	public Packet_ModelTransform FpHandTransform;

	public Packet_ModelTransform TpHandTransform;

	public Packet_ModelTransform TpOffHandTransform;

	public Packet_ModelTransform GroundTransform;

	public int Fertility;

	public byte[] ParticleProperties;

	public int ParticlePropertiesQuantity;

	public int RandomDrawOffset;

	public int RandomizeAxes;

	public int RandomizeRotations;

	public Packet_BlockDrop[] Drops;

	public int DropsCount;

	public int DropsLength;

	public int LiquidLevel;

	public string Attributes;

	public Packet_CombustibleProperties CombustibleProps;

	public int[] SideAo;

	public int SideAoCount;

	public int SideAoLength;

	public int NeighbourSideAo;

	public Packet_GrindingProperties GrindingProps;

	public Packet_NutritionProperties NutritionProps;

	public Packet_TransitionableProperties[] TransitionableProps;

	public int TransitionablePropsCount;

	public int TransitionablePropsLength;

	public int MaxStackSize;

	public byte[] CropProps;

	public string[] CropPropBehaviors;

	public int CropPropBehaviorsCount;

	public int CropPropBehaviorsLength;

	public int MaterialDensity;

	public int AttackPower;

	public int AttackRange;

	public int LiquidSelectable;

	public int MiningTier;

	public int RequiredMiningTier;

	public int[] Miningmaterial;

	public int MiningmaterialCount;

	public int MiningmaterialLength;

	public int[] Miningmaterialspeed;

	public int MiningmaterialspeedCount;

	public int MiningmaterialspeedLength;

	public int DragMultiplierFloat;

	public int StorageFlags;

	public int RenderAlphaTest;

	public string HeldTpHitAnimation;

	public string HeldRightTpIdleAnimation;

	public string HeldLeftTpIdleAnimation;

	public string HeldTpUseAnimation;

	public int RainPermeable;

	public string LiquidCode;

	public Packet_VariantPart[] Variant;

	public int VariantCount;

	public int VariantLength;

	public Packet_CompositeShape Lod0shape;

	public int Frostable;

	public Packet_CrushingProperties CrushingProps;

	public int RandomSizeAdjust;

	public Packet_CompositeShape Lod2shape;

	public int DoNotRenderAtLod2;

	public int Width;

	public int Height;

	public int Length;

	public int IsMissing;

	public int Durability;

	public string HeldLeftReadyAnimation;

	public string HeldRightReadyAnimation;

	public const int TextureCodesFieldID = 1;

	public const int CompositeTexturesFieldID = 2;

	public const int InventoryTextureCodesFieldID = 3;

	public const int InventoryCompositeTexturesFieldID = 4;

	public const int BlockIdFieldID = 5;

	public const int CodeFieldID = 6;

	public const int EntityClassFieldID = 58;

	public const int BehaviorsFieldID = 7;

	public const int EntityBehaviorsFieldID = 84;

	public const int RenderPassFieldID = 8;

	public const int DrawTypeFieldID = 9;

	public const int MatterStateFieldID = 10;

	public const int WalkSpeedFloatFieldID = 11;

	public const int IsSlipperyWalkFieldID = 12;

	public const int SoundsFieldID = 13;

	public const int HeldSoundsFieldID = 83;

	public const int LightHsvFieldID = 14;

	public const int VertexFlagsFieldID = 51;

	public const int ClimbableFieldID = 15;

	public const int CreativeInventoryTabsFieldID = 16;

	public const int CreativeInventoryStacksFieldID = 17;

	public const int SideOpaqueFlagsFieldID = 24;

	public const int FaceCullModeFieldID = 23;

	public const int SideSolidFlagsFieldID = 46;

	public const int SeasonColorMapFieldID = 25;

	public const int ClimateColorMapFieldID = 88;

	public const int CullFacesFieldID = 26;

	public const int ReplacableFieldID = 27;

	public const int LightAbsorptionFieldID = 29;

	public const int HardnessLevelFieldID = 30;

	public const int ResistanceFieldID = 31;

	public const int BlockMaterialFieldID = 32;

	public const int ModdataFieldID = 33;

	public const int ShapeFieldID = 34;

	public const int ShapeInventoryFieldID = 35;

	public const int AmbientocclusionFieldID = 38;

	public const int CollisionBoxesFieldID = 39;

	public const int SelectionBoxesFieldID = 40;

	public const int ParticleCollisionBoxesFieldID = 91;

	public const int BlockclassFieldID = 41;

	public const int GuiTransformFieldID = 42;

	public const int FpHandTransformFieldID = 43;

	public const int TpHandTransformFieldID = 44;

	public const int TpOffHandTransformFieldID = 99;

	public const int GroundTransformFieldID = 45;

	public const int FertilityFieldID = 47;

	public const int ParticlePropertiesFieldID = 48;

	public const int ParticlePropertiesQuantityFieldID = 49;

	public const int RandomDrawOffsetFieldID = 50;

	public const int RandomizeAxesFieldID = 69;

	public const int RandomizeRotationsFieldID = 87;

	public const int DropsFieldID = 52;

	public const int LiquidLevelFieldID = 53;

	public const int AttributesFieldID = 54;

	public const int CombustiblePropsFieldID = 55;

	public const int SideAoFieldID = 57;

	public const int NeighbourSideAoFieldID = 79;

	public const int GrindingPropsFieldID = 77;

	public const int NutritionPropsFieldID = 59;

	public const int TransitionablePropsFieldID = 85;

	public const int MaxStackSizeFieldID = 60;

	public const int CropPropsFieldID = 61;

	public const int CropPropBehaviorsFieldID = 90;

	public const int MaterialDensityFieldID = 62;

	public const int AttackPowerFieldID = 63;

	public const int AttackRangeFieldID = 70;

	public const int LiquidSelectableFieldID = 64;

	public const int MiningTierFieldID = 65;

	public const int RequiredMiningTierFieldID = 66;

	public const int MiningmaterialFieldID = 67;

	public const int MiningmaterialspeedFieldID = 76;

	public const int DragMultiplierFloatFieldID = 68;

	public const int StorageFlagsFieldID = 71;

	public const int RenderAlphaTestFieldID = 72;

	public const int HeldTpHitAnimationFieldID = 73;

	public const int HeldRightTpIdleAnimationFieldID = 74;

	public const int HeldLeftTpIdleAnimationFieldID = 80;

	public const int HeldTpUseAnimationFieldID = 75;

	public const int RainPermeableFieldID = 78;

	public const int LiquidCodeFieldID = 81;

	public const int VariantFieldID = 82;

	public const int Lod0shapeFieldID = 86;

	public const int FrostableFieldID = 89;

	public const int CrushingPropsFieldID = 92;

	public const int RandomSizeAdjustFieldID = 93;

	public const int Lod2shapeFieldID = 94;

	public const int DoNotRenderAtLod2FieldID = 95;

	public const int WidthFieldID = 96;

	public const int HeightFieldID = 97;

	public const int LengthFieldID = 98;

	public const int IsMissingFieldID = 100;

	public const int DurabilityFieldID = 101;

	public const int HeldLeftReadyAnimationFieldID = 102;

	public const int HeldRightReadyAnimationFieldID = 103;

	public string[] GetTextureCodes()
	{
		return TextureCodes;
	}

	public void SetTextureCodes(string[] value, int count, int length)
	{
		TextureCodes = value;
		TextureCodesCount = count;
		TextureCodesLength = length;
	}

	public void SetTextureCodes(string[] value)
	{
		TextureCodes = value;
		TextureCodesCount = value.Length;
		TextureCodesLength = value.Length;
	}

	public int GetTextureCodesCount()
	{
		return TextureCodesCount;
	}

	public void TextureCodesAdd(string value)
	{
		if (TextureCodesCount >= TextureCodesLength)
		{
			if ((TextureCodesLength *= 2) == 0)
			{
				TextureCodesLength = 1;
			}
			string[] newArray = new string[TextureCodesLength];
			for (int i = 0; i < TextureCodesCount; i++)
			{
				newArray[i] = TextureCodes[i];
			}
			TextureCodes = newArray;
		}
		TextureCodes[TextureCodesCount++] = value;
	}

	public Packet_CompositeTexture[] GetCompositeTextures()
	{
		return CompositeTextures;
	}

	public void SetCompositeTextures(Packet_CompositeTexture[] value, int count, int length)
	{
		CompositeTextures = value;
		CompositeTexturesCount = count;
		CompositeTexturesLength = length;
	}

	public void SetCompositeTextures(Packet_CompositeTexture[] value)
	{
		CompositeTextures = value;
		CompositeTexturesCount = value.Length;
		CompositeTexturesLength = value.Length;
	}

	public int GetCompositeTexturesCount()
	{
		return CompositeTexturesCount;
	}

	public void CompositeTexturesAdd(Packet_CompositeTexture value)
	{
		if (CompositeTexturesCount >= CompositeTexturesLength)
		{
			if ((CompositeTexturesLength *= 2) == 0)
			{
				CompositeTexturesLength = 1;
			}
			Packet_CompositeTexture[] newArray = new Packet_CompositeTexture[CompositeTexturesLength];
			for (int i = 0; i < CompositeTexturesCount; i++)
			{
				newArray[i] = CompositeTextures[i];
			}
			CompositeTextures = newArray;
		}
		CompositeTextures[CompositeTexturesCount++] = value;
	}

	public string[] GetInventoryTextureCodes()
	{
		return InventoryTextureCodes;
	}

	public void SetInventoryTextureCodes(string[] value, int count, int length)
	{
		InventoryTextureCodes = value;
		InventoryTextureCodesCount = count;
		InventoryTextureCodesLength = length;
	}

	public void SetInventoryTextureCodes(string[] value)
	{
		InventoryTextureCodes = value;
		InventoryTextureCodesCount = value.Length;
		InventoryTextureCodesLength = value.Length;
	}

	public int GetInventoryTextureCodesCount()
	{
		return InventoryTextureCodesCount;
	}

	public void InventoryTextureCodesAdd(string value)
	{
		if (InventoryTextureCodesCount >= InventoryTextureCodesLength)
		{
			if ((InventoryTextureCodesLength *= 2) == 0)
			{
				InventoryTextureCodesLength = 1;
			}
			string[] newArray = new string[InventoryTextureCodesLength];
			for (int i = 0; i < InventoryTextureCodesCount; i++)
			{
				newArray[i] = InventoryTextureCodes[i];
			}
			InventoryTextureCodes = newArray;
		}
		InventoryTextureCodes[InventoryTextureCodesCount++] = value;
	}

	public Packet_CompositeTexture[] GetInventoryCompositeTextures()
	{
		return InventoryCompositeTextures;
	}

	public void SetInventoryCompositeTextures(Packet_CompositeTexture[] value, int count, int length)
	{
		InventoryCompositeTextures = value;
		InventoryCompositeTexturesCount = count;
		InventoryCompositeTexturesLength = length;
	}

	public void SetInventoryCompositeTextures(Packet_CompositeTexture[] value)
	{
		InventoryCompositeTextures = value;
		InventoryCompositeTexturesCount = value.Length;
		InventoryCompositeTexturesLength = value.Length;
	}

	public int GetInventoryCompositeTexturesCount()
	{
		return InventoryCompositeTexturesCount;
	}

	public void InventoryCompositeTexturesAdd(Packet_CompositeTexture value)
	{
		if (InventoryCompositeTexturesCount >= InventoryCompositeTexturesLength)
		{
			if ((InventoryCompositeTexturesLength *= 2) == 0)
			{
				InventoryCompositeTexturesLength = 1;
			}
			Packet_CompositeTexture[] newArray = new Packet_CompositeTexture[InventoryCompositeTexturesLength];
			for (int i = 0; i < InventoryCompositeTexturesCount; i++)
			{
				newArray[i] = InventoryCompositeTextures[i];
			}
			InventoryCompositeTextures = newArray;
		}
		InventoryCompositeTextures[InventoryCompositeTexturesCount++] = value;
	}

	public void SetBlockId(int value)
	{
		BlockId = value;
	}

	public void SetCode(string value)
	{
		Code = value;
	}

	public void SetEntityClass(string value)
	{
		EntityClass = value;
	}

	public Packet_Behavior[] GetBehaviors()
	{
		return Behaviors;
	}

	public void SetBehaviors(Packet_Behavior[] value, int count, int length)
	{
		Behaviors = value;
		BehaviorsCount = count;
		BehaviorsLength = length;
	}

	public void SetBehaviors(Packet_Behavior[] value)
	{
		Behaviors = value;
		BehaviorsCount = value.Length;
		BehaviorsLength = value.Length;
	}

	public int GetBehaviorsCount()
	{
		return BehaviorsCount;
	}

	public void BehaviorsAdd(Packet_Behavior value)
	{
		if (BehaviorsCount >= BehaviorsLength)
		{
			if ((BehaviorsLength *= 2) == 0)
			{
				BehaviorsLength = 1;
			}
			Packet_Behavior[] newArray = new Packet_Behavior[BehaviorsLength];
			for (int i = 0; i < BehaviorsCount; i++)
			{
				newArray[i] = Behaviors[i];
			}
			Behaviors = newArray;
		}
		Behaviors[BehaviorsCount++] = value;
	}

	public void SetEntityBehaviors(string value)
	{
		EntityBehaviors = value;
	}

	public void SetRenderPass(int value)
	{
		RenderPass = value;
	}

	public void SetDrawType(int value)
	{
		DrawType = value;
	}

	public void SetMatterState(int value)
	{
		MatterState = value;
	}

	public void SetWalkSpeedFloat(int value)
	{
		WalkSpeedFloat = value;
	}

	public void SetIsSlipperyWalk(bool value)
	{
		IsSlipperyWalk = value;
	}

	public void SetSounds(Packet_BlockSoundSet value)
	{
		Sounds = value;
	}

	public void SetHeldSounds(Packet_HeldSoundSet value)
	{
		HeldSounds = value;
	}

	public int[] GetLightHsv()
	{
		return LightHsv;
	}

	public void SetLightHsv(int[] value, int count, int length)
	{
		LightHsv = value;
		LightHsvCount = count;
		LightHsvLength = length;
	}

	public void SetLightHsv(int[] value)
	{
		LightHsv = value;
		LightHsvCount = value.Length;
		LightHsvLength = value.Length;
	}

	public int GetLightHsvCount()
	{
		return LightHsvCount;
	}

	public void LightHsvAdd(int value)
	{
		if (LightHsvCount >= LightHsvLength)
		{
			if ((LightHsvLength *= 2) == 0)
			{
				LightHsvLength = 1;
			}
			int[] newArray = new int[LightHsvLength];
			for (int i = 0; i < LightHsvCount; i++)
			{
				newArray[i] = LightHsv[i];
			}
			LightHsv = newArray;
		}
		LightHsv[LightHsvCount++] = value;
	}

	public void SetVertexFlags(int value)
	{
		VertexFlags = value;
	}

	public void SetClimbable(int value)
	{
		Climbable = value;
	}

	public string[] GetCreativeInventoryTabs()
	{
		return CreativeInventoryTabs;
	}

	public void SetCreativeInventoryTabs(string[] value, int count, int length)
	{
		CreativeInventoryTabs = value;
		CreativeInventoryTabsCount = count;
		CreativeInventoryTabsLength = length;
	}

	public void SetCreativeInventoryTabs(string[] value)
	{
		CreativeInventoryTabs = value;
		CreativeInventoryTabsCount = value.Length;
		CreativeInventoryTabsLength = value.Length;
	}

	public int GetCreativeInventoryTabsCount()
	{
		return CreativeInventoryTabsCount;
	}

	public void CreativeInventoryTabsAdd(string value)
	{
		if (CreativeInventoryTabsCount >= CreativeInventoryTabsLength)
		{
			if ((CreativeInventoryTabsLength *= 2) == 0)
			{
				CreativeInventoryTabsLength = 1;
			}
			string[] newArray = new string[CreativeInventoryTabsLength];
			for (int i = 0; i < CreativeInventoryTabsCount; i++)
			{
				newArray[i] = CreativeInventoryTabs[i];
			}
			CreativeInventoryTabs = newArray;
		}
		CreativeInventoryTabs[CreativeInventoryTabsCount++] = value;
	}

	public void SetCreativeInventoryStacks(byte[] value)
	{
		CreativeInventoryStacks = value;
	}

	public int[] GetSideOpaqueFlags()
	{
		return SideOpaqueFlags;
	}

	public void SetSideOpaqueFlags(int[] value, int count, int length)
	{
		SideOpaqueFlags = value;
		SideOpaqueFlagsCount = count;
		SideOpaqueFlagsLength = length;
	}

	public void SetSideOpaqueFlags(int[] value)
	{
		SideOpaqueFlags = value;
		SideOpaqueFlagsCount = value.Length;
		SideOpaqueFlagsLength = value.Length;
	}

	public int GetSideOpaqueFlagsCount()
	{
		return SideOpaqueFlagsCount;
	}

	public void SideOpaqueFlagsAdd(int value)
	{
		if (SideOpaqueFlagsCount >= SideOpaqueFlagsLength)
		{
			if ((SideOpaqueFlagsLength *= 2) == 0)
			{
				SideOpaqueFlagsLength = 1;
			}
			int[] newArray = new int[SideOpaqueFlagsLength];
			for (int i = 0; i < SideOpaqueFlagsCount; i++)
			{
				newArray[i] = SideOpaqueFlags[i];
			}
			SideOpaqueFlags = newArray;
		}
		SideOpaqueFlags[SideOpaqueFlagsCount++] = value;
	}

	public void SetFaceCullMode(int value)
	{
		FaceCullMode = value;
	}

	public int[] GetSideSolidFlags()
	{
		return SideSolidFlags;
	}

	public void SetSideSolidFlags(int[] value, int count, int length)
	{
		SideSolidFlags = value;
		SideSolidFlagsCount = count;
		SideSolidFlagsLength = length;
	}

	public void SetSideSolidFlags(int[] value)
	{
		SideSolidFlags = value;
		SideSolidFlagsCount = value.Length;
		SideSolidFlagsLength = value.Length;
	}

	public int GetSideSolidFlagsCount()
	{
		return SideSolidFlagsCount;
	}

	public void SideSolidFlagsAdd(int value)
	{
		if (SideSolidFlagsCount >= SideSolidFlagsLength)
		{
			if ((SideSolidFlagsLength *= 2) == 0)
			{
				SideSolidFlagsLength = 1;
			}
			int[] newArray = new int[SideSolidFlagsLength];
			for (int i = 0; i < SideSolidFlagsCount; i++)
			{
				newArray[i] = SideSolidFlags[i];
			}
			SideSolidFlags = newArray;
		}
		SideSolidFlags[SideSolidFlagsCount++] = value;
	}

	public void SetSeasonColorMap(string value)
	{
		SeasonColorMap = value;
	}

	public void SetClimateColorMap(string value)
	{
		ClimateColorMap = value;
	}

	public void SetCullFaces(int value)
	{
		CullFaces = value;
	}

	public void SetReplacable(int value)
	{
		Replacable = value;
	}

	public void SetLightAbsorption(int value)
	{
		LightAbsorption = value;
	}

	public void SetHardnessLevel(int value)
	{
		HardnessLevel = value;
	}

	public void SetResistance(int value)
	{
		Resistance = value;
	}

	public void SetBlockMaterial(int value)
	{
		BlockMaterial = value;
	}

	public void SetModdata(byte[] value)
	{
		Moddata = value;
	}

	public void SetShape(Packet_CompositeShape value)
	{
		Shape = value;
	}

	public void SetShapeInventory(Packet_CompositeShape value)
	{
		ShapeInventory = value;
	}

	public void SetAmbientocclusion(int value)
	{
		Ambientocclusion = value;
	}

	public Packet_Cube[] GetCollisionBoxes()
	{
		return CollisionBoxes;
	}

	public void SetCollisionBoxes(Packet_Cube[] value, int count, int length)
	{
		CollisionBoxes = value;
		CollisionBoxesCount = count;
		CollisionBoxesLength = length;
	}

	public void SetCollisionBoxes(Packet_Cube[] value)
	{
		CollisionBoxes = value;
		CollisionBoxesCount = value.Length;
		CollisionBoxesLength = value.Length;
	}

	public int GetCollisionBoxesCount()
	{
		return CollisionBoxesCount;
	}

	public void CollisionBoxesAdd(Packet_Cube value)
	{
		if (CollisionBoxesCount >= CollisionBoxesLength)
		{
			if ((CollisionBoxesLength *= 2) == 0)
			{
				CollisionBoxesLength = 1;
			}
			Packet_Cube[] newArray = new Packet_Cube[CollisionBoxesLength];
			for (int i = 0; i < CollisionBoxesCount; i++)
			{
				newArray[i] = CollisionBoxes[i];
			}
			CollisionBoxes = newArray;
		}
		CollisionBoxes[CollisionBoxesCount++] = value;
	}

	public Packet_Cube[] GetSelectionBoxes()
	{
		return SelectionBoxes;
	}

	public void SetSelectionBoxes(Packet_Cube[] value, int count, int length)
	{
		SelectionBoxes = value;
		SelectionBoxesCount = count;
		SelectionBoxesLength = length;
	}

	public void SetSelectionBoxes(Packet_Cube[] value)
	{
		SelectionBoxes = value;
		SelectionBoxesCount = value.Length;
		SelectionBoxesLength = value.Length;
	}

	public int GetSelectionBoxesCount()
	{
		return SelectionBoxesCount;
	}

	public void SelectionBoxesAdd(Packet_Cube value)
	{
		if (SelectionBoxesCount >= SelectionBoxesLength)
		{
			if ((SelectionBoxesLength *= 2) == 0)
			{
				SelectionBoxesLength = 1;
			}
			Packet_Cube[] newArray = new Packet_Cube[SelectionBoxesLength];
			for (int i = 0; i < SelectionBoxesCount; i++)
			{
				newArray[i] = SelectionBoxes[i];
			}
			SelectionBoxes = newArray;
		}
		SelectionBoxes[SelectionBoxesCount++] = value;
	}

	public Packet_Cube[] GetParticleCollisionBoxes()
	{
		return ParticleCollisionBoxes;
	}

	public void SetParticleCollisionBoxes(Packet_Cube[] value, int count, int length)
	{
		ParticleCollisionBoxes = value;
		ParticleCollisionBoxesCount = count;
		ParticleCollisionBoxesLength = length;
	}

	public void SetParticleCollisionBoxes(Packet_Cube[] value)
	{
		ParticleCollisionBoxes = value;
		ParticleCollisionBoxesCount = value.Length;
		ParticleCollisionBoxesLength = value.Length;
	}

	public int GetParticleCollisionBoxesCount()
	{
		return ParticleCollisionBoxesCount;
	}

	public void ParticleCollisionBoxesAdd(Packet_Cube value)
	{
		if (ParticleCollisionBoxesCount >= ParticleCollisionBoxesLength)
		{
			if ((ParticleCollisionBoxesLength *= 2) == 0)
			{
				ParticleCollisionBoxesLength = 1;
			}
			Packet_Cube[] newArray = new Packet_Cube[ParticleCollisionBoxesLength];
			for (int i = 0; i < ParticleCollisionBoxesCount; i++)
			{
				newArray[i] = ParticleCollisionBoxes[i];
			}
			ParticleCollisionBoxes = newArray;
		}
		ParticleCollisionBoxes[ParticleCollisionBoxesCount++] = value;
	}

	public void SetBlockclass(string value)
	{
		Blockclass = value;
	}

	public void SetGuiTransform(Packet_ModelTransform value)
	{
		GuiTransform = value;
	}

	public void SetFpHandTransform(Packet_ModelTransform value)
	{
		FpHandTransform = value;
	}

	public void SetTpHandTransform(Packet_ModelTransform value)
	{
		TpHandTransform = value;
	}

	public void SetTpOffHandTransform(Packet_ModelTransform value)
	{
		TpOffHandTransform = value;
	}

	public void SetGroundTransform(Packet_ModelTransform value)
	{
		GroundTransform = value;
	}

	public void SetFertility(int value)
	{
		Fertility = value;
	}

	public void SetParticleProperties(byte[] value)
	{
		ParticleProperties = value;
	}

	public void SetParticlePropertiesQuantity(int value)
	{
		ParticlePropertiesQuantity = value;
	}

	public void SetRandomDrawOffset(int value)
	{
		RandomDrawOffset = value;
	}

	public void SetRandomizeAxes(int value)
	{
		RandomizeAxes = value;
	}

	public void SetRandomizeRotations(int value)
	{
		RandomizeRotations = value;
	}

	public Packet_BlockDrop[] GetDrops()
	{
		return Drops;
	}

	public void SetDrops(Packet_BlockDrop[] value, int count, int length)
	{
		Drops = value;
		DropsCount = count;
		DropsLength = length;
	}

	public void SetDrops(Packet_BlockDrop[] value)
	{
		Drops = value;
		DropsCount = value.Length;
		DropsLength = value.Length;
	}

	public int GetDropsCount()
	{
		return DropsCount;
	}

	public void DropsAdd(Packet_BlockDrop value)
	{
		if (DropsCount >= DropsLength)
		{
			if ((DropsLength *= 2) == 0)
			{
				DropsLength = 1;
			}
			Packet_BlockDrop[] newArray = new Packet_BlockDrop[DropsLength];
			for (int i = 0; i < DropsCount; i++)
			{
				newArray[i] = Drops[i];
			}
			Drops = newArray;
		}
		Drops[DropsCount++] = value;
	}

	public void SetLiquidLevel(int value)
	{
		LiquidLevel = value;
	}

	public void SetAttributes(string value)
	{
		Attributes = value;
	}

	public void SetCombustibleProps(Packet_CombustibleProperties value)
	{
		CombustibleProps = value;
	}

	public int[] GetSideAo()
	{
		return SideAo;
	}

	public void SetSideAo(int[] value, int count, int length)
	{
		SideAo = value;
		SideAoCount = count;
		SideAoLength = length;
	}

	public void SetSideAo(int[] value)
	{
		SideAo = value;
		SideAoCount = value.Length;
		SideAoLength = value.Length;
	}

	public int GetSideAoCount()
	{
		return SideAoCount;
	}

	public void SideAoAdd(int value)
	{
		if (SideAoCount >= SideAoLength)
		{
			if ((SideAoLength *= 2) == 0)
			{
				SideAoLength = 1;
			}
			int[] newArray = new int[SideAoLength];
			for (int i = 0; i < SideAoCount; i++)
			{
				newArray[i] = SideAo[i];
			}
			SideAo = newArray;
		}
		SideAo[SideAoCount++] = value;
	}

	public void SetNeighbourSideAo(int value)
	{
		NeighbourSideAo = value;
	}

	public void SetGrindingProps(Packet_GrindingProperties value)
	{
		GrindingProps = value;
	}

	public void SetNutritionProps(Packet_NutritionProperties value)
	{
		NutritionProps = value;
	}

	public Packet_TransitionableProperties[] GetTransitionableProps()
	{
		return TransitionableProps;
	}

	public void SetTransitionableProps(Packet_TransitionableProperties[] value, int count, int length)
	{
		TransitionableProps = value;
		TransitionablePropsCount = count;
		TransitionablePropsLength = length;
	}

	public void SetTransitionableProps(Packet_TransitionableProperties[] value)
	{
		TransitionableProps = value;
		TransitionablePropsCount = value.Length;
		TransitionablePropsLength = value.Length;
	}

	public int GetTransitionablePropsCount()
	{
		return TransitionablePropsCount;
	}

	public void TransitionablePropsAdd(Packet_TransitionableProperties value)
	{
		if (TransitionablePropsCount >= TransitionablePropsLength)
		{
			if ((TransitionablePropsLength *= 2) == 0)
			{
				TransitionablePropsLength = 1;
			}
			Packet_TransitionableProperties[] newArray = new Packet_TransitionableProperties[TransitionablePropsLength];
			for (int i = 0; i < TransitionablePropsCount; i++)
			{
				newArray[i] = TransitionableProps[i];
			}
			TransitionableProps = newArray;
		}
		TransitionableProps[TransitionablePropsCount++] = value;
	}

	public void SetMaxStackSize(int value)
	{
		MaxStackSize = value;
	}

	public void SetCropProps(byte[] value)
	{
		CropProps = value;
	}

	public string[] GetCropPropBehaviors()
	{
		return CropPropBehaviors;
	}

	public void SetCropPropBehaviors(string[] value, int count, int length)
	{
		CropPropBehaviors = value;
		CropPropBehaviorsCount = count;
		CropPropBehaviorsLength = length;
	}

	public void SetCropPropBehaviors(string[] value)
	{
		CropPropBehaviors = value;
		CropPropBehaviorsCount = value.Length;
		CropPropBehaviorsLength = value.Length;
	}

	public int GetCropPropBehaviorsCount()
	{
		return CropPropBehaviorsCount;
	}

	public void CropPropBehaviorsAdd(string value)
	{
		if (CropPropBehaviorsCount >= CropPropBehaviorsLength)
		{
			if ((CropPropBehaviorsLength *= 2) == 0)
			{
				CropPropBehaviorsLength = 1;
			}
			string[] newArray = new string[CropPropBehaviorsLength];
			for (int i = 0; i < CropPropBehaviorsCount; i++)
			{
				newArray[i] = CropPropBehaviors[i];
			}
			CropPropBehaviors = newArray;
		}
		CropPropBehaviors[CropPropBehaviorsCount++] = value;
	}

	public void SetMaterialDensity(int value)
	{
		MaterialDensity = value;
	}

	public void SetAttackPower(int value)
	{
		AttackPower = value;
	}

	public void SetAttackRange(int value)
	{
		AttackRange = value;
	}

	public void SetLiquidSelectable(int value)
	{
		LiquidSelectable = value;
	}

	public void SetMiningTier(int value)
	{
		MiningTier = value;
	}

	public void SetRequiredMiningTier(int value)
	{
		RequiredMiningTier = value;
	}

	public int[] GetMiningmaterial()
	{
		return Miningmaterial;
	}

	public void SetMiningmaterial(int[] value, int count, int length)
	{
		Miningmaterial = value;
		MiningmaterialCount = count;
		MiningmaterialLength = length;
	}

	public void SetMiningmaterial(int[] value)
	{
		Miningmaterial = value;
		MiningmaterialCount = value.Length;
		MiningmaterialLength = value.Length;
	}

	public int GetMiningmaterialCount()
	{
		return MiningmaterialCount;
	}

	public void MiningmaterialAdd(int value)
	{
		if (MiningmaterialCount >= MiningmaterialLength)
		{
			if ((MiningmaterialLength *= 2) == 0)
			{
				MiningmaterialLength = 1;
			}
			int[] newArray = new int[MiningmaterialLength];
			for (int i = 0; i < MiningmaterialCount; i++)
			{
				newArray[i] = Miningmaterial[i];
			}
			Miningmaterial = newArray;
		}
		Miningmaterial[MiningmaterialCount++] = value;
	}

	public int[] GetMiningmaterialspeed()
	{
		return Miningmaterialspeed;
	}

	public void SetMiningmaterialspeed(int[] value, int count, int length)
	{
		Miningmaterialspeed = value;
		MiningmaterialspeedCount = count;
		MiningmaterialspeedLength = length;
	}

	public void SetMiningmaterialspeed(int[] value)
	{
		Miningmaterialspeed = value;
		MiningmaterialspeedCount = value.Length;
		MiningmaterialspeedLength = value.Length;
	}

	public int GetMiningmaterialspeedCount()
	{
		return MiningmaterialspeedCount;
	}

	public void MiningmaterialspeedAdd(int value)
	{
		if (MiningmaterialspeedCount >= MiningmaterialspeedLength)
		{
			if ((MiningmaterialspeedLength *= 2) == 0)
			{
				MiningmaterialspeedLength = 1;
			}
			int[] newArray = new int[MiningmaterialspeedLength];
			for (int i = 0; i < MiningmaterialspeedCount; i++)
			{
				newArray[i] = Miningmaterialspeed[i];
			}
			Miningmaterialspeed = newArray;
		}
		Miningmaterialspeed[MiningmaterialspeedCount++] = value;
	}

	public void SetDragMultiplierFloat(int value)
	{
		DragMultiplierFloat = value;
	}

	public void SetStorageFlags(int value)
	{
		StorageFlags = value;
	}

	public void SetRenderAlphaTest(int value)
	{
		RenderAlphaTest = value;
	}

	public void SetHeldTpHitAnimation(string value)
	{
		HeldTpHitAnimation = value;
	}

	public void SetHeldRightTpIdleAnimation(string value)
	{
		HeldRightTpIdleAnimation = value;
	}

	public void SetHeldLeftTpIdleAnimation(string value)
	{
		HeldLeftTpIdleAnimation = value;
	}

	public void SetHeldTpUseAnimation(string value)
	{
		HeldTpUseAnimation = value;
	}

	public void SetRainPermeable(int value)
	{
		RainPermeable = value;
	}

	public void SetLiquidCode(string value)
	{
		LiquidCode = value;
	}

	public Packet_VariantPart[] GetVariant()
	{
		return Variant;
	}

	public void SetVariant(Packet_VariantPart[] value, int count, int length)
	{
		Variant = value;
		VariantCount = count;
		VariantLength = length;
	}

	public void SetVariant(Packet_VariantPart[] value)
	{
		Variant = value;
		VariantCount = value.Length;
		VariantLength = value.Length;
	}

	public int GetVariantCount()
	{
		return VariantCount;
	}

	public void VariantAdd(Packet_VariantPart value)
	{
		if (VariantCount >= VariantLength)
		{
			if ((VariantLength *= 2) == 0)
			{
				VariantLength = 1;
			}
			Packet_VariantPart[] newArray = new Packet_VariantPart[VariantLength];
			for (int i = 0; i < VariantCount; i++)
			{
				newArray[i] = Variant[i];
			}
			Variant = newArray;
		}
		Variant[VariantCount++] = value;
	}

	public void SetLod0shape(Packet_CompositeShape value)
	{
		Lod0shape = value;
	}

	public void SetFrostable(int value)
	{
		Frostable = value;
	}

	public void SetCrushingProps(Packet_CrushingProperties value)
	{
		CrushingProps = value;
	}

	public void SetRandomSizeAdjust(int value)
	{
		RandomSizeAdjust = value;
	}

	public void SetLod2shape(Packet_CompositeShape value)
	{
		Lod2shape = value;
	}

	public void SetDoNotRenderAtLod2(int value)
	{
		DoNotRenderAtLod2 = value;
	}

	public void SetWidth(int value)
	{
		Width = value;
	}

	public void SetHeight(int value)
	{
		Height = value;
	}

	public void SetLength(int value)
	{
		Length = value;
	}

	public void SetIsMissing(int value)
	{
		IsMissing = value;
	}

	public void SetDurability(int value)
	{
		Durability = value;
	}

	public void SetHeldLeftReadyAnimation(string value)
	{
		HeldLeftReadyAnimation = value;
	}

	public void SetHeldRightReadyAnimation(string value)
	{
		HeldRightReadyAnimation = value;
	}

	internal void InitializeValues()
	{
		MatterState = 0;
	}
}
