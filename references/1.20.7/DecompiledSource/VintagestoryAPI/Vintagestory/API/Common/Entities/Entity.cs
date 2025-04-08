using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common.Entities;

/// <summary>
/// The basic class for all entities in the game
/// </summary>
public abstract class Entity : RegistryObject
{
	public static WaterSplashParticles SplashParticleProps;

	public static AdvancedParticleProperties[] FireParticleProps;

	public static FloatingSedimentParticles FloatingSedimentParticles;

	public static AirBubbleParticles AirBubbleParticleProps;

	public static SimpleParticleProperties bioLumiParticles;

	public static NormalizedSimplexNoise bioLumiNoise;

	/// <summary>
	/// Color used when the entity is being attacked
	/// </summary>
	protected int HurtColor = ColorUtil.ToRgba(255, 255, 100, 100);

	/// <summary>
	/// World where the entity is spawned in. Available on the game client and server.
	/// </summary>
	public IWorldAccessor World;

	/// <summary>
	/// The api, if you need it. Available on the game client and server.
	/// </summary>
	public ICoreAPI Api;

	/// <summary>
	/// The vanilla physics systems will call this method if a physics behavior was assigned to it. The game client for example requires this to be called for the current player to properly render the player. Available on the game client and server.
	/// </summary>
	public PhysicsTickDelegate PhysicsUpdateWatcher;

	/// <summary>
	/// An uptime value running activities. Available on the game client and server. Not synchronized.
	/// </summary>
	public Dictionary<string, long> ActivityTimers = new Dictionary<string, long>();

	/// <summary>
	/// Client position
	/// </summary>
	public EntityPos Pos = new EntityPos();

	/// <summary>
	/// Server simulated position. May not exactly match the client positon
	/// </summary>
	public EntityPos ServerPos = new EntityPos();

	/// <summary>
	/// Server simulated position copy. Needed by Entities server system to send pos updatess only if ServerPos differs noticably from PreviousServerPos
	/// </summary>
	public EntityPos PreviousServerPos = new EntityPos();

	/// <summary>
	/// The position where the entity last had contact with the ground. Set by the game client and server.
	/// </summary>
	public Vec3d PositionBeforeFalling = new Vec3d();

	public long InChunkIndex3d;

	/// <summary>
	/// The entities collision box. Offseted by the animation system when necessary. Set by the game client and server.
	/// </summary>
	public Cuboidf CollisionBox;

	/// <summary>
	/// The entities collision box. Not Offseted. Set by the game client and server.
	/// </summary>
	public Cuboidf OriginCollisionBox;

	/// <summary>
	/// The entities selection box. Offseted by the animation system when necessary. Set by the game client and server.
	/// </summary>
	public Cuboidf SelectionBox;

	/// <summary>
	/// The entities selection box. Not Offseted. Set by the game client and server.
	/// </summary>
	public Cuboidf OriginSelectionBox;

	/// <summary>
	/// Used by the teleporter block
	/// </summary>
	public bool Teleporting;

	/// <summary>
	/// A unique identifier for this entity. Set by the game client and server.
	/// </summary>
	public long EntityId;

	/// <summary>
	/// The range in blocks the entity has to be to a client to do physics and AI. When outside range, then <seealso cref="F:Vintagestory.API.Common.Entities.Entity.State" /> will be set to inactive
	/// </summary>
	public int SimulationRange;

	/// <summary>
	/// The face the entity is climbing on. Null if the entity is not climbing. Set by the game client and server.
	/// </summary>
	public BlockFacing ClimbingOnFace;

	public BlockFacing ClimbingIntoFace;

	/// <summary>
	/// Set by the game client and server.
	/// </summary>
	public Cuboidf ClimbingOnCollBox;

	/// <summary>
	/// True if this entity is in touch with the ground. Set by the game client and server.
	/// </summary>
	public bool OnGround;

	/// <summary>
	/// True if the bottom of the collisionbox is inside a liquid. Set by the game client and server.
	/// </summary>
	public bool FeetInLiquid;

	protected bool resetLightHsv;

	public bool InLava;

	public long InLavaBeginTotalMs;

	public long OnFireBeginTotalMs;

	/// <summary>
	/// True if the collisionbox is 2/3rds submerged in liquid. Set by the game client and server.
	/// </summary>
	public bool Swimming;

	/// <summary>
	/// True if the entity is in touch with something solid on the vertical axis. Set by the game client and server.
	/// </summary>
	public bool CollidedVertically;

	/// <summary>
	/// True if the entity is in touch with something solid on both horizontal axes. Set by the game client and server.
	/// </summary>
	public bool CollidedHorizontally;

	/// <summary>
	/// The current entity state. NOT stored in WatchedAttributes in from/tobytes when sending to client as always set to Active on client-side Initialize().  Server-side if saved it would likely initially be Despawned when an entity is first loaded from the save due to entities being despawned during the UnloadChunks process, so let's make it always Despawned for consistent behavior (it will be set to Active/Inactive during Initialize() anyhow)
	/// </summary>
	public EnumEntityState State = EnumEntityState.Despawned;

	public EntityDespawnData DespawnReason;

	/// <summary>
	/// Permanently stored entity attributes that are sent to client everytime they have been changed
	/// </summary>
	public SyncedTreeAttribute WatchedAttributes = new SyncedTreeAttribute();

	/// <summary>
	/// If entity debug mode is on, this info will be transitted to client and displayed above the entities head
	/// </summary>
	public SyncedTreeAttribute DebugAttributes = new SyncedTreeAttribute();

	/// <summary>
	/// Permanently stored entity attributes that are only client or only server side
	/// </summary>
	public SyncedTreeAttribute Attributes = new SyncedTreeAttribute();

	/// <summary>
	/// Set by the client renderer when the entity was rendered last frame
	/// </summary>
	public bool IsRendered;

	/// <summary>
	/// Set by the client renderer when the entity shadow was rendered last frame
	/// </summary>
	public bool IsShadowRendered;

	public EntityStats Stats;

	protected float fireDamageAccum;

	public double touchDistanceSq;

	public Vec3d ownPosRepulse = new Vec3d();

	public bool hasRepulseBehavior;

	public bool customRepulseBehavior;

	/// <summary>
	/// Used by PhysicsManager. Added here to increase performance
	/// 0 = not tracked, 1 = lowResTracked, 2 = fullyTracked
	/// </summary>
	public byte IsTracked;

	/// <summary>
	/// Used by the PhysicsManager to tell connected clients that the next entity position packet should not have its position change get interpolated. Gets set to false after the packet was sent
	/// </summary>
	public bool IsTeleport;

	/// <summary>
	/// If true, will call EntityBehavior.IntersectsRay. Default off to increase performance.
	/// </summary>
	public bool trickleDownRayIntersects;

	/// <summary>
	/// If true, will fully simulate animations on the server so one has access to the positions of all attachment points.
	/// If false, only root level attachment points will be available server side
	/// </summary>
	public bool requirePosesOnServer;

	/// <summary>
	/// Used for efficiency in multi-player servers, to avoid regenerating the packet again for each connected client
	/// </summary>
	public object packet;

	/// <summary>
	/// Used only when deserialising an entity, otherwise null
	/// </summary>
	private Dictionary<string, string> codeRemaps;

	protected bool alive = true;

	public float minHorRangeToClient;

	protected bool shapeFresh;

	/// <summary>
	/// Used by AItasks for perfomance. When searching for nearby entities we distinguish between (A) Creatures and (B) Inanimate entitie. Inanimate entities are items on the ground, projectiles, armor stands, rafts, falling blocks etc
	/// <br />Note 1: Dead creatures / corpses count as a Creature. EntityPlayer is a Creature of course.
	/// <br />Note 2: Straw Dummy we count as a Creature, because weapons can target it and bees can attack it. In contrast, Armor Stand we count as Inanimate, because nothing should ever attack or target it.
	/// </summary>
	public virtual bool IsCreature => false;

	public virtual bool CanStepPitch => Properties.Habitat != EnumHabitat.Air;

	public virtual bool CanSwivel
	{
		get
		{
			if (Properties.Habitat != EnumHabitat.Air)
			{
				if (Properties.Habitat == EnumHabitat.Land)
				{
					return !Swimming;
				}
				return true;
			}
			return false;
		}
	}

	public virtual bool CanSwivelNow => OnGround;

	/// <summary>
	/// Server simulated animations. Only takes care of stopping animations once they're done
	/// Set and Called by the Entities ServerSystem
	/// </summary>
	public virtual IAnimationManager AnimManager { get; set; }

	public bool IsOnFire
	{
		get
		{
			return WatchedAttributes.GetBool("onFire");
		}
		set
		{
			if (value != WatchedAttributes.GetBool("onFire"))
			{
				WatchedAttributes.SetBool("onFire", value);
			}
		}
	}

	public EntityProperties Properties { get; protected set; }

	public EntitySidedProperties SidedProperties
	{
		get
		{
			if (Properties == null)
			{
				return null;
			}
			if (World.Side.IsClient())
			{
				return Properties.Client;
			}
			return Properties.Server;
		}
	}

	/// <summary>
	/// Should return true when this entity should be interactable by a player or other entities
	/// </summary>
	public virtual bool IsInteractable => true;

	/// <summary>
	/// Used for passive physics simulation, together with the MaterialDensity to check how deep in the water the entity should float
	/// </summary>
	public virtual double SwimmingOffsetY => (double)SelectionBox.Y1 + (double)SelectionBox.Y2 * 0.66;

	/// <summary>
	/// CollidedVertically || CollidedHorizontally
	/// </summary>
	public bool Collided
	{
		get
		{
			if (!CollidedVertically)
			{
				return CollidedHorizontally;
			}
			return true;
		}
	}

	/// <summary>
	/// ServerPos on server, Pos on client
	/// </summary>
	public EntityPos SidedPos
	{
		get
		{
			if (World.Side != EnumAppSide.Server)
			{
				return Pos;
			}
			return ServerPos;
		}
	}

	/// <summary>
	/// The height of the eyes for the given entity.
	/// </summary>
	public virtual Vec3d LocalEyePos { get; set; } = new Vec3d();


	/// <summary>
	/// If gravity should applied to this entity
	/// </summary>
	public virtual bool ApplyGravity
	{
		get
		{
			if (Properties.Habitat != EnumHabitat.Land)
			{
				if (Properties.Habitat == EnumHabitat.Sea || Properties.Habitat == EnumHabitat.Underwater)
				{
					return !Swimming;
				}
				return false;
			}
			return true;
		}
	}

	/// <summary>
	/// Determines on whether an entity floats on liquids or not and how strongly items get pushed by water. Water has a density of 1000.
	/// A density below 1000 means the entity floats on top of water if has a physics simulation behavior attached to it.
	/// </summary>
	public virtual float MaterialDensity => 3000f;

	/// <summary>
	/// If set, the entity will emit dynamic light
	/// </summary>
	public virtual byte[] LightHsv { get; set; }

	/// <summary>
	/// If the entity should despawn next server tick. By default returns !Alive for non-creatures and creatures that don't have a Decay behavior
	/// </summary>
	public virtual bool ShouldDespawn => !Alive;

	/// <summary>
	/// Players and whatever the player rides on will be stored seperatly
	/// </summary>
	public virtual bool StoreWithChunk => true;

	public virtual bool AllowOutsideLoadedRange => false;

	/// <summary>
	/// Whether this entity should always stay in Active model, regardless on how far away other player are
	/// </summary>
	public virtual bool AlwaysActive { get; set; }

	/// <summary>
	/// True if the entity is in state active or inactive, or generally not dead (for non-living entities, 'dead' means ready to despawn)
	/// </summary>
	public virtual bool Alive
	{
		get
		{
			return alive;
		}
		set
		{
			WatchedAttributes.SetInt("entityDead", (!value) ? 1 : 0);
			alive = value;
		}
	}

	public virtual bool AdjustCollisionBoxToAnimation => !alive;

	public float IdleSoundChanceModifier
	{
		get
		{
			return WatchedAttributes.GetFloat("idleSoundChanceModifier", 1f);
		}
		set
		{
			WatchedAttributes.SetFloat("idleSoundChanceModifier", value);
		}
	}

	/// <summary>
	/// Used by some renderers to apply an overal color tint on the entity
	/// </summary>
	public int RenderColor { get; set; } = -1;


	/// <summary>
	/// A small offset used to prevent players from clipping through the blocks above ladders: relevant if the entity's collision box is sometimes adjusted by the game code
	/// </summary>
	public virtual double LadderFixDelta => 0.0;

	public bool ShapeFresh => shapeFresh;

	public virtual double FrustumSphereRadius => Math.Max(3f, Math.Max(SelectionBox?.XSize ?? 1f, SelectionBox?.YSize ?? 1f));

	public event Action OnInitialized;

	static Entity()
	{
		SplashParticleProps = new WaterSplashParticles();
		FireParticleProps = new AdvancedParticleProperties[3];
		FloatingSedimentParticles = new FloatingSedimentParticles();
		AirBubbleParticleProps = new AirBubbleParticles();
		FireParticleProps[0] = new AdvancedParticleProperties
		{
			HsvaColor = new NatFloat[4]
			{
				NatFloat.createUniform(30f, 20f),
				NatFloat.createUniform(255f, 50f),
				NatFloat.createUniform(255f, 50f),
				NatFloat.createUniform(255f, 0f)
			},
			GravityEffect = NatFloat.createUniform(0f, 0f),
			Velocity = new NatFloat[3]
			{
				NatFloat.createUniform(0.2f, 0.05f),
				NatFloat.createUniform(0.5f, 0.1f),
				NatFloat.createUniform(0.2f, 0.05f)
			},
			Size = NatFloat.createUniform(0.25f, 0f),
			Quantity = NatFloat.createUniform(0.25f, 0f),
			VertexFlags = 128,
			SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.5f),
			SelfPropelled = true
		};
		FireParticleProps[1] = new AdvancedParticleProperties
		{
			HsvaColor = new NatFloat[4]
			{
				NatFloat.createUniform(30f, 20f),
				NatFloat.createUniform(255f, 50f),
				NatFloat.createUniform(255f, 50f),
				NatFloat.createUniform(255f, 0f)
			},
			OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
			GravityEffect = NatFloat.createUniform(0f, 0f),
			Velocity = new NatFloat[3]
			{
				NatFloat.createUniform(0f, 0.02f),
				NatFloat.createUniform(0f, 0.02f),
				NatFloat.createUniform(0f, 0.02f)
			},
			Size = NatFloat.createUniform(0.3f, 0.05f),
			Quantity = NatFloat.createUniform(0.25f, 0f),
			VertexFlags = 128,
			SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1f),
			LifeLength = NatFloat.createUniform(0.5f, 0f),
			ParticleModel = EnumParticleModel.Quad
		};
		FireParticleProps[2] = new AdvancedParticleProperties
		{
			HsvaColor = new NatFloat[4]
			{
				NatFloat.createUniform(0f, 0f),
				NatFloat.createUniform(0f, 0f),
				NatFloat.createUniform(40f, 30f),
				NatFloat.createUniform(220f, 50f)
			},
			OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
			GravityEffect = NatFloat.createUniform(0f, 0f),
			Velocity = new NatFloat[3]
			{
				NatFloat.createUniform(0f, 0.05f),
				NatFloat.createUniform(0.2f, 0.3f),
				NatFloat.createUniform(0f, 0.05f)
			},
			Size = NatFloat.createUniform(0.3f, 0.05f),
			Quantity = NatFloat.createUniform(0.25f, 0f),
			SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1.5f),
			LifeLength = NatFloat.createUniform(1.5f, 0f),
			ParticleModel = EnumParticleModel.Quad,
			SelfPropelled = true
		};
		bioLumiParticles = new SimpleParticleProperties
		{
			Color = ColorUtil.ToRgba(255, 0, 230, 142),
			MinSize = 0.02f,
			MaxSize = 0.07f,
			MinQuantity = 1f,
			GravityEffect = 0f,
			LifeLength = 1f,
			ParticleModel = EnumParticleModel.Quad,
			ShouldDieInAir = true,
			VertexFlags = 255,
			MinPos = new Vec3d(),
			AddPos = new Vec3d()
		};
		bioLumiParticles.ShouldDieInAir = true;
		bioLumiParticles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, -150f);
		bioLumiParticles.MinSize = 0.02f;
		bioLumiParticles.MaxSize = 0.07f;
		bioLumiNoise = new NormalizedSimplexNoise(new double[2] { 1.0, 0.5 }, new double[2] { 5.0, 10.0 }, 97901L);
	}

	/// <summary>
	/// Creates a new instance of an entity
	/// </summary>
	public Entity()
	{
		SimulationRange = GlobalConstants.DefaultSimulationRange;
		AnimManager = new AnimationManager();
		Stats = new EntityStats(this);
		WatchedAttributes.SetAttribute("animations", new TreeAttribute());
		WatchedAttributes.SetAttribute("extraInfoText", new TreeAttribute());
	}

	/// <summary>
	/// Creates a minimally populated entity with configurable tracking range, no Stats, no AnimManager and no animations attribute. Currently used by EntityItem.
	/// </summary>
	/// <param name="trackingRange"></param>
	protected Entity(int trackingRange)
	{
		SimulationRange = trackingRange;
		WatchedAttributes.SetAttribute("extraInfoText", new TreeAttribute());
	}

	/// <summary>
	/// Called when the entity got hurt. On the client side, dmgSource is null
	/// </summary>
	/// <param name="dmgSource"></param>
	/// <param name="damage"></param>
	public virtual void OnHurt(DamageSource dmgSource, float damage)
	{
	}

	/// <summary>
	/// Called when this entity got created or loaded
	/// </summary>
	/// <param name="properties"></param>
	/// <param name="api"></param>
	/// <param name="InChunkIndex3d"></param>
	public virtual void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
	{
		World = api.World;
		Api = api;
		Properties = properties;
		Class = properties.Class;
		this.InChunkIndex3d = InChunkIndex3d;
		alive = WatchedAttributes.GetInt("entityDead") == 0;
		WatchedAttributes.SetFloat("onHurt", 0f);
		int onHurtCounter = WatchedAttributes.GetInt("onHurtCounter");
		WatchedAttributes.RegisterModifiedListener("onHurt", delegate
		{
			float @float = WatchedAttributes.GetFloat("onHurt");
			if (@float != 0f)
			{
				int @int = WatchedAttributes.GetInt("onHurtCounter");
				if (@int != onHurtCounter)
				{
					onHurtCounter = @int;
					if (Attributes.GetInt("dmgkb") == 0)
					{
						Attributes.SetInt("dmgkb", 1);
					}
					if ((double)@float > 0.05)
					{
						SetActivityRunning("invulnerable", 500);
						if (World.Side == EnumAppSide.Client)
						{
							OnHurt(null, WatchedAttributes.GetFloat("onHurt"));
						}
					}
				}
			}
		});
		WatchedAttributes.RegisterModifiedListener("onFire", updateOnFire);
		WatchedAttributes.RegisterModifiedListener("entityDead", updateColSelBoxes);
		if (World.Side == EnumAppSide.Client && Properties.Client.SizeGrowthFactor != 0f)
		{
			WatchedAttributes.RegisterModifiedListener("grow", delegate
			{
				float sizeGrowthFactor = Properties.Client.SizeGrowthFactor;
				if (sizeGrowthFactor != 0f)
				{
					EntityClientProperties client = World.GetEntityType(Code).Client;
					Properties.Client.Size = client.Size + WatchedAttributes.GetTreeAttribute("grow").GetFloat("age") * sizeGrowthFactor;
				}
			});
		}
		if (Properties.CollisionBoxSize != null || properties.SelectionBoxSize != null)
		{
			updateColSelBoxes();
		}
		DoInitialActiveCheck(api);
		if (api.Side == EnumAppSide.Server && properties.Client != null && properties.Client.TexturesAlternatesCount > 0 && !WatchedAttributes.HasAttribute("textureIndex"))
		{
			WatchedAttributes.SetInt("textureIndex", World.Rand.Next(properties.Client.TexturesAlternatesCount + 1));
		}
		Properties.Initialize(this, api);
		Properties.Client.DetermineLoadedShape(EntityId);
		if (api.Side == EnumAppSide.Server)
		{
			AnimManager.LoadAnimator(api, this, properties.Client.LoadedShapeForEntity, null, requirePosesOnServer, "head");
			AnimManager.OnServerTick(0f);
		}
		else
		{
			AnimManager.Init(api, this);
		}
		LocalEyePos.Y = Properties.EyeHeight;
		TriggerOnInitialized();
	}

	public virtual void AfterInitialized(bool onFirstSpawn)
	{
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			behavior.AfterInitialized(onFirstSpawn);
		}
	}

	protected void TriggerOnInitialized()
	{
		this.OnInitialized?.Invoke();
	}

	protected void DoInitialActiveCheck(ICoreAPI api)
	{
		if (AlwaysActive || api.Side == EnumAppSide.Client)
		{
			State = EnumEntityState.Active;
			return;
		}
		State = EnumEntityState.Inactive;
		IPlayer[] players = World.AllOnlinePlayers;
		for (int i = 0; i < players.Length; i++)
		{
			EntityPlayer entityPlayer = players[i].Entity;
			if (entityPlayer != null && Pos.InRangeOf(entityPlayer.Pos, SimulationRange * SimulationRange))
			{
				State = EnumEntityState.Active;
				break;
			}
		}
	}

	protected void updateColSelBoxes()
	{
		if (WatchedAttributes.GetInt("entityDead") == 0 || Properties.DeadCollisionBoxSize == null)
		{
			SetCollisionBox(Properties.CollisionBoxSize.X, Properties.CollisionBoxSize.Y);
			Vec2f selboxs = Properties.SelectionBoxSize ?? Properties.CollisionBoxSize;
			SetSelectionBox(selboxs.X, selboxs.Y);
		}
		else
		{
			SetCollisionBox(Properties.DeadCollisionBoxSize.X, Properties.DeadCollisionBoxSize.Y);
			Vec2f selboxs2 = Properties.DeadSelectionBoxSize ?? Properties.DeadCollisionBoxSize;
			SetSelectionBox(selboxs2.X, selboxs2.Y);
		}
		double touchdist = Math.Max(0.001f, SelectionBox.XSize / 2f);
		touchDistanceSq = touchdist * touchdist;
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			behavior.UpdateColSelBoxes();
		}
	}

	protected void updateOnFire()
	{
		bool onfire = IsOnFire;
		if (onfire)
		{
			OnFireBeginTotalMs = World.ElapsedMilliseconds;
		}
		if (onfire && LightHsv == null)
		{
			LightHsv = new byte[3] { 5, 7, 10 };
			resetLightHsv = true;
		}
		if (!onfire && resetLightHsv)
		{
			LightHsv = null;
		}
	}

	/// <summary>
	/// Called when something tries to given an itemstack to this entity
	/// </summary>
	/// <param name="itemstack"></param>
	/// <returns></returns>
	public virtual bool TryGiveItemStack(ItemStack itemstack)
	{
		EnumHandling handled = EnumHandling.PassThrough;
		bool ok = false;
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			ok |= behavior.TryGiveItemStack(itemstack, ref handled);
			if (handled == EnumHandling.PreventSubsequent)
			{
				return ok;
			}
		}
		return ok;
	}

	/// <summary>
	/// Is called before the entity is killed, should return what items this entity should drop. Return null or empty array for no drops.
	/// </summary>
	/// <param name="world"></param>
	/// <param name="pos"></param>
	/// <param name="byPlayer"></param>
	/// <returns></returns>
	public virtual ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer)
	{
		EnumHandling handled = EnumHandling.PassThrough;
		ItemStack[] stacks = null;
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			stacks = behavior.GetDrops(world, pos, byPlayer, ref handled);
			if (handled == EnumHandling.PreventSubsequent)
			{
				return stacks;
			}
		}
		if (handled == EnumHandling.PreventDefault)
		{
			return stacks;
		}
		if (Properties.Drops == null)
		{
			return null;
		}
		List<ItemStack> todrop = new List<ItemStack>();
		float dropMul = 1f;
		JsonObject attributes = Properties.Attributes;
		if ((attributes == null || !attributes["isMechanical"].AsBool()) && byPlayer?.Entity != null)
		{
			dropMul = 1f + byPlayer.Entity.Stats.GetBlended("animalLootDropRate");
		}
		for (int i = 0; i < Properties.Drops.Length; i++)
		{
			BlockDropItemStack bdStack = Properties.Drops[i];
			float extraMul = 1f;
			if (bdStack.DropModbyStat != null && byPlayer?.Entity != null)
			{
				extraMul = byPlayer.Entity.Stats.GetBlended(bdStack.DropModbyStat);
			}
			ItemStack stack = bdStack.GetNextItemStack(dropMul * extraMul);
			if (stack != null)
			{
				if (stack.Collectible is IResolvableCollectible irc)
				{
					DummySlot slot = new DummySlot(stack);
					irc.Resolve(slot, world);
					stack = slot.Itemstack;
				}
				todrop.Add(stack);
				if (bdStack.LastDrop)
				{
					break;
				}
			}
		}
		return todrop.ToArray();
	}

	/// <summary>
	/// Teleports the entity to given position. Actual teleport is delayed until target chunk is loaded.
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <param name="onTeleported"></param>
	public virtual void TeleportToDouble(double x, double y, double z, Action onTeleported = null)
	{
		Teleporting = true;
		if (World.Api is ICoreServerAPI sapi)
		{
			sapi.WorldManager.LoadChunkColumnPriority((int)x / 32, (int)z / 32, new ChunkLoadOptions
			{
				OnLoaded = delegate
				{
					IsTeleport = true;
					Pos.SetPos(x, y, z);
					ServerPos.SetPos(x, y, z);
					PositionBeforeFalling.Set(x, y, z);
					Pos.Motion.Set(0.0, 0.0, 0.0);
					onTeleported?.Invoke();
					Teleporting = false;
				}
			});
		}
	}

	/// <summary>
	/// Teleports the entity to given position
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	public virtual void TeleportTo(int x, int y, int z)
	{
		TeleportToDouble(x, y, z);
	}

	/// <summary>
	/// Teleports the entity to given position
	/// </summary>
	/// <param name="position"></param>
	public virtual void TeleportTo(Vec3d position)
	{
		TeleportToDouble(position.X, position.Y, position.Z);
	}

	/// <summary>
	/// Teleports the entity to given position
	/// </summary>
	/// <param name="position"></param>
	public virtual void TeleportTo(BlockPos position)
	{
		TeleportToDouble(position.X, position.Y, position.Z);
	}

	/// <summary>
	/// Teleports the entity to given position
	/// </summary>
	/// <param name="position"></param>
	/// <param name="onTeleported"></param>
	public virtual void TeleportTo(EntityPos position, Action onTeleported = null)
	{
		Pos.Yaw = position.Yaw;
		Pos.Pitch = position.Pitch;
		Pos.Roll = position.Roll;
		ServerPos.Yaw = position.Yaw;
		ServerPos.Pitch = position.Pitch;
		ServerPos.Roll = position.Roll;
		TeleportToDouble(position.X, position.Y, position.Z, onTeleported);
	}

	/// <summary>
	/// Called when the entity should be receiving damage from given source
	/// </summary>
	/// <param name="damageSource"></param>
	/// <param name="damage"></param>
	/// <returns>True if the entity actually received damage</returns>
	public virtual bool ReceiveDamage(DamageSource damageSource, float damage)
	{
		if ((!Alive || IsActivityRunning("invulnerable")) && damageSource.Type != EnumDamageType.Heal)
		{
			return false;
		}
		if (ShouldReceiveDamage(damageSource, damage))
		{
			foreach (EntityBehavior behavior in SidedProperties.Behaviors)
			{
				behavior.OnEntityReceiveDamage(damageSource, ref damage);
			}
			if (damageSource.Type != EnumDamageType.Heal && damage > 0f)
			{
				WatchedAttributes.SetInt("onHurtCounter", WatchedAttributes.GetInt("onHurtCounter") + 1);
				WatchedAttributes.SetFloat("onHurt", damage);
				if (damage > 0.05f)
				{
					AnimManager.StartAnimation("hurt");
				}
			}
			if (damageSource.GetSourcePosition() != null)
			{
				bool verticalAttack = false;
				if (damageSource.GetAttackAngle(Pos.XYZ, out var _, out var attackPitch))
				{
					verticalAttack = Math.Abs(attackPitch) > 1.3962633609771729 || Math.Abs(attackPitch) < 0.1745329201221466;
				}
				Vec3d dir = (SidedPos.XYZ - damageSource.GetSourcePosition()).Normalize();
				if (verticalAttack)
				{
					dir.Y = 0.05000000074505806;
					dir.Normalize();
				}
				else
				{
					dir.Y = 0.699999988079071;
				}
				dir.Y /= damageSource.YDirKnockbackDiv;
				float factor = damageSource.KnockbackStrength * GameMath.Clamp((1f - Properties.KnockbackResistance) / 10f, 0f, 1f);
				WatchedAttributes.SetFloat("onHurtDir", (float)Math.Atan2(dir.X, dir.Z));
				WatchedAttributes.SetDouble("kbdirX", dir.X * (double)factor);
				WatchedAttributes.SetDouble("kbdirY", dir.Y * (double)factor);
				WatchedAttributes.SetDouble("kbdirZ", dir.Z * (double)factor);
			}
			else
			{
				WatchedAttributes.SetDouble("kbdirX", 0.0);
				WatchedAttributes.SetDouble("kbdirY", 0.0);
				WatchedAttributes.SetDouble("kbdirZ", 0.0);
				WatchedAttributes.SetFloat("onHurtDir", -999f);
			}
			return damage > 0f;
		}
		return false;
	}

	/// <summary>
	/// Should return true if the entity can get damaged by given damageSource. Is called by ReceiveDamage.
	/// </summary>
	/// <param name="damageSource"></param>
	/// <param name="damage"></param>
	/// <returns></returns>
	public virtual bool ShouldReceiveDamage(DamageSource damageSource, float damage)
	{
		return true;
	}

	/// <summary>
	/// Called every 1/75 second
	/// </summary>
	/// <param name="dt"></param>
	public virtual void OnGameTick(float dt)
	{
		if (World.EntityDebugMode)
		{
			UpdateDebugAttributes();
			DebugAttributes.MarkAllDirty();
		}
		if (World.Side == EnumAppSide.Client)
		{
			int val = RemainingActivityTime("invulnerable");
			if (val >= 0)
			{
				RenderColor = ColorUtil.ColorOverlay(HurtColor, -1, 1f - (float)val / 500f);
			}
			alive = WatchedAttributes.GetInt("entityDead") == 0;
			if (World.FrameProfiler.Enabled)
			{
				World.FrameProfiler.Enter("behaviors");
				foreach (EntityBehavior behavior in SidedProperties.Behaviors)
				{
					behavior.OnGameTick(dt);
					World.FrameProfiler.Mark(behavior.ProfilerName);
				}
				World.FrameProfiler.Leave();
			}
			else
			{
				foreach (EntityBehavior behavior3 in SidedProperties.Behaviors)
				{
					behavior3.OnGameTick(dt);
				}
			}
			if (World.Rand.NextDouble() < (double)(IdleSoundChanceModifier * Properties.IdleSoundChance) / 100.0 && Alive)
			{
				PlayEntitySound("idle", null, randomizePitch: true, Properties.IdleSoundRange);
			}
		}
		else
		{
			if (!shapeFresh && requirePosesOnServer)
			{
				CompositeShape compositeShape = Properties.Client.Shape;
				Shape entityShape = Properties.Client.LoadedShapeForEntity;
				if (entityShape != null)
				{
					OnTesselation(ref entityShape, compositeShape.Base.ToString());
					OnTesselated();
				}
			}
			if (World.FrameProfiler.Enabled)
			{
				World.FrameProfiler.Enter("behaviors");
				foreach (EntityBehavior behavior2 in SidedProperties.Behaviors)
				{
					behavior2.OnGameTick(dt);
					World.FrameProfiler.Mark(behavior2.ProfilerName);
				}
				World.FrameProfiler.Leave();
			}
			else
			{
				foreach (EntityBehavior behavior4 in SidedProperties.Behaviors)
				{
					behavior4.OnGameTick(dt);
				}
			}
			if (InLava)
			{
				Ignite();
			}
		}
		if (IsOnFire)
		{
			Block fluidBlock = World.BlockAccessor.GetBlock(Pos.AsBlockPos, 2);
			if (((fluidBlock.IsLiquid() && fluidBlock.LiquidCode != "lava") || World.ElapsedMilliseconds - OnFireBeginTotalMs > 12000) && !InLava)
			{
				IsOnFire = false;
			}
			else
			{
				if (World.Side == EnumAppSide.Client)
				{
					int index = Math.Min(FireParticleProps.Length - 1, Api.World.Rand.Next(FireParticleProps.Length + 1));
					AdvancedParticleProperties particles = FireParticleProps[index];
					particles.basePos.Set(Pos.X, Pos.Y + (double)(SelectionBox.YSize / 2f), Pos.Z);
					particles.PosOffset[0].var = SelectionBox.XSize / 2f;
					particles.PosOffset[1].var = SelectionBox.YSize / 2f;
					particles.PosOffset[2].var = SelectionBox.ZSize / 2f;
					particles.Velocity[0].avg = (float)Pos.Motion.X * 10f;
					particles.Velocity[1].avg = (float)Pos.Motion.Y * 5f;
					particles.Velocity[2].avg = (float)Pos.Motion.Z * 10f;
					particles.Quantity.avg = GameMath.Sqrt(particles.PosOffset[0].var + particles.PosOffset[1].var + particles.PosOffset[2].var) * index switch
					{
						1 => 3f, 
						0 => 0.5f, 
						_ => 1.25f, 
					};
					Api.World.SpawnParticles(particles);
				}
				else
				{
					ApplyFireDamage(dt);
				}
				if (!alive && InLava && !(this is EntityPlayer))
				{
					DieInLava();
				}
			}
		}
		if (World.Side == EnumAppSide.Server)
		{
			try
			{
				AnimManager.OnServerTick(dt);
			}
			catch (Exception)
			{
				World.Logger.Error("Error ticking animations for entity " + Code.ToShortString() + " at " + SidedPos.AsBlockPos);
				throw;
			}
		}
		if (CollisionBox != null)
		{
			ownPosRepulse.Set(SidedPos.X + (double)(CollisionBox.X2 - OriginCollisionBox.X2), SidedPos.Y + (double)(CollisionBox.Y2 - OriginCollisionBox.Y2), SidedPos.Z + (double)(CollisionBox.Z2 - OriginCollisionBox.Z2));
		}
		World.FrameProfiler.Mark("entity-animation-ticking");
	}

	protected void ApplyFireDamage(float dt)
	{
		fireDamageAccum += dt;
		if (fireDamageAccum > 1f)
		{
			ReceiveDamage(new DamageSource
			{
				Source = EnumDamageSource.Internal,
				Type = EnumDamageType.Fire
			}, 0.5f);
			fireDamageAccum = 0f;
		}
	}

	protected void DieInLava()
	{
		float q = GameMath.Clamp(SelectionBox.XSize * SelectionBox.YSize * SelectionBox.ZSize * 150f, 10f, 150f);
		Api.World.SpawnParticles(q, ColorUtil.ColorFromRgba(20, 20, 20, 255), new Vec3d(ServerPos.X + (double)SelectionBox.X1, ServerPos.Y + (double)SelectionBox.Y1, ServerPos.Z + (double)SelectionBox.Z1), new Vec3d(ServerPos.X + (double)SelectionBox.X2, ServerPos.Y + (double)SelectionBox.Y2, ServerPos.Z + (double)SelectionBox.Z2), new Vec3f(-1f, -1f, -1f), new Vec3f(2f, 2f, 2f), 2f, 1f, 1f, EnumParticleModel.Cube);
		Die(EnumDespawnReason.Combusted);
	}

	public virtual void OnAsyncParticleTick(float dt, IAsyncParticleManager manager)
	{
	}

	public virtual void Ignite()
	{
		IsOnFire = true;
	}

	public virtual ITexPositionSource GetTextureSource()
	{
		if (Api.Side != EnumAppSide.Client)
		{
			return null;
		}
		ITexPositionSource texSource = null;
		List<EntityBehavior> bhs = Properties.Client?.Behaviors;
		EnumHandling handling = EnumHandling.PassThrough;
		if (bhs != null)
		{
			foreach (EntityBehavior item in bhs)
			{
				texSource = item.GetTextureSource(ref handling);
				if (handling == EnumHandling.PreventSubsequent)
				{
					return texSource;
				}
			}
		}
		if (handling == EnumHandling.PreventDefault)
		{
			return texSource;
		}
		int altTexNumber = WatchedAttributes.GetInt("textureIndex");
		return (Api as ICoreClientAPI).Tesselator.GetTextureSource(this, null, altTexNumber);
	}

	public virtual void MarkShapeModified()
	{
		shapeFresh = false;
	}

	/// <summary>
	/// Called by EntityShapeRenderer.cs before tesselating the entity shape
	/// </summary>
	/// <param name="entityShape"></param>
	/// <param name="shapePathForLogging"></param>
	public virtual void OnTesselation(ref Shape entityShape, string shapePathForLogging)
	{
		shapeFresh = true;
		bool shapeIsCloned = false;
		OnTesselation(ref entityShape, shapePathForLogging, ref shapeIsCloned);
		if (shapeIsCloned && entityShape.Animations != null)
		{
			Animation[] animations = entityShape.Animations;
			for (int i = 0; i < animations.Length; i++)
			{
				animations[i].PrevNextKeyFrameByFrame = null;
			}
		}
	}

	protected virtual void OnTesselation(ref Shape entityShape, string shapePathForLogging, ref bool shapeIsCloned)
	{
		shapeFresh = true;
		CompositeShape cshape = Properties.Client.Shape;
		if (cshape?.Overlays != null && cshape.Overlays.Length != 0)
		{
			shapeIsCloned = true;
			entityShape = entityShape.Clone();
			IDictionary<string, CompositeTexture> textures = Properties.Client.Textures;
			CompositeShape[] overlays = cshape.Overlays;
			foreach (CompositeShape overlay in overlays)
			{
				Shape shape = Api.Assets.TryGet(overlay.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json"))?.ToObject<Shape>();
				if (shape == null)
				{
					Api.Logger.Error("Entity {0} defines a shape overlay {1}, but no such file found. Will ignore.", Code, overlay.Base);
					continue;
				}
				string texturePrefixCode = null;
				JsonObject attributes = Properties.Attributes;
				if (attributes != null && attributes["wearableTexturePrefixCode"].Exists)
				{
					texturePrefixCode = Properties.Attributes["wearableTexturePrefixCode"].AsString();
				}
				entityShape.StepParentShape(shape, overlay.Base.ToShortString(), shapePathForLogging, Api.Logger, delegate(string texcode, AssetLocation tloc)
				{
					if (Api is ICoreClientAPI coreClientAPI && (texturePrefixCode != null || !textures.ContainsKey(texcode)))
					{
						CompositeTexture compositeTexture2 = (textures[texturePrefixCode + "-" + texcode] = new CompositeTexture(tloc));
						CompositeTexture compositeTexture3 = compositeTexture2;
						compositeTexture3.Bake(Api.Assets);
						coreClientAPI.EntityTextureAtlas.GetOrInsertTexture(compositeTexture3.Baked.TextureFilenames[0], out var textureSubId, out var _);
						compositeTexture3.Baked.TextureSubId = textureSubId;
					}
				});
			}
		}
		string[] willDisableElements = null;
		JsonObject attributes2 = Properties.Attributes;
		if (attributes2 != null && attributes2["disableElements"].Exists)
		{
			willDisableElements = Properties.Attributes["disableElements"].AsArray<string>();
		}
		List<EntityBehavior> bhs = ((World.Side != EnumAppSide.Server) ? Properties.Client?.Behaviors : Properties.Server?.Behaviors);
		EnumHandling handling = EnumHandling.PassThrough;
		if (bhs != null)
		{
			foreach (EntityBehavior item in bhs)
			{
				item.OnTesselation(ref entityShape, shapePathForLogging, ref shapeIsCloned, ref willDisableElements);
				if (handling == EnumHandling.PreventSubsequent)
				{
					break;
				}
			}
		}
		if (willDisableElements != null && willDisableElements.Length != 0)
		{
			if (!shapeIsCloned)
			{
				Shape newShape = entityShape.Clone();
				entityShape = newShape;
				shapeIsCloned = true;
			}
			entityShape.RemoveElements(willDisableElements);
		}
		if (shapeIsCloned)
		{
			AnimManager.LoadAnimator(World.Api, this, entityShape, AnimManager.Animator?.Animations, requirePosesOnServer, "head");
		}
		else
		{
			AnimManager.LoadAnimatorCached(World.Api, this, entityShape, AnimManager.Animator?.Animations, requirePosesOnServer, "head");
		}
	}

	public virtual void OnTesselated()
	{
		List<EntityBehavior> bhs = ((World.Side != EnumAppSide.Server) ? Properties.Client?.Behaviors : Properties.Server?.Behaviors);
		if (bhs == null)
		{
			return;
		}
		foreach (EntityBehavior item in bhs)
		{
			item.OnTesselated();
		}
	}

	/// <summary>
	/// Called when the entity collided vertically
	/// </summary>
	/// <param name="motionY"></param>
	public virtual void OnFallToGround(double motionY)
	{
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			behavior.OnFallToGround(PositionBeforeFalling, motionY);
		}
	}

	/// <summary>
	/// Called when the entity collided with something solid and <see cref="P:Vintagestory.API.Common.Entities.Entity.Collided" /> was false before
	/// </summary>
	public virtual void OnCollided()
	{
	}

	/// <summary>
	/// Called when the entity got in touch with a liquid
	/// </summary>
	public virtual void OnCollideWithLiquid()
	{
		if (World.Side == EnumAppSide.Server)
		{
			return;
		}
		EntityPos pos = SidedPos;
		float yDistance = (float)Math.Abs(PositionBeforeFalling.Y - pos.Y);
		double width = SelectionBox.XSize;
		double height = SelectionBox.YSize;
		double splashStrength = (double)(2f * GameMath.Sqrt(width * height)) + pos.Motion.Length() * 10.0;
		if (!(splashStrength < 0.4000000059604645) && !(yDistance < 0.25f))
		{
			string sound = (new string[3] { "sounds/environment/smallsplash", "sounds/environment/mediumsplash", "sounds/environment/largesplash" })[(int)GameMath.Clamp(splashStrength / 1.6, 0.0, 2.0)];
			splashStrength = Math.Min(10.0, splashStrength);
			float qmod = GameMath.Sqrt(width * height);
			World.PlaySoundAt(new AssetLocation(sound), (float)pos.X, (float)pos.InternalY, (float)pos.Z);
			BlockPos blockpos = pos.AsBlockPos;
			Vec3d aboveBlockPos = new Vec3d(Pos.X, (double)blockpos.InternalY + 1.02, Pos.Z);
			World.SpawnCubeParticles(blockpos, aboveBlockPos, SelectionBox.XSize, (int)((double)(qmod * 8f) * splashStrength), 0.75f);
			World.SpawnCubeParticles(blockpos, aboveBlockPos, SelectionBox.XSize, (int)((double)(qmod * 8f) * splashStrength), 0.25f);
			if (splashStrength >= 2.0)
			{
				SplashParticleProps.BasePos.Set(pos.X - width / 2.0, pos.Y - 0.75, pos.Z - width / 2.0);
				SplashParticleProps.AddPos.Set(width, 0.75, width);
				SplashParticleProps.AddVelocity.Set((float)GameMath.Clamp(pos.Motion.X * 30.0, -2.0, 2.0), 1f, (float)GameMath.Clamp(pos.Motion.Z * 30.0, -2.0, 2.0));
				SplashParticleProps.QuantityMul = (float)(splashStrength - 1.0) * qmod;
				World.SpawnParticles(SplashParticleProps);
			}
			SpawnWaterMovementParticles((float)Math.Min(0.25, splashStrength / 10.0), 0.0, -0.5);
		}
	}

	protected virtual void SpawnWaterMovementParticles(float quantityMul, double offx = 0.0, double offy = 0.0, double offz = 0.0)
	{
		if (World.Side == EnumAppSide.Server)
		{
			return;
		}
		ClimateCondition climate = (Api as ICoreClientAPI).World.Player.Entity.selfClimateCond;
		if (climate == null)
		{
			return;
		}
		float dist = Math.Max(0f, (28f - climate.Temperature) / 6f) + Math.Max(0f, (0.8f - climate.Rainfall) * 3f);
		double qmul = bioLumiNoise.Noise(SidedPos.X / 300.0, SidedPos.Z / 300.0) * 2.0 - 1.0 - (double)dist;
		if (!(qmul < 0.0))
		{
			if (this is EntityPlayer && Swimming)
			{
				bioLumiParticles.MinPos.Set(SidedPos.X + (double)(2f * SelectionBox.X1), SidedPos.Y + offy + 0.5 + (double)(1.25f * SelectionBox.Y1), SidedPos.Z + (double)(2f * SelectionBox.Z1));
				bioLumiParticles.AddPos.Set(3f * SelectionBox.XSize, 0.5f * SelectionBox.YSize, 3f * SelectionBox.ZSize);
			}
			else
			{
				bioLumiParticles.MinPos.Set(SidedPos.X + (double)(1.25f * SelectionBox.X1), SidedPos.Y + offy + (double)(1.25f * SelectionBox.Y1), SidedPos.Z + (double)(1.25f * SelectionBox.Z1));
				bioLumiParticles.AddPos.Set(1.5f * SelectionBox.XSize, 1.5f * SelectionBox.YSize, 1.5f * SelectionBox.ZSize);
			}
			bioLumiParticles.MinQuantity = Math.Min(200f, 100f * quantityMul * (float)qmul);
			bioLumiParticles.MinVelocity.Set(-0.2f + 2f * (float)Pos.Motion.X, -0.2f + 2f * (float)Pos.Motion.Y, -0.2f + 2f * (float)Pos.Motion.Z);
			bioLumiParticles.AddVelocity.Set(0.4f + 2f * (float)Pos.Motion.X, 0.4f + 2f * (float)Pos.Motion.Y, 0.4f + 2f * (float)Pos.Motion.Z);
			World.SpawnParticles(bioLumiParticles);
		}
	}

	/// <summary>
	/// Called when after the got loaded from the savegame (not called during spawn)
	/// </summary>
	public virtual void OnEntityLoaded()
	{
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			behavior.OnEntityLoaded();
		}
		Properties.Client.Renderer?.OnEntityLoaded();
		MarkShapeModified();
	}

	/// <summary>
	/// Called when the entity spawns (not called when loaded from the savegame).
	/// </summary>
	public virtual void OnEntitySpawn()
	{
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			behavior.OnEntitySpawn();
		}
		Properties.Client.Renderer?.OnEntityLoaded();
		MarkShapeModified();
	}

	/// <summary>
	/// Called when the entity despawns
	/// </summary>
	/// <param name="despawn"></param>
	public virtual void OnEntityDespawn(EntityDespawnData despawn)
	{
		if (SidedProperties == null)
		{
			return;
		}
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			behavior.OnEntityDespawn(despawn);
		}
		AnimManager.Dispose();
		WatchedAttributes.OnModified.Clear();
	}

	/// <summary>
	/// Called when the entity has left a liquid
	/// </summary>
	public virtual void OnExitedLiquid()
	{
	}

	/// <summary>
	/// Called when an entity has interacted with this entity
	/// </summary>
	/// <param name="byEntity"></param>
	/// <param name="itemslot">If being interacted with a block/item, this should be the slot the item is being held in</param>
	/// <param name="hitPosition">Relative position on the entites hitbox where the entity interacted at</param>
	/// <param name="mode">0 = attack, 1 = interact</param>
	public virtual void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode)
	{
		EnumHandling handled = EnumHandling.PassThrough;
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			behavior.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
			if (handled == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
	}

	/// <summary>
	/// Called when a player looks at the entity with interaction help enabled
	/// </summary>
	/// <param name="world"></param>
	/// <param name="es"></param>
	/// <param name="player"></param>
	/// <returns></returns>
	public virtual WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player)
	{
		EnumHandling handled = EnumHandling.PassThrough;
		List<WorldInteraction> interactions = new List<WorldInteraction>();
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			WorldInteraction[] wis = behavior.GetInteractionHelp(world, es, player, ref handled);
			if (wis != null)
			{
				interactions.AddRange(wis);
			}
			if (handled == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
		return interactions.ToArray();
	}

	/// <summary>
	/// Called by client when a new server pos arrived
	/// </summary>
	public virtual void OnReceivedServerPos(bool isTeleport)
	{
		EnumHandling handled = EnumHandling.PassThrough;
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			behavior.OnReceivedServerPos(isTeleport, ref handled);
			if (handled == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
		if (handled == EnumHandling.PassThrough && GetBehavior("entityinterpolation") == null)
		{
			Pos.SetFrom(ServerPos);
		}
	}

	/// <summary>
	/// Called when on the client side something called capi.Network.SendEntityPacket()
	/// </summary>
	/// <param name="player"></param>
	/// <param name="packetid"></param>
	/// <param name="data"></param>
	public virtual void OnReceivedClientPacket(IServerPlayer player, int packetid, byte[] data)
	{
		EnumHandling handled = EnumHandling.PassThrough;
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			behavior.OnReceivedClientPacket(player, packetid, data, ref handled);
			if (handled == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
	}

	/// <summary>
	/// Called when on the server side something called sapi.Network.SendEntityPacket()
	/// Packetid = 1 is used for teleporting
	/// Packetid = 2 is used for BehaviorHarvestable
	/// </summary>
	/// <param name="packetid"></param>
	/// <param name="data"></param>
	public virtual void OnReceivedServerPacket(int packetid, byte[] data)
	{
		if (packetid == 1)
		{
			Vec3d newPos = SerializerUtil.Deserialize<Vec3d>(data);
			if (Api is ICoreClientAPI ic && ic.World.Player.Entity.EntityId == EntityId)
			{
				Pos.SetPosWithDimension(newPos);
				((EntityPlayer)this).UpdatePartitioning();
			}
			ServerPos.SetPosWithDimension(newPos);
			World.BlockAccessor.MarkBlockDirty(newPos.AsBlockPos);
			return;
		}
		EnumHandling handled = EnumHandling.PassThrough;
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			behavior.OnReceivedServerPacket(packetid, data, ref handled);
			if (handled == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
	}

	public virtual void OnReceivedServerAnimations(int[] activeAnimations, int activeAnimationsCount, float[] activeAnimationSpeeds)
	{
		AnimManager.OnReceivedServerAnimations(activeAnimations, activeAnimationsCount, activeAnimationSpeeds);
	}

	/// <summary>
	/// Called by BehaviorCollectEntities of nearby entities. Should return the itemstack that should be collected. If the item stack was fully picked up, BehaviorCollectEntities will kill this entity
	/// </summary>
	/// <param name="byEntity"></param>
	/// <returns></returns>
	public virtual ItemStack OnCollected(Entity byEntity)
	{
		return null;
	}

	/// <summary>
	/// Called on the server when the entity was changed from active to inactive state or vice versa
	/// </summary>
	/// <param name="beforeState"></param>
	public virtual void OnStateChanged(EnumEntityState beforeState)
	{
		EnumHandling handled = EnumHandling.PassThrough;
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			behavior.OnStateChanged(beforeState, ref handled);
			if (handled == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
	}

	/// <summary>
	/// Helper method to set the CollisionBox
	/// </summary>
	/// <param name="length"></param>
	/// <param name="height"></param>
	public virtual void SetCollisionBox(float length, float height)
	{
		CollisionBox = new Cuboidf
		{
			X1 = (0f - length) / 2f,
			Z1 = (0f - length) / 2f,
			X2 = length / 2f,
			Z2 = length / 2f,
			Y2 = height
		};
		OriginCollisionBox = CollisionBox.Clone();
	}

	public virtual void SetSelectionBox(float length, float height)
	{
		SelectionBox = new Cuboidf
		{
			X1 = (0f - length) / 2f,
			Z1 = (0f - length) / 2f,
			X2 = length / 2f,
			Z2 = length / 2f,
			Y2 = height
		};
		OriginSelectionBox = SelectionBox.Clone();
	}

	/// <summary>
	/// Adds given behavior to the entities list of active behaviors
	/// </summary>
	/// <param name="behavior"></param>
	public virtual void AddBehavior(EntityBehavior behavior)
	{
		SidedProperties.Behaviors.Add(behavior);
	}

	/// <summary>
	/// Removes given behavior to the entities list of active behaviors. Does nothing if the behavior has already been removed
	/// </summary>
	/// <param name="behavior"></param>
	public virtual void RemoveBehavior(EntityBehavior behavior)
	{
		SidedProperties.Behaviors.Remove(behavior);
	}

	/// <summary>
	/// Returns true if the entity has given active behavior
	/// </summary>
	/// <param name="behaviorName"></param>
	/// <returns></returns>
	public virtual bool HasBehavior(string behaviorName)
	{
		for (int i = 0; i < SidedProperties.Behaviors.Count; i++)
		{
			if (SidedProperties.Behaviors[i].PropertyName().Equals(behaviorName))
			{
				return true;
			}
		}
		return false;
	}

	public virtual bool HasBehavior<T>() where T : EntityBehavior
	{
		for (int i = 0; i < SidedProperties.Behaviors.Count; i++)
		{
			if (SidedProperties.Behaviors[i] is T)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Returns the behavior instance for given entity. Returns null if it doesn't exist.
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	public virtual EntityBehavior GetBehavior(string name)
	{
		return SidedProperties.Behaviors.FirstOrDefault((EntityBehavior bh) => bh.PropertyName().Equals(name));
	}

	/// <summary>
	/// Returns the first behavior instance for given entity of given type. Returns null if it doesn't exist.
	/// </summary>
	/// <returns></returns>
	public virtual T GetBehavior<T>() where T : EntityBehavior
	{
		return (T)SidedProperties.Behaviors.FirstOrDefault((EntityBehavior bh) => bh is T);
	}

	/// <summary>
	/// Returns itself and any behaviors that implement the interface T as a List
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public virtual List<T> GetInterfaces<T>() where T : class
	{
		List<T> interfaces = new List<T>();
		if (this is T)
		{
			interfaces.Add(this as T);
		}
		for (int i = 0; i < SidedProperties.Behaviors.Count; i++)
		{
			if (SidedProperties.Behaviors[i] is T)
			{
				interfaces.Add(SidedProperties.Behaviors[i] as T);
			}
		}
		return interfaces;
	}

	/// <summary>
	/// Returns itself or the first behavior that implements the interface T
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public virtual T GetInterface<T>() where T : class
	{
		if (this is T)
		{
			return this as T;
		}
		return SidedProperties.Behaviors.FirstOrDefault((EntityBehavior bh) => bh is T) as T;
	}

	/// <summary>
	/// Returns true if given activity is running
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	public virtual bool IsActivityRunning(string key)
	{
		ActivityTimers.TryGetValue(key, out var val);
		return val > World.ElapsedMilliseconds;
	}

	/// <summary>
	/// Returns the remaining time on an activity in milliesconds
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	public virtual int RemainingActivityTime(string key)
	{
		ActivityTimers.TryGetValue(key, out var val);
		return (int)(val - World.ElapsedMilliseconds);
	}

	/// <summary>
	/// Starts an activity for a given duration
	/// </summary>
	/// <param name="key"></param>
	/// <param name="milliseconds"></param>
	public virtual void SetActivityRunning(string key, int milliseconds)
	{
		ActivityTimers[key] = World.ElapsedMilliseconds + milliseconds;
	}

	/// <summary>
	/// Updates the DebugAttributes tree
	/// </summary>
	public virtual void UpdateDebugAttributes()
	{
		if (World.Side == EnumAppSide.Client)
		{
			DebugAttributes.SetString("Entity Id", EntityId.ToString() ?? "");
			DebugAttributes.SetString("Yaw, Pitch", $"{Pos.Yaw * (180f / (float)Math.PI):0.##}, {Pos.Pitch * (180f / (float)Math.PI):0.##}");
			if (AnimManager != null)
			{
				UpdateAnimationDebugAttributes();
			}
		}
	}

	protected virtual void UpdateAnimationDebugAttributes()
	{
		string anims = "";
		int i = 0;
		foreach (string anim2 in AnimManager.ActiveAnimationsByAnimCode.Keys)
		{
			if (i++ > 0)
			{
				anims += ",";
			}
			anims += anim2;
		}
		i = 0;
		StringBuilder runninganims = new StringBuilder();
		if (AnimManager.Animator == null)
		{
			return;
		}
		RunningAnimation[] animations = AnimManager.Animator.Animations;
		foreach (RunningAnimation anim in animations)
		{
			if (anim.Running)
			{
				if (i++ > 0)
				{
					runninganims.Append(",");
				}
				runninganims.Append(anim.Animation.Code);
			}
		}
		DebugAttributes.SetString("Active Animations", (anims.Length > 0) ? anims : "-");
		DebugAttributes.SetString("Running Animations", (runninganims.Length > 0) ? runninganims.ToString() : "-");
	}

	/// <summary>
	/// In order to maintain legacy mod API compatibility of FromBytes(BinaryReader reader, bool isSync), we create an overload which server-side calling code will actually call, and store the remaps parameter in a field
	/// </summary>
	/// <param name="reader"></param>
	/// <param name="isSync"></param>
	/// <param name="serversideRemaps"></param>
	public virtual void FromBytes(BinaryReader reader, bool isSync, Dictionary<string, string> serversideRemaps)
	{
		codeRemaps = serversideRemaps;
		FromBytes(reader, isSync);
		codeRemaps = null;
	}

	/// <summary>
	/// Loads the entity from a stored byte array from the SaveGame
	/// </summary>
	/// <param name="reader"></param>
	/// <param name="isSync">True if this is a sync operation, not a chunk read operation</param>
	public virtual void FromBytes(BinaryReader reader, bool isSync)
	{
		string version = "";
		if (!isSync)
		{
			version = reader.ReadString().DeDuplicate();
		}
		EntityId = reader.ReadInt64();
		WatchedAttributes.FromBytes(reader);
		if (!WatchedAttributes.HasAttribute("extraInfoText"))
		{
			WatchedAttributes["extraInfoText"] = new TreeAttribute();
		}
		if (GameVersion.IsLowerVersionThan(version, "1.7.0") && this is EntityPlayer)
		{
			WatchedAttributes.GetTreeAttribute("health")?.SetFloat("basemaxhealth", 15f);
		}
		ServerPos.FromBytes(reader);
		GetHeadPositionFromWatchedAttributes();
		Pos.SetFrom(ServerPos);
		PositionBeforeFalling.X = reader.ReadDouble();
		PositionBeforeFalling.Y = reader.ReadDouble();
		PositionBeforeFalling.Z = reader.ReadDouble();
		string codeString = reader.ReadString().DeDuplicate();
		if (codeRemaps != null && codeRemaps.TryGetValue(codeString, out var remappedString))
		{
			codeString = remappedString;
		}
		Code = new AssetLocation(codeString);
		if (!isSync)
		{
			Attributes.FromBytes(reader);
		}
		if (isSync || GameVersion.IsAtLeastVersion(version, "1.8.0-pre.1"))
		{
			TreeAttribute tree = new TreeAttribute();
			tree.FromBytes(reader);
			AnimManager?.FromAttributes(tree, version);
			if (Properties?.Server?.Behaviors != null)
			{
				foreach (EntityBehavior behavior in Properties.Server.Behaviors)
				{
					behavior.FromBytes(isSync);
				}
			}
		}
		if (GameVersion.IsLowerVersionThan(version, "1.10-dev.2") && this is EntityPlayer)
		{
			WatchedAttributes.GetTreeAttribute("hunger")?.SetFloat("maxsaturation", 1500f);
		}
		Stats.FromTreeAttributes(WatchedAttributes);
	}

	/// <summary>
	/// Serializes the slots contents to be stored in the SaveGame
	/// </summary>
	/// <param name="writer"></param>
	/// <param name="forClient">True when being used to send an entity to the client</param>
	public virtual void ToBytes(BinaryWriter writer, bool forClient)
	{
		if (Properties?.Server?.Behaviors != null)
		{
			foreach (EntityBehavior behavior in Properties.Server.Behaviors)
			{
				behavior.ToBytes(forClient);
			}
		}
		if (!forClient)
		{
			writer.Write("1.20.7");
		}
		writer.Write(EntityId);
		SetHeadPositionToWatchedAttributes();
		WatchedAttributes.ToBytes(writer);
		ServerPos.ToBytes(writer);
		writer.Write(PositionBeforeFalling.X);
		writer.Write(PositionBeforeFalling.Y);
		writer.Write(PositionBeforeFalling.Z);
		if (Code == null)
		{
			World.Logger.Error("Entity.ToBytes(): entityType.Code is null?! Entity will probably be incorrectly saved to disk");
		}
		writer.Write(Code?.ToShortString());
		if (!forClient)
		{
			Attributes.ToBytes(writer);
		}
		TreeAttribute tree = new TreeAttribute();
		AnimManager?.ToAttributes(tree, forClient);
		Stats.ToTreeAttributes(WatchedAttributes, forClient);
		tree.ToBytes(writer);
	}

	/// <summary>
	/// Relevant only for entities with heads, implemented in EntityAgent.  Other sub-classes of Entity (if not EntityAgent) should similarly override this if the headYaw/headPitch are relevant to them
	/// </summary>
	protected virtual void SetHeadPositionToWatchedAttributes()
	{
	}

	/// <summary>
	/// Relevant only for entities with heads, implemented in EntityAgent.  Other sub-classes of Entity (if not EntityAgent) should similarly override this if the headYaw/headPitch are relevant to them
	/// </summary>
	protected virtual void GetHeadPositionFromWatchedAttributes()
	{
	}

	/// <summary>
	/// Revives the entity and heals for 9999.
	/// </summary>
	public virtual void Revive()
	{
		Alive = true;
		ReceiveDamage(new DamageSource
		{
			Source = EnumDamageSource.Revive,
			Type = EnumDamageType.Heal
		}, 9999f);
		AnimManager?.StopAnimation("die");
		IsOnFire = false;
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			behavior.OnEntityRevive();
		}
	}

	/// <summary>
	/// Makes the entity despawn. Entities only drop something on EnumDespawnReason.Death
	/// </summary>
	public virtual void Die(EnumDespawnReason reason = EnumDespawnReason.Death, DamageSource damageSourceForDeath = null)
	{
		if (!Alive)
		{
			return;
		}
		Alive = false;
		if (reason == EnumDespawnReason.Death)
		{
			Api.Event.TriggerEntityDeath(this, damageSourceForDeath);
			ItemStack[] drops = GetDrops(World, Pos.AsBlockPos, null);
			if (drops != null)
			{
				for (int i = 0; i < drops.Length; i++)
				{
					World.SpawnItemEntity(drops[i], SidedPos.XYZ.Add(0.0, 0.25, 0.0));
				}
			}
			AnimManager.ActiveAnimationsByAnimCode.Clear();
			AnimManager.StartAnimation("die");
			if (reason == EnumDespawnReason.Death && damageSourceForDeath != null && World.Side == EnumAppSide.Server)
			{
				WatchedAttributes.SetInt("deathReason", (int)damageSourceForDeath.Source);
				WatchedAttributes.SetInt("deathDamageType", (int)damageSourceForDeath.Type);
				Entity byEntity = damageSourceForDeath.GetCauseEntity();
				if (byEntity != null)
				{
					WatchedAttributes.SetString("deathByEntityLangCode", "prefixandcreature-" + byEntity.Code.Path.Replace("-", ""));
					WatchedAttributes.SetString("deathByEntity", byEntity.Code.ToString());
				}
				if (byEntity is EntityPlayer)
				{
					WatchedAttributes.SetString("deathByPlayer", (byEntity as EntityPlayer).Player?.PlayerName);
				}
			}
			foreach (EntityBehavior behavior in SidedProperties.Behaviors)
			{
				behavior.OnEntityDeath(damageSourceForDeath);
			}
		}
		DespawnReason = new EntityDespawnData
		{
			Reason = reason,
			DamageSourceForDeath = damageSourceForDeath
		};
	}

	/// <summary>
	/// Assumes that it is only called on the server
	/// </summary>
	/// <param name="type"></param>
	/// <param name="dualCallByPlayer"></param>
	/// <param name="randomizePitch"></param>
	/// <param name="range"></param>
	public virtual void PlayEntitySound(string type, IPlayer dualCallByPlayer = null, bool randomizePitch = true, float range = 24f)
	{
		if (Properties.ResolvedSounds != null && Properties.ResolvedSounds.TryGetValue(type, out var locations) && locations.Length != 0)
		{
			World.PlaySoundAt(locations[World.Rand.Next(locations.Length)], (float)SidedPos.X, (float)SidedPos.InternalY, (float)SidedPos.Z, dualCallByPlayer, randomizePitch, range);
		}
	}

	/// <summary>
	/// Should return true if this item can be picked up as an itemstack
	/// </summary>
	/// <param name="byEntity"></param>
	/// <returns></returns>
	public virtual bool CanCollect(Entity byEntity)
	{
		return false;
	}

	/// <summary>
	/// This method pings the Notify() method of all behaviors and ai tasks. Can be used to spread information to other creatures.
	/// </summary>
	/// <param name="key"></param>
	/// <param name="data"></param>
	public virtual void Notify(string key, object data)
	{
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			behavior.Notify(key, data);
		}
	}

	/// <summary>
	/// This method is called by the BlockSchematic class a moment before a schematic containing this entity is getting exported.
	/// Since a schematic can be placed anywhere in the world, this method has to make sure the entities position is set to a value
	/// relative of the schematic origin point defined by startPos
	/// Right after calling this method, the world edit system will call .ToBytes() to serialize the entity
	/// </summary>
	/// <param name="startPos"></param>
	public virtual void WillExport(BlockPos startPos)
	{
		ServerPos.X -= startPos.X;
		ServerPos.Y -= startPos.Y;
		ServerPos.Z -= startPos.Z;
		Pos.X -= startPos.X;
		Pos.Y -= startPos.Y;
		Pos.Z -= startPos.Z;
		PositionBeforeFalling.X -= startPos.X;
		PositionBeforeFalling.Y -= startPos.Y;
		PositionBeforeFalling.Z -= startPos.Z;
	}

	/// <summary>
	/// This method is called by the BlockSchematic class a moment after a schematic containing this entity has been exported.
	/// Since a schematic can be placed anywhere in the world, this method has to make sure the entities position is set to the correct
	/// position in relation to the target position of the schematic to be imported.
	/// </summary>
	/// <param name="startPos"></param>
	public virtual void DidImportOrExport(BlockPos startPos)
	{
		ServerPos.X += startPos.X;
		ServerPos.Y += startPos.Y;
		ServerPos.Z += startPos.Z;
		ServerPos.Dimension = startPos.dimension;
		Pos.X += startPos.X;
		Pos.Y += startPos.Y;
		Pos.Z += startPos.Z;
		Pos.Dimension = startPos.dimension;
		PositionBeforeFalling.X += startPos.X;
		PositionBeforeFalling.Y += startPos.Y;
		PositionBeforeFalling.Z += startPos.Z;
	}

	/// <summary>
	/// Called by the worldedit schematic exporter so that it can also export the mappings of items/blocks stored inside blockentities
	/// </summary>
	/// <param name="blockIdMapping"></param>
	/// <param name="itemIdMapping"></param>
	public virtual void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
	{
		foreach (EntityBehavior behavior in Properties.Server.Behaviors)
		{
			behavior.OnStoreCollectibleMappings(blockIdMapping, itemIdMapping);
		}
	}

	/// <summary>
	/// Called by the blockschematic loader so that you may fix any blockid/itemid mappings against the mapping of the savegame, if you store any collectibles in this blockentity.
	/// Note: Some vanilla blocks resolve randomized contents in this method.
	/// Hint: Use itemstack.FixMapping() to do the job for you.
	/// </summary>
	/// <param name="worldForNewMappings"></param>
	/// <param name="oldBlockIdMapping"></param>
	/// <param name="oldItemIdMapping"></param>
	/// <param name="schematicSeed">If you need some sort of randomness consistency accross an imported schematic, you can use this value</param>
	/// <param name="resolveImports">Turn it off to spawn structures as they are. For example, in this mode, instead of traders, their meta spawners will spawn</param>
	public virtual void OnLoadCollectibleMappings(IWorldAccessor worldForNewMappings, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
	{
		foreach (EntityBehavior behavior in Properties.Server.Behaviors)
		{
			behavior.OnLoadCollectibleMappings(worldForNewMappings, oldBlockIdMapping, oldItemIdMapping, resolveImports);
		}
	}

	/// <summary>
	/// Gets the name for this entity
	/// </summary>
	/// <returns></returns>
	public virtual string GetName()
	{
		string name = null;
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			string bhname = behavior.GetName(ref handling);
			switch (handling)
			{
			case EnumHandling.PreventSubsequent:
				return name;
			case EnumHandling.PreventDefault:
				name = bhname;
				break;
			}
		}
		if (name != null)
		{
			return name;
		}
		if (!Alive)
		{
			return Lang.GetMatching(Code.Domain + ":item-dead-creature-" + Code.Path);
		}
		return Lang.GetMatching(Code.Domain + ":item-creature-" + Code.Path);
	}

	/// <summary>
	/// gets the info text for the entity.
	/// </summary>
	/// <returns></returns>
	public virtual string GetInfoText()
	{
		StringBuilder infotext = new StringBuilder();
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			behavior.GetInfoText(infotext);
		}
		int generation = WatchedAttributes.GetInt("generation");
		if (generation > 0)
		{
			infotext.AppendLine(Lang.Get("Generation: {0}", generation));
		}
		if (!Alive && WatchedAttributes.HasAttribute("deathByPlayer"))
		{
			infotext.AppendLine(Lang.Get("Killed by Player: {0}", WatchedAttributes.GetString("deathByPlayer")));
		}
		if (WatchedAttributes.HasAttribute("extraInfoText"))
		{
			foreach (KeyValuePair<string, IAttribute> item in WatchedAttributes.GetTreeAttribute("extraInfoText").SortedCopy())
			{
				infotext.AppendLine(item.Value.ToString());
			}
		}
		if (Api is ICoreClientAPI capi && capi.Settings.Bool["extendedDebugInfo"])
		{
			infotext.AppendLine("<font color=\"#bbbbbb\">Id:" + EntityId + "</font>");
			infotext.AppendLine(string.Concat("<font color=\"#bbbbbb\">Code: ", Code, "</font>"));
		}
		return infotext.ToString();
	}

	/// <summary>
	/// Starts the animation for the entity.
	/// </summary>
	/// <param name="code"></param>
	public virtual void StartAnimation(string code)
	{
		AnimManager.StartAnimation(code);
	}

	/// <summary>
	/// stops the animation for the entity.
	/// </summary>
	/// <param name="code"></param>
	public virtual void StopAnimation(string code)
	{
		AnimManager.StopAnimation(code);
	}

	/// <summary>
	/// To test for player-&gt;entity selection.
	/// </summary>
	/// <param name="ray"></param>
	/// <param name="interesectionTester">Is already preloaded with the ray</param>
	/// <param name="intersectionDistance"></param>
	/// <returns></returns>
	public virtual bool IntersectsRay(Ray ray, AABBIntersectionTest interesectionTester, out double intersectionDistance, ref int selectionBoxIndex)
	{
		if (trickleDownRayIntersects)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			bool preventDefault = false;
			bool intersects = false;
			intersectionDistance = 0.0;
			foreach (EntityBehavior behavior in SidedProperties.Behaviors)
			{
				intersects |= behavior.IntersectsRay(ray, interesectionTester, out intersectionDistance, ref selectionBoxIndex, ref handled);
				preventDefault = preventDefault || handled == EnumHandling.PreventDefault;
				if (handled == EnumHandling.PreventSubsequent)
				{
					return intersects;
				}
			}
			if (preventDefault)
			{
				return intersects;
			}
		}
		if (interesectionTester.RayIntersectsWithCuboid(SelectionBox, SidedPos.X, SidedPos.InternalY, SidedPos.Z))
		{
			intersectionDistance = Pos.SquareDistanceTo(ray.origin);
			return true;
		}
		intersectionDistance = 0.0;
		return false;
	}

	public virtual double GetTouchDistance()
	{
		Cuboidf selectionBox = SelectionBox;
		float dist = ((selectionBox != null) ? (selectionBox.XSize / 2f) : 0.25f);
		foreach (EntityBehavior behavior in SidedProperties.Behaviors)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			float d = behavior.GetTouchDistance(ref handling);
			if (handling != 0)
			{
				dist = d;
			}
			if (handling == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
		return dist;
	}
}
