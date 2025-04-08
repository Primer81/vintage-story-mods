using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common.Entities;

public class EntityProperties
{
	/// <summary>
	/// Assigned on registering the entity type
	/// </summary>
	public int Id;

	public string Color;

	/// <summary>
	/// The entity code in the code.
	/// </summary>
	public AssetLocation Code;

	/// <summary>
	/// Variant values as resolved from blocktype/itemtype or entitytype
	/// </summary>
	public OrderedDictionary<string, string> Variant = new OrderedDictionary<string, string>();

	/// <summary>
	/// The classification of the entity.
	/// </summary>
	public string Class;

	/// <summary>
	/// Natural habitat of the entity. Decides whether to apply gravity or not
	/// </summary>
	public EnumHabitat Habitat = EnumHabitat.Land;

	/// <summary>
	/// The size of the entity's hitbox (default: 0.2f/0.2f)
	/// </summary>
	public Vec2f CollisionBoxSize = new Vec2f(0.2f, 0.2f);

	/// <summary>
	/// The size of the hitbox while the entity is dead.
	/// </summary>
	public Vec2f DeadCollisionBoxSize = new Vec2f(0.3f, 0.3f);

	/// <summary>
	/// The size of the entity's hitbox (default: null, i.e. same as collision box)
	/// </summary>
	public Vec2f SelectionBoxSize;

	/// <summary>
	/// The size of the hitbox while the entity is dead.  (default: null, i.e. same as dead collision box)
	/// </summary>
	public Vec2f DeadSelectionBoxSize;

	/// <summary>
	/// How high the camera should be placed if this entity were to be controlled by the player
	/// </summary>
	public double EyeHeight;

	public double SwimmingEyeHeight;

	/// <summary>
	/// The mass of this type of entity in kilograms, on average - defaults to 25kg (medium-low) if not set by the asset
	/// </summary>
	public float Weight = 25f;

	/// <summary>
	/// If true the entity can climb on walls
	/// </summary>
	public bool CanClimb;

	/// <summary>
	/// If true the entity can climb anywhere.
	/// </summary>
	public bool CanClimbAnywhere;

	/// <summary>
	/// Whether the entity should take fall damage
	/// </summary>
	public bool FallDamage = true;

	/// <summary>
	/// If less than one, mitigates fall damage (e.g. could be used for mountainous creatures); if more than one, increases fall damage (e.g fragile creatures?)
	/// </summary>
	public float FallDamageMultiplier = 1f;

	public float ClimbTouchDistance;

	/// <summary>
	/// Should the model in question rotate if climbing?
	/// </summary>
	public bool RotateModelOnClimb;

	/// <summary>
	/// The resistance to being pushed back by an impact.
	/// </summary>
	public float KnockbackResistance;

	/// <summary>
	/// The attributes of the entity.  These are the Attributes read from the entity type's JSON file.
	/// <br />If your code modifies these Attributes (not recommended!), the changes will apply to all entities of the same type.
	/// </summary>
	public JsonObject Attributes;

	/// <summary>
	/// The client properties of the entity.
	/// </summary>
	public EntityClientProperties Client;

	/// <summary>
	/// The server properties of the entity.
	/// </summary>
	public EntityServerProperties Server;

	/// <summary>
	/// The sounds that this entity can make.
	/// </summary>
	public Dictionary<string, AssetLocation> Sounds;

	/// <summary>
	/// The sounds this entity can make after being resolved.
	/// </summary>
	public Dictionary<string, AssetLocation[]> ResolvedSounds = new Dictionary<string, AssetLocation[]>();

	/// <summary>
	/// The chance that an idle sound will play for the entity.
	/// </summary>
	public float IdleSoundChance = 0.3f;

	/// <summary>
	/// The sound range for the idle sound in blocks.
	/// </summary>
	public float IdleSoundRange = 24f;

	/// <summary>
	/// The drops for the entity when they are killed.
	/// </summary>
	public BlockDropItemStack[] Drops;

	public byte[] DropsPacket;

	/// <summary>
	/// The collision box they have.
	/// </summary>
	public Cuboidf SpawnCollisionBox => new Cuboidf
	{
		X1 = (0f - CollisionBoxSize.X) / 2f,
		Z1 = (0f - CollisionBoxSize.X) / 2f,
		X2 = CollisionBoxSize.X / 2f,
		Z2 = CollisionBoxSize.X / 2f,
		Y2 = CollisionBoxSize.Y
	};

	/// <summary>
	/// Creates a copy of this object.
	/// </summary>
	/// <returns></returns>
	public EntityProperties Clone()
	{
		BlockDropItemStack[] DropsCopy;
		if (Drops == null)
		{
			DropsCopy = null;
		}
		else
		{
			DropsCopy = new BlockDropItemStack[Drops.Length];
			for (int j = 0; j < DropsCopy.Length; j++)
			{
				DropsCopy[j] = Drops[j].Clone();
			}
		}
		Dictionary<string, AssetLocation> csounds = new Dictionary<string, AssetLocation>();
		foreach (KeyValuePair<string, AssetLocation> val2 in Sounds)
		{
			csounds[val2.Key] = val2.Value.Clone();
		}
		Dictionary<string, AssetLocation[]> cresolvedsounds = new Dictionary<string, AssetLocation[]>();
		foreach (KeyValuePair<string, AssetLocation[]> val in ResolvedSounds)
		{
			AssetLocation[] locs = val.Value;
			cresolvedsounds[val.Key] = new AssetLocation[locs.Length];
			for (int i = 0; i < locs.Length; i++)
			{
				cresolvedsounds[val.Key][i] = locs[i].Clone();
			}
		}
		if (!(Attributes is JsonObject_ReadOnly) && Attributes != null)
		{
			Attributes = new JsonObject_ReadOnly(Attributes);
		}
		return new EntityProperties
		{
			Code = Code.Clone(),
			Class = Class,
			Color = Color,
			Habitat = Habitat,
			CollisionBoxSize = CollisionBoxSize.Clone(),
			DeadCollisionBoxSize = DeadCollisionBoxSize.Clone(),
			SelectionBoxSize = SelectionBoxSize?.Clone(),
			DeadSelectionBoxSize = DeadSelectionBoxSize?.Clone(),
			CanClimb = CanClimb,
			Weight = Weight,
			CanClimbAnywhere = CanClimbAnywhere,
			FallDamage = FallDamage,
			FallDamageMultiplier = FallDamageMultiplier,
			ClimbTouchDistance = ClimbTouchDistance,
			RotateModelOnClimb = RotateModelOnClimb,
			KnockbackResistance = KnockbackResistance,
			Attributes = Attributes,
			Sounds = new Dictionary<string, AssetLocation>(Sounds),
			IdleSoundChance = IdleSoundChance,
			IdleSoundRange = IdleSoundRange,
			Drops = DropsCopy,
			EyeHeight = EyeHeight,
			SwimmingEyeHeight = SwimmingEyeHeight,
			Client = (Client?.Clone() as EntityClientProperties),
			Server = (Server?.Clone() as EntityServerProperties),
			Variant = new OrderedDictionary<string, string>(Variant)
		};
	}

	/// <summary>
	/// Initalizes the properties for the entity.
	/// </summary>
	/// <param name="entity">the entity to tie this to.</param>
	/// <param name="api">The Core API</param>
	public void Initialize(Entity entity, ICoreAPI api)
	{
		if (api.Side.IsClient())
		{
			if (Client == null)
			{
				return;
			}
			Client.loadBehaviors(entity, this, api.World);
		}
		else if (Server != null)
		{
			Server.loadBehaviors(entity, this, api.World);
		}
		Client?.Init(Code, api.World);
		InitSounds(api.Assets);
	}

	/// <summary>
	/// Initializes the sounds for this entity type.
	/// </summary>
	/// <param name="assetManager"></param>
	public void InitSounds(IAssetManager assetManager)
	{
		if (Sounds == null)
		{
			return;
		}
		foreach (KeyValuePair<string, AssetLocation> val in Sounds)
		{
			if (val.Value.Path.EndsWith('*'))
			{
				List<IAsset> manyInCategory = assetManager.GetManyInCategory("sounds", val.Value.Path.Substring(0, val.Value.Path.Length - 1), val.Value.Domain);
				AssetLocation[] sounds = new AssetLocation[manyInCategory.Count];
				int i = 0;
				foreach (IAsset asset in manyInCategory)
				{
					sounds[i++] = asset.Location;
				}
				ResolvedSounds[val.Key] = sounds;
			}
			else
			{
				ResolvedSounds[val.Key] = new AssetLocation[1] { val.Value.Clone().WithPathPrefix("sounds/") };
			}
		}
	}

	internal void PopulateDrops(IWorldAccessor worldForResolve)
	{
		using (MemoryStream ms = new MemoryStream(DropsPacket))
		{
			BinaryReader reader = new BinaryReader(ms);
			BlockDropItemStack[] drops = new BlockDropItemStack[reader.ReadInt32()];
			for (int i = 0; i < drops.Length; i++)
			{
				drops[i] = new BlockDropItemStack();
				drops[i].FromBytes(reader, worldForResolve.ClassRegistry);
				drops[i].Resolve(worldForResolve, "decode entity drops for ", Code);
			}
			Drops = drops;
		}
		DropsPacket = null;
	}
}
