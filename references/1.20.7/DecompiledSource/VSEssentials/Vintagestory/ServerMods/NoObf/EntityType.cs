using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods.NoObf;

[DocumentAsJson]
[JsonObject(MemberSerialization.OptIn)]
public class EntityType : RegistryObjectType
{
	[JsonProperty]
	public EnumHabitat Habitat = EnumHabitat.Land;

	[JsonProperty]
	public Vec2f CollisionBoxSize = new Vec2f(0.5f, 0.5f);

	[JsonProperty]
	public Vec2f DeadCollisionBoxSize = new Vec2f(0.5f, 0.25f);

	[JsonProperty]
	public Vec2f SelectionBoxSize;

	[JsonProperty]
	public Vec2f DeadSelectionBoxSize;

	[JsonProperty]
	public double EyeHeight = 0.1;

	[JsonProperty]
	public double? SwimmingEyeHeight;

	[JsonProperty]
	public float Weight = 25f;

	[JsonProperty]
	public bool CanClimb;

	[JsonProperty]
	public bool CanClimbAnywhere;

	[Obsolete("This will be removed in 1.20. Instead set FallDamageMultiplier to 0.0 for no fall damage")]
	[JsonProperty]
	public bool FallDamage = true;

	[JsonProperty]
	public float FallDamageMultiplier = 1f;

	[JsonProperty]
	public float ClimbTouchDistance = 0.5f;

	[JsonProperty]
	public bool RotateModelOnClimb;

	[JsonProperty]
	public float KnockbackResistance;

	[JsonProperty]
	[JsonConverter(typeof(JsonAttributesConverter))]
	public JsonObject Attributes;

	[JsonProperty(ItemConverterType = typeof(JsonAttributesConverter))]
	public Dictionary<string, JsonObject> BehaviorConfigs;

	[JsonProperty]
	public ClientEntityConfig Client;

	[JsonProperty]
	public ServerEntityConfig Server;

	[JsonProperty]
	public Dictionary<string, AssetLocation> Sounds;

	[JsonProperty]
	public float IdleSoundChance = 0.05f;

	[JsonProperty]
	public float IdleSoundRange = 24f;

	[JsonProperty]
	public BlockDropItemStack[] Drops;

	[JsonProperty]
	public Vec2f HitBoxSize
	{
		get
		{
			return null;
		}
		set
		{
			CollisionBoxSize = value;
			SelectionBoxSize = value;
		}
	}

	[JsonProperty]
	public Vec2f DeadHitBoxSize
	{
		get
		{
			return null;
		}
		set
		{
			DeadCollisionBoxSize = value;
			DeadSelectionBoxSize = value;
		}
	}

	public EntityProperties CreateProperties()
	{
		BlockDropItemStack[] DropsCopy;
		if (Drops == null)
		{
			DropsCopy = null;
		}
		else
		{
			DropsCopy = new BlockDropItemStack[Drops.Length];
			for (int i = 0; i < DropsCopy.Length; i++)
			{
				DropsCopy[i] = Drops[i].Clone();
			}
		}
		EntityProperties properties = new EntityProperties
		{
			Code = Code,
			Variant = new OrderedDictionary<string, string>(Variant),
			Class = Class,
			Habitat = Habitat,
			CollisionBoxSize = CollisionBoxSize,
			DeadCollisionBoxSize = DeadCollisionBoxSize,
			SelectionBoxSize = SelectionBoxSize,
			DeadSelectionBoxSize = DeadSelectionBoxSize,
			Weight = Weight,
			CanClimb = CanClimb,
			CanClimbAnywhere = CanClimbAnywhere,
			FallDamage = (FallDamage && FallDamageMultiplier > 0f),
			FallDamageMultiplier = FallDamageMultiplier,
			ClimbTouchDistance = ClimbTouchDistance,
			RotateModelOnClimb = RotateModelOnClimb,
			KnockbackResistance = KnockbackResistance,
			Attributes = Attributes,
			Sounds = ((Sounds == null) ? new Dictionary<string, AssetLocation>() : new Dictionary<string, AssetLocation>(Sounds)),
			IdleSoundChance = IdleSoundChance,
			IdleSoundRange = IdleSoundRange,
			Drops = DropsCopy,
			EyeHeight = EyeHeight,
			SwimmingEyeHeight = (SwimmingEyeHeight ?? EyeHeight)
		};
		if (Client != null)
		{
			properties.Client = new EntityClientProperties(Client.Behaviors, BehaviorConfigs)
			{
				RendererName = Client.Renderer,
				Textures = new FastSmallDictionary<string, CompositeTexture>(Client.Textures),
				GlowLevel = Client.GlowLevel,
				PitchStep = Client.PitchStep,
				Shape = Client.Shape,
				Size = Client.Size,
				SizeGrowthFactor = Client.SizeGrowthFactor,
				Animations = Client.Animations,
				AnimationsByMetaCode = Client.AnimationsByMetaCode
			};
		}
		if (Server != null)
		{
			properties.Server = new EntityServerProperties(Server.Behaviors, BehaviorConfigs)
			{
				Attributes = (Server.Attributes?.ToAttribute() as TreeAttribute),
				SpawnConditions = Server.SpawnConditions
			};
		}
		return properties;
	}

	internal override RegistryObjectType CreateAndPopulate(ICoreServerAPI api, AssetLocation fullcode, JObject jobject, JsonSerializer deserializer, OrderedDictionary<string, string> variant)
	{
		return CreateResolvedType<EntityType>(api, fullcode, jobject, deserializer, variant);
	}
}
