public class Packet_EntityType
{
	public string Code;

	public string Class;

	public string Renderer;

	public int Habitat;

	public byte[] Drops;

	public Packet_CompositeShape Shape;

	public Packet_Behavior[] Behaviors;

	public int BehaviorsCount;

	public int BehaviorsLength;

	public int CollisionBoxLength;

	public int CollisionBoxHeight;

	public int DeadCollisionBoxLength;

	public int DeadCollisionBoxHeight;

	public int SelectionBoxLength;

	public int SelectionBoxHeight;

	public int DeadSelectionBoxLength;

	public int DeadSelectionBoxHeight;

	public string Attributes;

	public string[] SoundKeys;

	public int SoundKeysCount;

	public int SoundKeysLength;

	public string[] SoundNames;

	public int SoundNamesCount;

	public int SoundNamesLength;

	public int IdleSoundChance;

	public int IdleSoundRange;

	public string[] TextureCodes;

	public int TextureCodesCount;

	public int TextureCodesLength;

	public Packet_CompositeTexture[] CompositeTextures;

	public int CompositeTexturesCount;

	public int CompositeTexturesLength;

	public int Size;

	public int EyeHeight;

	public int SwimmingEyeHeight;

	public int Weight;

	public int CanClimb;

	public byte[] AnimationMetaData;

	public int KnockbackResistance;

	public int GlowLevel;

	public int CanClimbAnywhere;

	public int ClimbTouchDistance;

	public int RotateModelOnClimb;

	public int FallDamage;

	public int FallDamageMultiplier;

	public Packet_VariantPart[] Variant;

	public int VariantCount;

	public int VariantLength;

	public int SizeGrowthFactor;

	public int PitchStep;

	public string Color;

	public const int CodeFieldID = 1;

	public const int ClassFieldID = 2;

	public const int RendererFieldID = 3;

	public const int HabitatFieldID = 4;

	public const int DropsFieldID = 25;

	public const int ShapeFieldID = 11;

	public const int BehaviorsFieldID = 5;

	public const int CollisionBoxLengthFieldID = 6;

	public const int CollisionBoxHeightFieldID = 7;

	public const int DeadCollisionBoxLengthFieldID = 26;

	public const int DeadCollisionBoxHeightFieldID = 27;

	public const int SelectionBoxLengthFieldID = 32;

	public const int SelectionBoxHeightFieldID = 33;

	public const int DeadSelectionBoxLengthFieldID = 34;

	public const int DeadSelectionBoxHeightFieldID = 35;

	public const int AttributesFieldID = 8;

	public const int SoundKeysFieldID = 9;

	public const int SoundNamesFieldID = 10;

	public const int IdleSoundChanceFieldID = 14;

	public const int IdleSoundRangeFieldID = 37;

	public const int TextureCodesFieldID = 12;

	public const int CompositeTexturesFieldID = 13;

	public const int SizeFieldID = 15;

	public const int EyeHeightFieldID = 16;

	public const int SwimmingEyeHeightFieldID = 36;

	public const int WeightFieldID = 29;

	public const int CanClimbFieldID = 17;

	public const int AnimationMetaDataFieldID = 18;

	public const int KnockbackResistanceFieldID = 19;

	public const int GlowLevelFieldID = 20;

	public const int CanClimbAnywhereFieldID = 21;

	public const int ClimbTouchDistanceFieldID = 22;

	public const int RotateModelOnClimbFieldID = 23;

	public const int FallDamageFieldID = 24;

	public const int FallDamageMultiplierFieldID = 39;

	public const int VariantFieldID = 28;

	public const int SizeGrowthFactorFieldID = 30;

	public const int PitchStepFieldID = 31;

	public const int ColorFieldID = 38;

	public void SetCode(string value)
	{
		Code = value;
	}

	public void SetClass(string value)
	{
		Class = value;
	}

	public void SetRenderer(string value)
	{
		Renderer = value;
	}

	public void SetHabitat(int value)
	{
		Habitat = value;
	}

	public void SetDrops(byte[] value)
	{
		Drops = value;
	}

	public void SetShape(Packet_CompositeShape value)
	{
		Shape = value;
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

	public void SetCollisionBoxLength(int value)
	{
		CollisionBoxLength = value;
	}

	public void SetCollisionBoxHeight(int value)
	{
		CollisionBoxHeight = value;
	}

	public void SetDeadCollisionBoxLength(int value)
	{
		DeadCollisionBoxLength = value;
	}

	public void SetDeadCollisionBoxHeight(int value)
	{
		DeadCollisionBoxHeight = value;
	}

	public void SetSelectionBoxLength(int value)
	{
		SelectionBoxLength = value;
	}

	public void SetSelectionBoxHeight(int value)
	{
		SelectionBoxHeight = value;
	}

	public void SetDeadSelectionBoxLength(int value)
	{
		DeadSelectionBoxLength = value;
	}

	public void SetDeadSelectionBoxHeight(int value)
	{
		DeadSelectionBoxHeight = value;
	}

	public void SetAttributes(string value)
	{
		Attributes = value;
	}

	public string[] GetSoundKeys()
	{
		return SoundKeys;
	}

	public void SetSoundKeys(string[] value, int count, int length)
	{
		SoundKeys = value;
		SoundKeysCount = count;
		SoundKeysLength = length;
	}

	public void SetSoundKeys(string[] value)
	{
		SoundKeys = value;
		SoundKeysCount = value.Length;
		SoundKeysLength = value.Length;
	}

	public int GetSoundKeysCount()
	{
		return SoundKeysCount;
	}

	public void SoundKeysAdd(string value)
	{
		if (SoundKeysCount >= SoundKeysLength)
		{
			if ((SoundKeysLength *= 2) == 0)
			{
				SoundKeysLength = 1;
			}
			string[] newArray = new string[SoundKeysLength];
			for (int i = 0; i < SoundKeysCount; i++)
			{
				newArray[i] = SoundKeys[i];
			}
			SoundKeys = newArray;
		}
		SoundKeys[SoundKeysCount++] = value;
	}

	public string[] GetSoundNames()
	{
		return SoundNames;
	}

	public void SetSoundNames(string[] value, int count, int length)
	{
		SoundNames = value;
		SoundNamesCount = count;
		SoundNamesLength = length;
	}

	public void SetSoundNames(string[] value)
	{
		SoundNames = value;
		SoundNamesCount = value.Length;
		SoundNamesLength = value.Length;
	}

	public int GetSoundNamesCount()
	{
		return SoundNamesCount;
	}

	public void SoundNamesAdd(string value)
	{
		if (SoundNamesCount >= SoundNamesLength)
		{
			if ((SoundNamesLength *= 2) == 0)
			{
				SoundNamesLength = 1;
			}
			string[] newArray = new string[SoundNamesLength];
			for (int i = 0; i < SoundNamesCount; i++)
			{
				newArray[i] = SoundNames[i];
			}
			SoundNames = newArray;
		}
		SoundNames[SoundNamesCount++] = value;
	}

	public void SetIdleSoundChance(int value)
	{
		IdleSoundChance = value;
	}

	public void SetIdleSoundRange(int value)
	{
		IdleSoundRange = value;
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

	public void SetSize(int value)
	{
		Size = value;
	}

	public void SetEyeHeight(int value)
	{
		EyeHeight = value;
	}

	public void SetSwimmingEyeHeight(int value)
	{
		SwimmingEyeHeight = value;
	}

	public void SetWeight(int value)
	{
		Weight = value;
	}

	public void SetCanClimb(int value)
	{
		CanClimb = value;
	}

	public void SetAnimationMetaData(byte[] value)
	{
		AnimationMetaData = value;
	}

	public void SetKnockbackResistance(int value)
	{
		KnockbackResistance = value;
	}

	public void SetGlowLevel(int value)
	{
		GlowLevel = value;
	}

	public void SetCanClimbAnywhere(int value)
	{
		CanClimbAnywhere = value;
	}

	public void SetClimbTouchDistance(int value)
	{
		ClimbTouchDistance = value;
	}

	public void SetRotateModelOnClimb(int value)
	{
		RotateModelOnClimb = value;
	}

	public void SetFallDamage(int value)
	{
		FallDamage = value;
	}

	public void SetFallDamageMultiplier(int value)
	{
		FallDamageMultiplier = value;
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

	public void SetSizeGrowthFactor(int value)
	{
		SizeGrowthFactor = value;
	}

	public void SetPitchStep(int value)
	{
		PitchStep = value;
	}

	public void SetColor(string value)
	{
		Color = value;
	}

	internal void InitializeValues()
	{
	}
}
