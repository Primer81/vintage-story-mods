public class Packet_ItemType
{
	public int ItemId;

	public int MaxStackSize;

	public string Code;

	public Packet_Behavior[] Behaviors;

	public int BehaviorsCount;

	public int BehaviorsLength;

	public Packet_CompositeTexture[] CompositeTextures;

	public int CompositeTexturesCount;

	public int CompositeTexturesLength;

	public int Durability;

	public int[] Miningmaterial;

	public int MiningmaterialCount;

	public int MiningmaterialLength;

	public int[] Miningmaterialspeed;

	public int MiningmaterialspeedCount;

	public int MiningmaterialspeedLength;

	public int[] Damagedby;

	public int DamagedbyCount;

	public int DamagedbyLength;

	public byte[] CreativeInventoryStacks;

	public string[] CreativeInventoryTabs;

	public int CreativeInventoryTabsCount;

	public int CreativeInventoryTabsLength;

	public Packet_ModelTransform GuiTransform;

	public Packet_ModelTransform FpHandTransform;

	public Packet_ModelTransform TpHandTransform;

	public Packet_ModelTransform TpOffHandTransform;

	public Packet_ModelTransform GroundTransform;

	public string Attributes;

	public Packet_CombustibleProperties CombustibleProps;

	public Packet_NutritionProperties NutritionProps;

	public Packet_GrindingProperties GrindingProps;

	public Packet_CrushingProperties CrushingProps;

	public Packet_TransitionableProperties[] TransitionableProps;

	public int TransitionablePropsCount;

	public int TransitionablePropsLength;

	public Packet_CompositeShape Shape;

	public string[] TextureCodes;

	public int TextureCodesCount;

	public int TextureCodesLength;

	public string ItemClass;

	public int Tool;

	public int MaterialDensity;

	public int AttackPower;

	public int AttackRange;

	public int LiquidSelectable;

	public int MiningTier;

	public int StorageFlags;

	public int RenderAlphaTest;

	public string HeldTpHitAnimation;

	public string HeldRightTpIdleAnimation;

	public string HeldLeftTpIdleAnimation;

	public string HeldTpUseAnimation;

	public int MatterState;

	public Packet_VariantPart[] Variant;

	public int VariantCount;

	public int VariantLength;

	public Packet_HeldSoundSet HeldSounds;

	public int Width;

	public int Height;

	public int Length;

	public int[] LightHsv;

	public int LightHsvCount;

	public int LightHsvLength;

	public int IsMissing;

	public string HeldLeftReadyAnimation;

	public string HeldRightReadyAnimation;

	public const int ItemIdFieldID = 1;

	public const int MaxStackSizeFieldID = 2;

	public const int CodeFieldID = 3;

	public const int BehaviorsFieldID = 39;

	public const int CompositeTexturesFieldID = 4;

	public const int DurabilityFieldID = 5;

	public const int MiningmaterialFieldID = 6;

	public const int MiningmaterialspeedFieldID = 31;

	public const int DamagedbyFieldID = 7;

	public const int CreativeInventoryStacksFieldID = 8;

	public const int CreativeInventoryTabsFieldID = 9;

	public const int GuiTransformFieldID = 10;

	public const int FpHandTransformFieldID = 11;

	public const int TpHandTransformFieldID = 12;

	public const int TpOffHandTransformFieldID = 43;

	public const int GroundTransformFieldID = 22;

	public const int AttributesFieldID = 13;

	public const int CombustiblePropsFieldID = 14;

	public const int NutritionPropsFieldID = 15;

	public const int GrindingPropsFieldID = 32;

	public const int CrushingPropsFieldID = 38;

	public const int TransitionablePropsFieldID = 36;

	public const int ShapeFieldID = 16;

	public const int TextureCodesFieldID = 17;

	public const int ItemClassFieldID = 18;

	public const int ToolFieldID = 19;

	public const int MaterialDensityFieldID = 20;

	public const int AttackPowerFieldID = 21;

	public const int AttackRangeFieldID = 25;

	public const int LiquidSelectableFieldID = 23;

	public const int MiningTierFieldID = 24;

	public const int StorageFlagsFieldID = 26;

	public const int RenderAlphaTestFieldID = 27;

	public const int HeldTpHitAnimationFieldID = 28;

	public const int HeldRightTpIdleAnimationFieldID = 29;

	public const int HeldLeftTpIdleAnimationFieldID = 34;

	public const int HeldTpUseAnimationFieldID = 30;

	public const int MatterStateFieldID = 33;

	public const int VariantFieldID = 35;

	public const int HeldSoundsFieldID = 37;

	public const int WidthFieldID = 40;

	public const int HeightFieldID = 41;

	public const int LengthFieldID = 42;

	public const int LightHsvFieldID = 44;

	public const int IsMissingFieldID = 45;

	public const int HeldLeftReadyAnimationFieldID = 46;

	public const int HeldRightReadyAnimationFieldID = 47;

	public void SetItemId(int value)
	{
		ItemId = value;
	}

	public void SetMaxStackSize(int value)
	{
		MaxStackSize = value;
	}

	public void SetCode(string value)
	{
		Code = value;
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

	public void SetDurability(int value)
	{
		Durability = value;
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

	public int[] GetDamagedby()
	{
		return Damagedby;
	}

	public void SetDamagedby(int[] value, int count, int length)
	{
		Damagedby = value;
		DamagedbyCount = count;
		DamagedbyLength = length;
	}

	public void SetDamagedby(int[] value)
	{
		Damagedby = value;
		DamagedbyCount = value.Length;
		DamagedbyLength = value.Length;
	}

	public int GetDamagedbyCount()
	{
		return DamagedbyCount;
	}

	public void DamagedbyAdd(int value)
	{
		if (DamagedbyCount >= DamagedbyLength)
		{
			if ((DamagedbyLength *= 2) == 0)
			{
				DamagedbyLength = 1;
			}
			int[] newArray = new int[DamagedbyLength];
			for (int i = 0; i < DamagedbyCount; i++)
			{
				newArray[i] = Damagedby[i];
			}
			Damagedby = newArray;
		}
		Damagedby[DamagedbyCount++] = value;
	}

	public void SetCreativeInventoryStacks(byte[] value)
	{
		CreativeInventoryStacks = value;
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

	public void SetAttributes(string value)
	{
		Attributes = value;
	}

	public void SetCombustibleProps(Packet_CombustibleProperties value)
	{
		CombustibleProps = value;
	}

	public void SetNutritionProps(Packet_NutritionProperties value)
	{
		NutritionProps = value;
	}

	public void SetGrindingProps(Packet_GrindingProperties value)
	{
		GrindingProps = value;
	}

	public void SetCrushingProps(Packet_CrushingProperties value)
	{
		CrushingProps = value;
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

	public void SetShape(Packet_CompositeShape value)
	{
		Shape = value;
	}

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

	public void SetItemClass(string value)
	{
		ItemClass = value;
	}

	public void SetTool(int value)
	{
		Tool = value;
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

	public void SetMatterState(int value)
	{
		MatterState = value;
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

	public void SetHeldSounds(Packet_HeldSoundSet value)
	{
		HeldSounds = value;
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

	public void SetIsMissing(int value)
	{
		IsMissing = value;
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
