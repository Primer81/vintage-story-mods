#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

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

//
// Summary:
//     The basic class for all entities in the game
public abstract class Entity : RegistryObject
{
    public static WaterSplashParticles SplashParticleProps;

    public static AdvancedParticleProperties[] FireParticleProps;

    public static FloatingSedimentParticles FloatingSedimentParticles;

    public static AirBubbleParticles AirBubbleParticleProps;

    public static SimpleParticleProperties bioLumiParticles;

    public static NormalizedSimplexNoise bioLumiNoise;

    //
    // Summary:
    //     Color used when the entity is being attacked
    protected int HurtColor = ColorUtil.ToRgba(255, 255, 100, 100);

    //
    // Summary:
    //     World where the entity is spawned in. Available on the game client and server.
    public IWorldAccessor World;

    //
    // Summary:
    //     The api, if you need it. Available on the game client and server.
    public ICoreAPI Api;

    //
    // Summary:
    //     The vanilla physics systems will call this method if a physics behavior was assigned
    //     to it. The game client for example requires this to be called for the current
    //     player to properly render the player. Available on the game client and server.
    public PhysicsTickDelegate PhysicsUpdateWatcher;

    //
    // Summary:
    //     An uptime value running activities. Available on the game client and server.
    //     Not synchronized.
    public Dictionary<string, long> ActivityTimers = new Dictionary<string, long>();

    //
    // Summary:
    //     Client position
    public EntityPos Pos = new EntityPos();

    //
    // Summary:
    //     Server simulated position. May not exactly match the client positon
    public EntityPos ServerPos = new EntityPos();

    //
    // Summary:
    //     Server simulated position copy. Needed by Entities server system to send pos
    //     updatess only if ServerPos differs noticably from PreviousServerPos
    public EntityPos PreviousServerPos = new EntityPos();

    //
    // Summary:
    //     The position where the entity last had contact with the ground. Set by the game
    //     client and server.
    public Vec3d PositionBeforeFalling = new Vec3d();

    public long InChunkIndex3d;

    //
    // Summary:
    //     The entities collision box. Offseted by the animation system when necessary.
    //     Set by the game client and server.
    public Cuboidf CollisionBox;

    //
    // Summary:
    //     The entities collision box. Not Offseted. Set by the game client and server.
    public Cuboidf OriginCollisionBox;

    //
    // Summary:
    //     The entities selection box. Offseted by the animation system when necessary.
    //     Set by the game client and server.
    public Cuboidf SelectionBox;

    //
    // Summary:
    //     The entities selection box. Not Offseted. Set by the game client and server.
    public Cuboidf OriginSelectionBox;

    //
    // Summary:
    //     Used by the teleporter block
    public bool Teleporting;

    //
    // Summary:
    //     A unique identifier for this entity. Set by the game client and server.
    public long EntityId;

    //
    // Summary:
    //     The range in blocks the entity has to be to a client to do physics and AI. When
    //     outside range, then Vintagestory.API.Common.Entities.Entity.State will be set
    //     to inactive
    public int SimulationRange;

    //
    // Summary:
    //     The face the entity is climbing on. Null if the entity is not climbing. Set by
    //     the game client and server.
    public BlockFacing ClimbingOnFace;

    public BlockFacing ClimbingIntoFace;

    //
    // Summary:
    //     Set by the game client and server.
    public Cuboidf ClimbingOnCollBox;

    //
    // Summary:
    //     True if this entity is in touch with the ground. Set by the game client and server.
    public bool OnGround;

    //
    // Summary:
    //     True if the bottom of the collisionbox is inside a liquid. Set by the game client
    //     and server.
    public bool FeetInLiquid;

    protected bool resetLightHsv;

    public bool InLava;

    public long InLavaBeginTotalMs;

    public long OnFireBeginTotalMs;

    //
    // Summary:
    //     True if the collisionbox is 2/3rds submerged in liquid. Set by the game client
    //     and server.
    public bool Swimming;

    //
    // Summary:
    //     True if the entity is in touch with something solid on the vertical axis. Set
    //     by the game client and server.
    public bool CollidedVertically;

    //
    // Summary:
    //     True if the entity is in touch with something solid on both horizontal axes.
    //     Set by the game client and server.
    public bool CollidedHorizontally;

    //
    // Summary:
    //     The current entity state. NOT stored in WatchedAttributes in from/tobytes when
    //     sending to client as always set to Active on client-side Initialize(). Server-side
    //     if saved it would likely initially be Despawned when an entity is first loaded
    //     from the save due to entities being despawned during the UnloadChunks process,
    //     so let's make it always Despawned for consistent behavior (it will be set to
    //     Active/Inactive during Initialize() anyhow)
    public EnumEntityState State = EnumEntityState.Despawned;

    public EntityDespawnData DespawnReason;

    //
    // Summary:
    //     Permanently stored entity attributes that are sent to client everytime they have
    //     been changed
    public SyncedTreeAttribute WatchedAttributes = new SyncedTreeAttribute();

    //
    // Summary:
    //     If entity debug mode is on, this info will be transitted to client and displayed
    //     above the entities head
    public SyncedTreeAttribute DebugAttributes = new SyncedTreeAttribute();

    //
    // Summary:
    //     Permanently stored entity attributes that are only client or only server side
    public SyncedTreeAttribute Attributes = new SyncedTreeAttribute();

    //
    // Summary:
    //     Set by the client renderer when the entity was rendered last frame
    public bool IsRendered;

    //
    // Summary:
    //     Set by the client renderer when the entity shadow was rendered last frame
    public bool IsShadowRendered;

    public EntityStats Stats;

    protected float fireDamageAccum;

    public double touchDistanceSq;

    public Vec3d ownPosRepulse = new Vec3d();

    public bool hasRepulseBehavior;

    public bool customRepulseBehavior;

    //
    // Summary:
    //     Used by PhysicsManager. Added here to increase performance 0 = not tracked, 1
    //     = lowResTracked, 2 = fullyTracked
    public byte IsTracked;

    //
    // Summary:
    //     Used by the PhysicsManager to tell connected clients that the next entity position
    //     packet should not have its position change get interpolated. Gets set to false
    //     after the packet was sent
    public bool IsTeleport;

    //
    // Summary:
    //     If true, will call EntityBehavior.IntersectsRay. Default off to increase performance.
    public bool trickleDownRayIntersects;

    //
    // Summary:
    //     If true, will fully simulate animations on the server so one has access to the
    //     positions of all attachment points. If false, only root level attachment points
    //     will be available server side
    public bool requirePosesOnServer;

    //
    // Summary:
    //     Used for efficiency in multi-player servers, to avoid regenerating the packet
    //     again for each connected client
    public object packet;

    //
    // Summary:
    //     Used only when deserialising an entity, otherwise null
    private Dictionary<string, string> codeRemaps;

    protected bool alive = true;

    public float minHorRangeToClient;

    protected bool shapeFresh;

    //
    // Summary:
    //     Used by AItasks for perfomance. When searching for nearby entities we distinguish
    //     between (A) Creatures and (B) Inanimate entitie. Inanimate entities are items
    //     on the ground, projectiles, armor stands, rafts, falling blocks etc
    //     Note 1: Dead creatures / corpses count as a Creature. EntityPlayer is a Creature
    //     of course.
    //     Note 2: Straw Dummy we count as a Creature, because weapons can target it and
    //     bees can attack it. In contrast, Armor Stand we count as Inanimate, because nothing
    //     should ever attack or target it.
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

    //
    // Summary:
    //     Server simulated animations. Only takes care of stopping animations once they're
    //     done Set and Called by the Entities ServerSystem
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

    //
    // Summary:
    //     Should return true when this entity should be interactable by a player or other
    //     entities
    public virtual bool IsInteractable => true;

    //
    // Summary:
    //     Used for passive physics simulation, together with the MaterialDensity to check
    //     how deep in the water the entity should float
    public virtual double SwimmingOffsetY => (double)SelectionBox.Y1 + (double)SelectionBox.Y2 * 0.66;

    //
    // Summary:
    //     CollidedVertically || CollidedHorizontally
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

    //
    // Summary:
    //     ServerPos on server, Pos on client
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

    //
    // Summary:
    //     The height of the eyes for the given entity.
    public virtual Vec3d LocalEyePos { get; set; } = new Vec3d();


    //
    // Summary:
    //     If gravity should applied to this entity
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

    //
    // Summary:
    //     Determines on whether an entity floats on liquids or not and how strongly items
    //     get pushed by water. Water has a density of 1000. A density below 1000 means
    //     the entity floats on top of water if has a physics simulation behavior attached
    //     to it.
    public virtual float MaterialDensity => 3000f;

    //
    // Summary:
    //     If set, the entity will emit dynamic light
    public virtual byte[] LightHsv { get; set; }

    //
    // Summary:
    //     If the entity should despawn next server tick. By default returns !Alive for
    //     non-creatures and creatures that don't have a Decay behavior
    public virtual bool ShouldDespawn => !Alive;

    //
    // Summary:
    //     Players and whatever the player rides on will be stored seperatly
    public virtual bool StoreWithChunk => true;

    public virtual bool AllowOutsideLoadedRange => false;

    //
    // Summary:
    //     Whether this entity should always stay in Active model, regardless on how far
    //     away other player are
    public virtual bool AlwaysActive { get; set; }

    //
    // Summary:
    //     True if the entity is in state active or inactive, or generally not dead (for
    //     non-living entities, 'dead' means ready to despawn)
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

    //
    // Summary:
    //     Used by some renderers to apply an overal color tint on the entity
    public int RenderColor { get; set; } = -1;


    //
    // Summary:
    //     A small offset used to prevent players from clipping through the blocks above
    //     ladders: relevant if the entity's collision box is sometimes adjusted by the
    //     game code
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

    //
    // Summary:
    //     Creates a new instance of an entity
    public Entity()
    {
        SimulationRange = GlobalConstants.DefaultSimulationRange;
        AnimManager = new AnimationManager();
        Stats = new EntityStats(this);
        WatchedAttributes.SetAttribute("animations", new TreeAttribute());
        WatchedAttributes.SetAttribute("extraInfoText", new TreeAttribute());
    }

    //
    // Summary:
    //     Creates a minimally populated entity with configurable tracking range, no Stats,
    //     no AnimManager and no animations attribute. Currently used by EntityItem.
    //
    // Parameters:
    //   trackingRange:
    protected Entity(int trackingRange)
    {
        SimulationRange = trackingRange;
        WatchedAttributes.SetAttribute("extraInfoText", new TreeAttribute());
    }

    //
    // Summary:
    //     Called when the entity got hurt. On the client side, dmgSource is null
    //
    // Parameters:
    //   dmgSource:
    //
    //   damage:
    public virtual void OnHurt(DamageSource dmgSource, float damage)
    {
    }

    //
    // Summary:
    //     Called when this entity got created or loaded
    //
    // Parameters:
    //   properties:
    //
    //   api:
    //
    //   InChunkIndex3d:
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
        IPlayer[] allOnlinePlayers = World.AllOnlinePlayers;
        for (int i = 0; i < allOnlinePlayers.Length; i++)
        {
            EntityPlayer entity = allOnlinePlayers[i].Entity;
            if (entity != null && Pos.InRangeOf(entity.Pos, SimulationRange * SimulationRange))
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
            Vec2f vec2f = Properties.SelectionBoxSize ?? Properties.CollisionBoxSize;
            SetSelectionBox(vec2f.X, vec2f.Y);
        }
        else
        {
            SetCollisionBox(Properties.DeadCollisionBoxSize.X, Properties.DeadCollisionBoxSize.Y);
            Vec2f vec2f2 = Properties.DeadSelectionBoxSize ?? Properties.DeadCollisionBoxSize;
            SetSelectionBox(vec2f2.X, vec2f2.Y);
        }

        double num = Math.Max(0.001f, SelectionBox.XSize / 2f);
        touchDistanceSq = num * num;
        foreach (EntityBehavior behavior in SidedProperties.Behaviors)
        {
            behavior.UpdateColSelBoxes();
        }
    }

    protected void updateOnFire()
    {
        bool isOnFire = IsOnFire;
        if (isOnFire)
        {
            OnFireBeginTotalMs = World.ElapsedMilliseconds;
        }

        if (isOnFire && LightHsv == null)
        {
            LightHsv = new byte[3] { 5, 7, 10 };
            resetLightHsv = true;
        }

        if (!isOnFire && resetLightHsv)
        {
            LightHsv = null;
        }
    }

    //
    // Summary:
    //     Called when something tries to given an itemstack to this entity
    //
    // Parameters:
    //   itemstack:
    public virtual bool TryGiveItemStack(ItemStack itemstack)
    {
        EnumHandling handling = EnumHandling.PassThrough;
        bool flag = false;
        foreach (EntityBehavior behavior in SidedProperties.Behaviors)
        {
            flag |= behavior.TryGiveItemStack(itemstack, ref handling);
            if (handling == EnumHandling.PreventSubsequent)
            {
                return flag;
            }
        }

        return flag;
    }

    //
    // Summary:
    //     Is called before the entity is killed, should return what items this entity should
    //     drop. Return null or empty array for no drops.
    //
    // Parameters:
    //   world:
    //
    //   pos:
    //
    //   byPlayer:
    public virtual ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer)
    {
        EnumHandling handling = EnumHandling.PassThrough;
        ItemStack[] result = null;
        foreach (EntityBehavior behavior in SidedProperties.Behaviors)
        {
            result = behavior.GetDrops(world, pos, byPlayer, ref handling);
            if (handling == EnumHandling.PreventSubsequent)
            {
                return result;
            }
        }

        if (handling == EnumHandling.PreventDefault)
        {
            return result;
        }

        if (Properties.Drops == null)
        {
            return null;
        }

        List<ItemStack> list = new List<ItemStack>();
        float num = 1f;
        JsonObject attributes = Properties.Attributes;
        if ((attributes == null || !attributes["isMechanical"].AsBool()) && byPlayer?.Entity != null)
        {
            num = 1f + byPlayer.Entity.Stats.GetBlended("animalLootDropRate");
        }

        for (int i = 0; i < Properties.Drops.Length; i++)
        {
            BlockDropItemStack blockDropItemStack = Properties.Drops[i];
            float num2 = 1f;
            if (blockDropItemStack.DropModbyStat != null && byPlayer?.Entity != null)
            {
                num2 = byPlayer.Entity.Stats.GetBlended(blockDropItemStack.DropModbyStat);
            }

            ItemStack itemStack = blockDropItemStack.GetNextItemStack(num * num2);
            if (itemStack != null)
            {
                if (itemStack.Collectible is IResolvableCollectible resolvableCollectible)
                {
                    DummySlot dummySlot = new DummySlot(itemStack);
                    resolvableCollectible.Resolve(dummySlot, world);
                    itemStack = dummySlot.Itemstack;
                }

                list.Add(itemStack);
                if (blockDropItemStack.LastDrop)
                {
                    break;
                }
            }
        }

        return list.ToArray();
    }

    //
    // Summary:
    //     Teleports the entity to given position. Actual teleport is delayed until target
    //     chunk is loaded.
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    //
    //   onTeleported:
    public virtual void TeleportToDouble(double x, double y, double z, Action onTeleported = null)
    {
        Teleporting = true;
        if (World.Api is ICoreServerAPI coreServerAPI)
        {
            coreServerAPI.WorldManager.LoadChunkColumnPriority((int)x / 32, (int)z / 32, new ChunkLoadOptions
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

    //
    // Summary:
    //     Teleports the entity to given position
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    public virtual void TeleportTo(int x, int y, int z)
    {
        TeleportToDouble(x, y, z);
    }

    //
    // Summary:
    //     Teleports the entity to given position
    //
    // Parameters:
    //   position:
    public virtual void TeleportTo(Vec3d position)
    {
        TeleportToDouble(position.X, position.Y, position.Z);
    }

    //
    // Summary:
    //     Teleports the entity to given position
    //
    // Parameters:
    //   position:
    public virtual void TeleportTo(BlockPos position)
    {
        TeleportToDouble(position.X, position.Y, position.Z);
    }

    //
    // Summary:
    //     Teleports the entity to given position
    //
    // Parameters:
    //   position:
    //
    //   onTeleported:
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

    //
    // Summary:
    //     Called when the entity should be receiving damage from given source
    //
    // Parameters:
    //   damageSource:
    //
    //   damage:
    //
    // Returns:
    //     True if the entity actually received damage
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
                bool flag = false;
                if (damageSource.GetAttackAngle(Pos.XYZ, out var _, out var attackPitch))
                {
                    flag = Math.Abs(attackPitch) > 1.3962633609771729 || Math.Abs(attackPitch) < 0.1745329201221466;
                }

                Vec3d vec3d = (SidedPos.XYZ - damageSource.GetSourcePosition()).Normalize();
                if (flag)
                {
                    vec3d.Y = 0.05000000074505806;
                    vec3d.Normalize();
                }
                else
                {
                    vec3d.Y = 0.699999988079071;
                }

                vec3d.Y /= damageSource.YDirKnockbackDiv;
                float num = damageSource.KnockbackStrength * GameMath.Clamp((1f - Properties.KnockbackResistance) / 10f, 0f, 1f);
                WatchedAttributes.SetFloat("onHurtDir", (float)Math.Atan2(vec3d.X, vec3d.Z));
                WatchedAttributes.SetDouble("kbdirX", vec3d.X * (double)num);
                WatchedAttributes.SetDouble("kbdirY", vec3d.Y * (double)num);
                WatchedAttributes.SetDouble("kbdirZ", vec3d.Z * (double)num);
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

    //
    // Summary:
    //     Should return true if the entity can get damaged by given damageSource. Is called
    //     by ReceiveDamage.
    //
    // Parameters:
    //   damageSource:
    //
    //   damage:
    public virtual bool ShouldReceiveDamage(DamageSource damageSource, float damage)
    {
        return true;
    }

    //
    // Summary:
    //     Called every 1/75 second
    //
    // Parameters:
    //   dt:
    public virtual void OnGameTick(float dt)
    {
        if (World.EntityDebugMode)
        {
            UpdateDebugAttributes();
            DebugAttributes.MarkAllDirty();
        }

        if (World.Side == EnumAppSide.Client)
        {
            int num = RemainingActivityTime("invulnerable");
            if (num >= 0)
            {
                RenderColor = ColorUtil.ColorOverlay(HurtColor, -1, 1f - (float)num / 500f);
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
                foreach (EntityBehavior behavior2 in SidedProperties.Behaviors)
                {
                    behavior2.OnGameTick(dt);
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
                CompositeShape shape = Properties.Client.Shape;
                Shape entityShape = Properties.Client.LoadedShapeForEntity;
                if (entityShape != null)
                {
                    OnTesselation(ref entityShape, shape.Base.ToString());
                    OnTesselated();
                }
            }

            if (World.FrameProfiler.Enabled)
            {
                World.FrameProfiler.Enter("behaviors");
                foreach (EntityBehavior behavior3 in SidedProperties.Behaviors)
                {
                    behavior3.OnGameTick(dt);
                    World.FrameProfiler.Mark(behavior3.ProfilerName);
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
            Block block = World.BlockAccessor.GetBlock(Pos.AsBlockPos, 2);
            if (((block.IsLiquid() && block.LiquidCode != "lava") || World.ElapsedMilliseconds - OnFireBeginTotalMs > 12000) && !InLava)
            {
                IsOnFire = false;
            }
            else
            {
                if (World.Side == EnumAppSide.Client)
                {
                    int num2 = Math.Min(FireParticleProps.Length - 1, Api.World.Rand.Next(FireParticleProps.Length + 1));
                    AdvancedParticleProperties advancedParticleProperties = FireParticleProps[num2];
                    advancedParticleProperties.basePos.Set(Pos.X, Pos.Y + (double)(SelectionBox.YSize / 2f), Pos.Z);
                    advancedParticleProperties.PosOffset[0].var = SelectionBox.XSize / 2f;
                    advancedParticleProperties.PosOffset[1].var = SelectionBox.YSize / 2f;
                    advancedParticleProperties.PosOffset[2].var = SelectionBox.ZSize / 2f;
                    advancedParticleProperties.Velocity[0].avg = (float)Pos.Motion.X * 10f;
                    advancedParticleProperties.Velocity[1].avg = (float)Pos.Motion.Y * 5f;
                    advancedParticleProperties.Velocity[2].avg = (float)Pos.Motion.Z * 10f;
                    advancedParticleProperties.Quantity.avg = GameMath.Sqrt(advancedParticleProperties.PosOffset[0].var + advancedParticleProperties.PosOffset[1].var + advancedParticleProperties.PosOffset[2].var) * num2 switch
                    {
                        1 => 3f,
                        0 => 0.5f,
                        _ => 1.25f,
                    };
                    Api.World.SpawnParticles(advancedParticleProperties);
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
        float quantity = GameMath.Clamp(SelectionBox.XSize * SelectionBox.YSize * SelectionBox.ZSize * 150f, 10f, 150f);
        Api.World.SpawnParticles(quantity, ColorUtil.ColorFromRgba(20, 20, 20, 255), new Vec3d(ServerPos.X + (double)SelectionBox.X1, ServerPos.Y + (double)SelectionBox.Y1, ServerPos.Z + (double)SelectionBox.Z1), new Vec3d(ServerPos.X + (double)SelectionBox.X2, ServerPos.Y + (double)SelectionBox.Y2, ServerPos.Z + (double)SelectionBox.Z2), new Vec3f(-1f, -1f, -1f), new Vec3f(2f, 2f, 2f), 2f, 1f, 1f, EnumParticleModel.Cube);
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

        ITexPositionSource result = null;
        List<EntityBehavior> list = Properties.Client?.Behaviors;
        EnumHandling handling = EnumHandling.PassThrough;
        if (list != null)
        {
            foreach (EntityBehavior item in list)
            {
                result = item.GetTextureSource(ref handling);
                if (handling == EnumHandling.PreventSubsequent)
                {
                    return result;
                }
            }
        }

        if (handling == EnumHandling.PreventDefault)
        {
            return result;
        }

        int @int = WatchedAttributes.GetInt("textureIndex");
        return (Api as ICoreClientAPI).Tesselator.GetTextureSource(this, null, @int);
    }

    public virtual void MarkShapeModified()
    {
        shapeFresh = false;
    }

    //
    // Summary:
    //     Called by EntityShapeRenderer.cs before tesselating the entity shape
    //
    // Parameters:
    //   entityShape:
    //
    //   shapePathForLogging:
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
        CompositeShape shape = Properties.Client.Shape;
        if (shape?.Overlays != null && shape.Overlays.Length != 0)
        {
            shapeIsCloned = true;
            entityShape = entityShape.Clone();
            IDictionary<string, CompositeTexture> textures = Properties.Client.Textures;
            CompositeShape[] overlays = shape.Overlays;
            foreach (CompositeShape compositeShape in overlays)
            {
                Shape shape2 = Api.Assets.TryGet(compositeShape.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json"))?.ToObject<Shape>();
                if (shape2 == null)
                {
                    Api.Logger.Error("Entity {0} defines a shape overlay {1}, but no such file found. Will ignore.", Code, compositeShape.Base);
                    continue;
                }

                string texturePrefixCode = null;
                JsonObject attributes = Properties.Attributes;
                if (attributes != null && attributes["wearableTexturePrefixCode"].Exists)
                {
                    texturePrefixCode = Properties.Attributes["wearableTexturePrefixCode"].AsString();
                }

                entityShape.StepParentShape(shape2, compositeShape.Base.ToShortString(), shapePathForLogging, Api.Logger, delegate (string texcode, AssetLocation tloc)
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

        string[] willDeleteElements = null;
        JsonObject attributes2 = Properties.Attributes;
        if (attributes2 != null && attributes2["disableElements"].Exists)
        {
            willDeleteElements = Properties.Attributes["disableElements"].AsArray<string>();
        }

        List<EntityBehavior> list = ((World.Side != EnumAppSide.Server) ? Properties.Client?.Behaviors : Properties.Server?.Behaviors);
        EnumHandling enumHandling = EnumHandling.PassThrough;
        if (list != null)
        {
            foreach (EntityBehavior item in list)
            {
                item.OnTesselation(ref entityShape, shapePathForLogging, ref shapeIsCloned, ref willDeleteElements);
                if (enumHandling == EnumHandling.PreventSubsequent)
                {
                    break;
                }
            }
        }

        if (willDeleteElements != null && willDeleteElements.Length != 0)
        {
            if (!shapeIsCloned)
            {
                Shape shape3 = entityShape.Clone();
                entityShape = shape3;
                shapeIsCloned = true;
            }

            entityShape.RemoveElements(willDeleteElements);
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
        List<EntityBehavior> list = ((World.Side != EnumAppSide.Server) ? Properties.Client?.Behaviors : Properties.Server?.Behaviors);
        if (list == null)
        {
            return;
        }

        foreach (EntityBehavior item in list)
        {
            item.OnTesselated();
        }
    }

    //
    // Summary:
    //     Called when the entity collided vertically
    //
    // Parameters:
    //   motionY:
    public virtual void OnFallToGround(double motionY)
    {
        foreach (EntityBehavior behavior in SidedProperties.Behaviors)
        {
            behavior.OnFallToGround(PositionBeforeFalling, motionY);
        }
    }

    //
    // Summary:
    //     Called when the entity collided with something solid and Vintagestory.API.Common.Entities.Entity.Collided
    //     was false before
    public virtual void OnCollided()
    {
    }

    //
    // Summary:
    //     Called when the entity got in touch with a liquid
    public virtual void OnCollideWithLiquid()
    {
        if (World.Side == EnumAppSide.Server)
        {
            return;
        }

        EntityPos sidedPos = SidedPos;
        float num = (float)Math.Abs(PositionBeforeFalling.Y - sidedPos.Y);
        double num2 = SelectionBox.XSize;
        double num3 = SelectionBox.YSize;
        double num4 = (double)(2f * GameMath.Sqrt(num2 * num3)) + sidedPos.Motion.Length() * 10.0;
        if (!(num4 < 0.4000000059604645) && !(num < 0.25f))
        {
            string domainAndPath = (new string[3] { "sounds/environment/smallsplash", "sounds/environment/mediumsplash", "sounds/environment/largesplash" })[(int)GameMath.Clamp(num4 / 1.6, 0.0, 2.0)];
            num4 = Math.Min(10.0, num4);
            float num5 = GameMath.Sqrt(num2 * num3);
            World.PlaySoundAt(new AssetLocation(domainAndPath), (float)sidedPos.X, (float)sidedPos.InternalY, (float)sidedPos.Z);
            BlockPos asBlockPos = sidedPos.AsBlockPos;
            Vec3d pos = new Vec3d(Pos.X, (double)asBlockPos.InternalY + 1.02, Pos.Z);
            World.SpawnCubeParticles(asBlockPos, pos, SelectionBox.XSize, (int)((double)(num5 * 8f) * num4), 0.75f);
            World.SpawnCubeParticles(asBlockPos, pos, SelectionBox.XSize, (int)((double)(num5 * 8f) * num4), 0.25f);
            if (num4 >= 2.0)
            {
                SplashParticleProps.BasePos.Set(sidedPos.X - num2 / 2.0, sidedPos.Y - 0.75, sidedPos.Z - num2 / 2.0);
                SplashParticleProps.AddPos.Set(num2, 0.75, num2);
                SplashParticleProps.AddVelocity.Set((float)GameMath.Clamp(sidedPos.Motion.X * 30.0, -2.0, 2.0), 1f, (float)GameMath.Clamp(sidedPos.Motion.Z * 30.0, -2.0, 2.0));
                SplashParticleProps.QuantityMul = (float)(num4 - 1.0) * num5;
                World.SpawnParticles(SplashParticleProps);
            }

            SpawnWaterMovementParticles((float)Math.Min(0.25, num4 / 10.0), 0.0, -0.5);
        }
    }

    protected virtual void SpawnWaterMovementParticles(float quantityMul, double offx = 0.0, double offy = 0.0, double offz = 0.0)
    {
        if (World.Side == EnumAppSide.Server)
        {
            return;
        }

        ClimateCondition selfClimateCond = (Api as ICoreClientAPI).World.Player.Entity.selfClimateCond;
        if (selfClimateCond == null)
        {
            return;
        }

        float num = Math.Max(0f, (28f - selfClimateCond.Temperature) / 6f) + Math.Max(0f, (0.8f - selfClimateCond.Rainfall) * 3f);
        double num2 = bioLumiNoise.Noise(SidedPos.X / 300.0, SidedPos.Z / 300.0) * 2.0 - 1.0 - (double)num;
        if (!(num2 < 0.0))
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

            bioLumiParticles.MinQuantity = Math.Min(200f, 100f * quantityMul * (float)num2);
            bioLumiParticles.MinVelocity.Set(-0.2f + 2f * (float)Pos.Motion.X, -0.2f + 2f * (float)Pos.Motion.Y, -0.2f + 2f * (float)Pos.Motion.Z);
            bioLumiParticles.AddVelocity.Set(0.4f + 2f * (float)Pos.Motion.X, 0.4f + 2f * (float)Pos.Motion.Y, 0.4f + 2f * (float)Pos.Motion.Z);
            World.SpawnParticles(bioLumiParticles);
        }
    }

    //
    // Summary:
    //     Called when after the got loaded from the savegame (not called during spawn)
    public virtual void OnEntityLoaded()
    {
        foreach (EntityBehavior behavior in SidedProperties.Behaviors)
        {
            behavior.OnEntityLoaded();
        }

        Properties.Client.Renderer?.OnEntityLoaded();
        MarkShapeModified();
    }

    //
    // Summary:
    //     Called when the entity spawns (not called when loaded from the savegame).
    public virtual void OnEntitySpawn()
    {
        foreach (EntityBehavior behavior in SidedProperties.Behaviors)
        {
            behavior.OnEntitySpawn();
        }

        Properties.Client.Renderer?.OnEntityLoaded();
        MarkShapeModified();
    }

    //
    // Summary:
    //     Called when the entity despawns
    //
    // Parameters:
    //   despawn:
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

    //
    // Summary:
    //     Called when the entity has left a liquid
    public virtual void OnExitedLiquid()
    {
    }

    //
    // Summary:
    //     Called when an entity has interacted with this entity
    //
    // Parameters:
    //   byEntity:
    //
    //   itemslot:
    //     If being interacted with a block/item, this should be the slot the item is being
    //     held in
    //
    //   hitPosition:
    //     Relative position on the entites hitbox where the entity interacted at
    //
    //   mode:
    //     0 = attack, 1 = interact
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

    //
    // Summary:
    //     Called when a player looks at the entity with interaction help enabled
    //
    // Parameters:
    //   world:
    //
    //   es:
    //
    //   player:
    public virtual WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player)
    {
        EnumHandling handled = EnumHandling.PassThrough;
        List<WorldInteraction> list = new List<WorldInteraction>();
        foreach (EntityBehavior behavior in SidedProperties.Behaviors)
        {
            WorldInteraction[] interactionHelp = behavior.GetInteractionHelp(world, es, player, ref handled);
            if (interactionHelp != null)
            {
                list.AddRange(interactionHelp);
            }

            if (handled == EnumHandling.PreventSubsequent)
            {
                break;
            }
        }

        return list.ToArray();
    }

    //
    // Summary:
    //     Called by client when a new server pos arrived
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

    //
    // Summary:
    //     Called when on the client side something called capi.Network.SendEntityPacket()
    //
    //
    // Parameters:
    //   player:
    //
    //   packetid:
    //
    //   data:
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

    //
    // Summary:
    //     Called when on the server side something called sapi.Network.SendEntityPacket()
    //     Packetid = 1 is used for teleporting Packetid = 2 is used for BehaviorHarvestable
    //
    //
    // Parameters:
    //   packetid:
    //
    //   data:
    public virtual void OnReceivedServerPacket(int packetid, byte[] data)
    {
        if (packetid == 1)
        {
            Vec3d vec3d = SerializerUtil.Deserialize<Vec3d>(data);
            if (Api is ICoreClientAPI coreClientAPI && coreClientAPI.World.Player.Entity.EntityId == EntityId)
            {
                Pos.SetPosWithDimension(vec3d);
                ((EntityPlayer)this).UpdatePartitioning();
            }

            ServerPos.SetPosWithDimension(vec3d);
            World.BlockAccessor.MarkBlockDirty(vec3d.AsBlockPos);
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

    //
    // Summary:
    //     Called by BehaviorCollectEntities of nearby entities. Should return the itemstack
    //     that should be collected. If the item stack was fully picked up, BehaviorCollectEntities
    //     will kill this entity
    //
    // Parameters:
    //   byEntity:
    public virtual ItemStack OnCollected(Entity byEntity)
    {
        return null;
    }

    //
    // Summary:
    //     Called on the server when the entity was changed from active to inactive state
    //     or vice versa
    //
    // Parameters:
    //   beforeState:
    public virtual void OnStateChanged(EnumEntityState beforeState)
    {
        EnumHandling handling = EnumHandling.PassThrough;
        foreach (EntityBehavior behavior in SidedProperties.Behaviors)
        {
            behavior.OnStateChanged(beforeState, ref handling);
            if (handling == EnumHandling.PreventSubsequent)
            {
                break;
            }
        }
    }

    //
    // Summary:
    //     Helper method to set the CollisionBox
    //
    // Parameters:
    //   length:
    //
    //   height:
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

    //
    // Summary:
    //     Adds given behavior to the entities list of active behaviors
    //
    // Parameters:
    //   behavior:
    public virtual void AddBehavior(EntityBehavior behavior)
    {
        SidedProperties.Behaviors.Add(behavior);
    }

    //
    // Summary:
    //     Removes given behavior to the entities list of active behaviors. Does nothing
    //     if the behavior has already been removed
    //
    // Parameters:
    //   behavior:
    public virtual void RemoveBehavior(EntityBehavior behavior)
    {
        SidedProperties.Behaviors.Remove(behavior);
    }

    //
    // Summary:
    //     Returns true if the entity has given active behavior
    //
    // Parameters:
    //   behaviorName:
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

    //
    // Summary:
    //     Returns the behavior instance for given entity. Returns null if it doesn't exist.
    //
    //
    // Parameters:
    //   name:
    public virtual EntityBehavior GetBehavior(string name)
    {
        return SidedProperties.Behaviors.FirstOrDefault((EntityBehavior bh) => bh.PropertyName().Equals(name));
    }

    //
    // Summary:
    //     Returns the first behavior instance for given entity of given type. Returns null
    //     if it doesn't exist.
    public virtual T GetBehavior<T>() where T : EntityBehavior
    {
        return (T)SidedProperties.Behaviors.FirstOrDefault((EntityBehavior bh) => bh is T);
    }

    //
    // Summary:
    //     Returns itself and any behaviors that implement the interface T as a List
    //
    // Type parameters:
    //   T:
    public virtual List<T> GetInterfaces<T>() where T : class
    {
        List<T> list = new List<T>();
        if (this is T)
        {
            list.Add(this as T);
        }

        for (int i = 0; i < SidedProperties.Behaviors.Count; i++)
        {
            if (SidedProperties.Behaviors[i] is T)
            {
                list.Add(SidedProperties.Behaviors[i] as T);
            }
        }

        return list;
    }

    //
    // Summary:
    //     Returns itself or the first behavior that implements the interface T
    //
    // Type parameters:
    //   T:
    public virtual T GetInterface<T>() where T : class
    {
        if (this is T)
        {
            return this as T;
        }

        return SidedProperties.Behaviors.FirstOrDefault((EntityBehavior bh) => bh is T) as T;
    }

    //
    // Summary:
    //     Returns true if given activity is running
    //
    // Parameters:
    //   key:
    public virtual bool IsActivityRunning(string key)
    {
        ActivityTimers.TryGetValue(key, out var value);
        return value > World.ElapsedMilliseconds;
    }

    //
    // Summary:
    //     Returns the remaining time on an activity in milliesconds
    //
    // Parameters:
    //   key:
    public virtual int RemainingActivityTime(string key)
    {
        ActivityTimers.TryGetValue(key, out var value);
        return (int)(value - World.ElapsedMilliseconds);
    }

    //
    // Summary:
    //     Starts an activity for a given duration
    //
    // Parameters:
    //   key:
    //
    //   milliseconds:
    public virtual void SetActivityRunning(string key, int milliseconds)
    {
        ActivityTimers[key] = World.ElapsedMilliseconds + milliseconds;
    }

    //
    // Summary:
    //     Updates the DebugAttributes tree
    public virtual void UpdateDebugAttributes()
    {
        if (World.Side == EnumAppSide.Client)
        {
            DebugAttributes.SetString("Entity Id", EntityId.ToString() ?? "");
            DebugAttributes.SetString("Yaw, Pitch", $"{Pos.Yaw * (180f / MathF.PI):0.##}, {Pos.Pitch * (180f / MathF.PI):0.##}");
            if (AnimManager != null)
            {
                UpdateAnimationDebugAttributes();
            }
        }
    }

    protected virtual void UpdateAnimationDebugAttributes()
    {
        string text = "";
        int num = 0;
        foreach (string key in AnimManager.ActiveAnimationsByAnimCode.Keys)
        {
            if (num++ > 0)
            {
                text += ",";
            }

            text += key;
        }

        num = 0;
        StringBuilder stringBuilder = new StringBuilder();
        if (AnimManager.Animator == null)
        {
            return;
        }

        RunningAnimation[] animations = AnimManager.Animator.Animations;
        foreach (RunningAnimation runningAnimation in animations)
        {
            if (runningAnimation.Running)
            {
                if (num++ > 0)
                {
                    stringBuilder.Append(",");
                }

                stringBuilder.Append(runningAnimation.Animation.Code);
            }
        }

        DebugAttributes.SetString("Active Animations", (text.Length > 0) ? text : "-");
        DebugAttributes.SetString("Running Animations", (stringBuilder.Length > 0) ? stringBuilder.ToString() : "-");
    }

    //
    // Summary:
    //     In order to maintain legacy mod API compatibility of FromBytes(BinaryReader reader,
    //     bool isSync), we create an overload which server-side calling code will actually
    //     call, and store the remaps parameter in a field
    //
    // Parameters:
    //   reader:
    //
    //   isSync:
    //
    //   serversideRemaps:
    public virtual void FromBytes(BinaryReader reader, bool isSync, Dictionary<string, string> serversideRemaps)
    {
        codeRemaps = serversideRemaps;
        FromBytes(reader, isSync);
        codeRemaps = null;
    }

    //
    // Summary:
    //     Loads the entity from a stored byte array from the SaveGame
    //
    // Parameters:
    //   reader:
    //
    //   isSync:
    //     True if this is a sync operation, not a chunk read operation
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
        string text = reader.ReadString().DeDuplicate();
        if (codeRemaps != null && codeRemaps.TryGetValue(text, out var value))
        {
            text = value;
        }

        Code = new AssetLocation(text);
        if (!isSync)
        {
            Attributes.FromBytes(reader);
        }

        if (isSync || GameVersion.IsAtLeastVersion(version, "1.8.0-pre.1"))
        {
            TreeAttribute treeAttribute = new TreeAttribute();
            treeAttribute.FromBytes(reader);
            AnimManager?.FromAttributes(treeAttribute, version);
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

    //
    // Summary:
    //     Serializes the slots contents to be stored in the SaveGame
    //
    // Parameters:
    //   writer:
    //
    //   forClient:
    //     True when being used to send an entity to the client
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

        TreeAttribute treeAttribute = new TreeAttribute();
        AnimManager?.ToAttributes(treeAttribute, forClient);
        Stats.ToTreeAttributes(WatchedAttributes, forClient);
        treeAttribute.ToBytes(writer);
    }

    //
    // Summary:
    //     Relevant only for entities with heads, implemented in EntityAgent. Other sub-classes
    //     of Entity (if not EntityAgent) should similarly override this if the headYaw/headPitch
    //     are relevant to them
    protected virtual void SetHeadPositionToWatchedAttributes()
    {
    }

    //
    // Summary:
    //     Relevant only for entities with heads, implemented in EntityAgent. Other sub-classes
    //     of Entity (if not EntityAgent) should similarly override this if the headYaw/headPitch
    //     are relevant to them
    protected virtual void GetHeadPositionFromWatchedAttributes()
    {
    }

    //
    // Summary:
    //     Revives the entity and heals for 9999.
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

    //
    // Summary:
    //     Makes the entity despawn. Entities only drop something on EnumDespawnReason.Death
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
                Entity causeEntity = damageSourceForDeath.GetCauseEntity();
                if (causeEntity != null)
                {
                    WatchedAttributes.SetString("deathByEntityLangCode", "prefixandcreature-" + causeEntity.Code.Path.Replace("-", ""));
                    WatchedAttributes.SetString("deathByEntity", causeEntity.Code.ToString());
                }

                if (causeEntity is EntityPlayer)
                {
                    WatchedAttributes.SetString("deathByPlayer", (causeEntity as EntityPlayer).Player?.PlayerName);
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

    //
    // Summary:
    //     Assumes that it is only called on the server
    //
    // Parameters:
    //   type:
    //
    //   dualCallByPlayer:
    //
    //   randomizePitch:
    //
    //   range:
    public virtual void PlayEntitySound(string type, IPlayer dualCallByPlayer = null, bool randomizePitch = true, float range = 24f)
    {
        if (Properties.ResolvedSounds != null && Properties.ResolvedSounds.TryGetValue(type, out var value) && value.Length != 0)
        {
            World.PlaySoundAt(value[World.Rand.Next(value.Length)], (float)SidedPos.X, (float)SidedPos.InternalY, (float)SidedPos.Z, dualCallByPlayer, randomizePitch, range);
        }
    }

    //
    // Summary:
    //     Should return true if this item can be picked up as an itemstack
    //
    // Parameters:
    //   byEntity:
    public virtual bool CanCollect(Entity byEntity)
    {
        return false;
    }

    //
    // Summary:
    //     This method pings the Notify() method of all behaviors and ai tasks. Can be used
    //     to spread information to other creatures.
    //
    // Parameters:
    //   key:
    //
    //   data:
    public virtual void Notify(string key, object data)
    {
        foreach (EntityBehavior behavior in SidedProperties.Behaviors)
        {
            behavior.Notify(key, data);
        }
    }

    //
    // Summary:
    //     This method is called by the BlockSchematic class a moment before a schematic
    //     containing this entity is getting exported. Since a schematic can be placed anywhere
    //     in the world, this method has to make sure the entities position is set to a
    //     value relative of the schematic origin point defined by startPos Right after
    //     calling this method, the world edit system will call .ToBytes() to serialize
    //     the entity
    //
    // Parameters:
    //   startPos:
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

    //
    // Summary:
    //     This method is called by the BlockSchematic class a moment after a schematic
    //     containing this entity has been exported. Since a schematic can be placed anywhere
    //     in the world, this method has to make sure the entities position is set to the
    //     correct position in relation to the target position of the schematic to be imported.
    //
    //
    // Parameters:
    //   startPos:
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

    //
    // Summary:
    //     Called by the worldedit schematic exporter so that it can also export the mappings
    //     of items/blocks stored inside blockentities
    //
    // Parameters:
    //   blockIdMapping:
    //
    //   itemIdMapping:
    public virtual void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
    {
        foreach (EntityBehavior behavior in Properties.Server.Behaviors)
        {
            behavior.OnStoreCollectibleMappings(blockIdMapping, itemIdMapping);
        }
    }

    //
    // Summary:
    //     Called by the blockschematic loader so that you may fix any blockid/itemid mappings
    //     against the mapping of the savegame, if you store any collectibles in this blockentity.
    //     Note: Some vanilla blocks resolve randomized contents in this method. Hint: Use
    //     itemstack.FixMapping() to do the job for you.
    //
    // Parameters:
    //   worldForNewMappings:
    //
    //   oldBlockIdMapping:
    //
    //   oldItemIdMapping:
    //
    //   schematicSeed:
    //     If you need some sort of randomness consistency accross an imported schematic,
    //     you can use this value
    //
    //   resolveImports:
    //     Turn it off to spawn structures as they are. For example, in this mode, instead
    //     of traders, their meta spawners will spawn
    public virtual void OnLoadCollectibleMappings(IWorldAccessor worldForNewMappings, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
    {
        foreach (EntityBehavior behavior in Properties.Server.Behaviors)
        {
            behavior.OnLoadCollectibleMappings(worldForNewMappings, oldBlockIdMapping, oldItemIdMapping, resolveImports);
        }
    }

    //
    // Summary:
    //     Gets the name for this entity
    public virtual string GetName()
    {
        string text = null;
        foreach (EntityBehavior behavior in SidedProperties.Behaviors)
        {
            EnumHandling handling = EnumHandling.PassThrough;
            string name = behavior.GetName(ref handling);
            switch (handling)
            {
                case EnumHandling.PreventSubsequent:
                    return text;
                case EnumHandling.PreventDefault:
                    text = name;
                    break;
            }
        }

        if (text != null)
        {
            return text;
        }

        if (!Alive)
        {
            return Lang.GetMatching(Code.Domain + ":item-dead-creature-" + Code.Path);
        }

        return Lang.GetMatching(Code.Domain + ":item-creature-" + Code.Path);
    }

    //
    // Summary:
    //     gets the info text for the entity.
    public virtual string GetInfoText()
    {
        StringBuilder stringBuilder = new StringBuilder();
        foreach (EntityBehavior behavior in SidedProperties.Behaviors)
        {
            behavior.GetInfoText(stringBuilder);
        }

        int @int = WatchedAttributes.GetInt("generation");
        if (@int > 0)
        {
            stringBuilder.AppendLine(Lang.Get("Generation: {0}", @int));
        }

        if (!Alive && WatchedAttributes.HasAttribute("deathByPlayer"))
        {
            stringBuilder.AppendLine(Lang.Get("Killed by Player: {0}", WatchedAttributes.GetString("deathByPlayer")));
        }

        if (WatchedAttributes.HasAttribute("extraInfoText"))
        {
            foreach (KeyValuePair<string, IAttribute> item in WatchedAttributes.GetTreeAttribute("extraInfoText").SortedCopy())
            {
                stringBuilder.AppendLine(item.Value.ToString());
            }
        }

        if (Api is ICoreClientAPI coreClientAPI && coreClientAPI.Settings.Bool["extendedDebugInfo"])
        {
            stringBuilder.AppendLine("<font color=\"#bbbbbb\">Id:" + EntityId + "</font>");
            stringBuilder.AppendLine(string.Concat("<font color=\"#bbbbbb\">Code: ", Code, "</font>"));
        }

        return stringBuilder.ToString();
    }

    //
    // Summary:
    //     Starts the animation for the entity.
    //
    // Parameters:
    //   code:
    public virtual void StartAnimation(string code)
    {
        AnimManager.StartAnimation(code);
    }

    //
    // Summary:
    //     stops the animation for the entity.
    //
    // Parameters:
    //   code:
    public virtual void StopAnimation(string code)
    {
        AnimManager.StopAnimation(code);
    }

    //
    // Summary:
    //     To test for player->entity selection.
    //
    // Parameters:
    //   ray:
    //
    //   interesectionTester:
    //     Is already preloaded with the ray
    //
    //   intersectionDistance:
    public virtual bool IntersectsRay(Ray ray, AABBIntersectionTest interesectionTester, out double intersectionDistance, ref int selectionBoxIndex)
    {
        if (trickleDownRayIntersects)
        {
            EnumHandling handled = EnumHandling.PassThrough;
            bool flag = false;
            bool flag2 = false;
            intersectionDistance = 0.0;
            foreach (EntityBehavior behavior in SidedProperties.Behaviors)
            {
                flag2 |= behavior.IntersectsRay(ray, interesectionTester, out intersectionDistance, ref selectionBoxIndex, ref handled);
                flag = flag || handled == EnumHandling.PreventDefault;
                if (handled == EnumHandling.PreventSubsequent)
                {
                    return flag2;
                }
            }

            if (flag)
            {
                return flag2;
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
        float num = ((selectionBox != null) ? (selectionBox.XSize / 2f) : 0.25f);
        foreach (EntityBehavior behavior in SidedProperties.Behaviors)
        {
            EnumHandling handling = EnumHandling.PassThrough;
            float touchDistance = behavior.GetTouchDistance(ref handling);
            if (handling != 0)
            {
                num = touchDistance;
            }

            if (handling == EnumHandling.PreventSubsequent)
            {
                break;
            }
        }

        return num;
    }
}
#if false // Decompilation log
'181' items in cache
------------------
Resolve: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.dll'
------------------
Resolve: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.InteropServices.dll'
------------------
Resolve: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.dll'
------------------
Resolve: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Found single assembly: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\SkiaSharp.dll'
------------------
Resolve: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Concurrent.dll'
------------------
Resolve: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Could not find by name: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
------------------
Resolve: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Found single assembly: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\protobuf-net.dll'
------------------
Resolve: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Security.Cryptography.dll'
------------------
Resolve: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Specialized.dll'
------------------
Resolve: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NetworkInformation.dll'
------------------
Resolve: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Primitives.dll'
------------------
Resolve: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Text.RegularExpressions.dll'
------------------
Resolve: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\cairo-sharp.dll'
------------------
Resolve: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Drawing.Primitives.dll'
------------------
Resolve: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ObjectModel.dll'
------------------
Resolve: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.TypeConverter.dll'
------------------
Resolve: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.GraphicsLibraryFramework.dll'
------------------
Resolve: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Found single assembly: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\Microsoft.Data.Sqlite.dll'
------------------
Resolve: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Data.Common.dll'
------------------
Resolve: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.dll'
------------------
Resolve: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.Desktop.dll'
------------------
Resolve: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.Thread.dll'
------------------
Resolve: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.Process.dll'
------------------
Resolve: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Linq.dll'
------------------
Resolve: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Console.dll'
------------------
Resolve: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NameResolution.dll'
------------------
Resolve: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.StackTrace.dll'
------------------
Resolve: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.Primitives.dll'
------------------
Resolve: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.ThreadPool.dll'
------------------
Resolve: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.NonGeneric.dll'
------------------
Resolve: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.CompilerServices.Unsafe.dll'
#endif
