using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods.NoObf;

[DocumentAsJson]
[JsonObject(MemberSerialization.OptIn)]
public abstract class CollectibleType : RegistryObjectType
{
	[JsonProperty]
	public CollectibleBehaviorType[] Behaviors = new CollectibleBehaviorType[0];

	[JsonProperty]
	public byte[] LightHsv = new byte[3];

	[JsonProperty]
	public float RenderAlphaTest = 0.05f;

	[JsonProperty]
	public int StorageFlags = 1;

	[JsonProperty]
	public int MaxStackSize = 1;

	[JsonProperty]
	public float AttackPower = 0.5f;

	[JsonProperty]
	public int Durability;

	[JsonProperty]
	public Size3f Size;

	[JsonProperty]
	public EnumItemDamageSource[] DamagedBy;

	[JsonProperty]
	public EnumTool? Tool;

	[JsonProperty]
	public float AttackRange = GlobalConstants.DefaultAttackRange;

	[JsonProperty]
	public Dictionary<EnumBlockMaterial, float> MiningSpeed;

	[JsonProperty]
	public int ToolTier;

	[JsonProperty]
	public EnumMatterState MatterState = EnumMatterState.Solid;

	[JsonProperty]
	public HeldSounds HeldSounds;

	[JsonProperty]
	public int MaterialDensity = 9999;

	[JsonProperty]
	[JsonConverter(typeof(JsonAttributesConverter))]
	public JsonObject Attributes;

	[JsonProperty]
	public CompositeShape Shape;

	[JsonProperty]
	public ModelTransform GuiTransform;

	[JsonProperty]
	public ModelTransform FpHandTransform;

	[JsonProperty]
	public ModelTransform TpHandTransform;

	[JsonProperty]
	public ModelTransform TpOffHandTransform;

	[JsonProperty]
	public ModelTransform GroundTransform;

	[JsonProperty]
	public CompositeTexture Texture;

	[JsonProperty]
	public Dictionary<string, CompositeTexture> Textures;

	[JsonProperty]
	public CombustibleProperties CombustibleProps;

	[JsonProperty]
	public FoodNutritionProperties NutritionProps;

	[JsonProperty]
	public TransitionableProperties[] TransitionableProps;

	[JsonProperty]
	public GrindingProperties GrindingProps;

	[JsonProperty]
	public CrushingProperties CrushingProps;

	[JsonProperty]
	public bool LiquidSelectable;

	[JsonProperty]
	public Dictionary<string, string[]> CreativeInventory = new Dictionary<string, string[]>();

	[JsonProperty]
	public CreativeTabAndStackList[] CreativeInventoryStacks;

	[JsonProperty]
	public string HeldTpHitAnimation = "breakhand";

	[JsonProperty]
	public string HeldRightTpIdleAnimation;

	[JsonProperty]
	public string HeldLeftTpIdleAnimation;

	[JsonProperty]
	public string HeldLeftReadyAnimation = "helditemready";

	[JsonProperty]
	public string HeldRightReadyAnimation = "helditemready";

	[JsonProperty("heldTpIdleAnimation")]
	private string HeldOldTpIdleAnimation;

	[JsonProperty]
	public string HeldTpUseAnimation = "interactstatic";

	[JsonProperty]
	public AdvancedParticleProperties[] ParticleProperties;

	[JsonProperty]
	[Obsolete("Use Size instead from game version 1.20.4 onwards, with the same values")]
	public Size3f Dimensions
	{
		get
		{
			return Size;
		}
		set
		{
			Size = value;
		}
	}

	[JsonProperty]
	[Obsolete("Use tool tier")]
	public int MiningTier
	{
		get
		{
			return ToolTier;
		}
		set
		{
			ToolTier = value;
		}
	}

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		OnDeserialized();
	}

	internal virtual void OnDeserialized()
	{
		if (Texture != null)
		{
			if (Textures == null)
			{
				Textures = new Dictionary<string, CompositeTexture>(1);
			}
			Textures["all"] = Texture;
		}
		if (HeldOldTpIdleAnimation != null && HeldRightTpIdleAnimation == null)
		{
			HeldRightTpIdleAnimation = HeldOldTpIdleAnimation;
		}
	}
}
