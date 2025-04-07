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
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public class EntityPlayer : EntityHumanoid, IPettable
{
    //
    // Summary:
    //     The block position previously selected by the player
    public BlockPos PreviousBlockSelection;

    //
    // Summary:
    //     The block or blocks currently selected by the player
    public BlockSelection BlockSelection;

    //
    // Summary:
    //     The entity or entities selected by the player
    public EntitySelection EntitySelection;

    //
    // Summary:
    //     The reason the player died (if the player did die). Set only by the game server.
    public DamageSource DeathReason;

    //
    // Summary:
    //     The camera position of the player's view. Set only by the game client.
    public Vec3d CameraPos = new Vec3d();

    //
    // Summary:
    //     An offset which can be applied to the camera position to achieve certain special
    //     effects or special features, for example Timeswitch feature. Set only by the
    //     game client.
    public Vec3d CameraPosOffset = new Vec3d();

    //
    // Summary:
    //     The yaw the player currently wants to walk towards to. Value set by the PlayerPhysics
    //     system. Set by the game client and server.
    public float WalkYaw;

    //
    // Summary:
    //     The pitch the player currently wants to move to. Only relevant while swimming.
    //     Value set by the PlayerPhysics system. Set by the game client and server.
    public float WalkPitch;

    //
    // Summary:
    //     Called whenever the game wants to spawn new creatures around the player. Called
    //     only by the game server.
    public CanSpawnNearbyDelegate OnCanSpawnNearby;

    public EntityTalkUtil talkUtil;

    public AngleConstraint BodyYawLimits;

    public AngleConstraint HeadYawLimits;

    //
    // Summary:
    //     Used to assist if this EntityPlayer needs to be repartitioned
    public List<Entity> entityListForPartitioning;

    //
    // Summary:
    //     This is not walkspeed per se, it is the walkspeed modifier as a result of armor
    //     and other gear. It corresponds to Stats.GetBlended("walkspeed") and gets updated
    //     every tick
    public float walkSpeed = 1f;

    private long lastInsideSoundTimeFinishTorso;

    private long lastInsideSoundTimeFinishLegs;

    private bool newSpawnGlow;

    private PlayerAnimationManager animManager;

    private PlayerAnimationManager selfFpAnimManager;

    public bool selfNowShadowPass;

    private double walkCounter;

    private double prevStepHeight;

    private int direction;

    public bool PrevFrameCanStandUp;

    public ClimateCondition selfClimateCond;

    private float climateCondAccum;

    private bool tesselating;

    private const int overlapPercentage = 10;

    private Cuboidf tmpCollBox = new Cuboidf();

    private bool holdPosition;

    private float[] prevAnimModelMatrix;

    private float secondsDead;

    private float strongWindAccum;

    private bool haveHandUseOrHit;

    protected static Dictionary<string, EnumTalkType> talkTypeByAnimation = new Dictionary<string, EnumTalkType>
    {
        {
            "wave",
            EnumTalkType.Meet
        },
        {
            "nod",
            EnumTalkType.Purchase
        },
        {
            "rage",
            EnumTalkType.Complain
        },
        {
            "shrug",
            EnumTalkType.Shrug
        },
        {
            "facepalm",
            EnumTalkType.IdleShort
        },
        {
            "laugh",
            EnumTalkType.Laugh
        }
    };

    public override float BodyYaw
    {
        get
        {
            return base.BodyYaw;
        }
        set
        {
            if (BodyYawLimits != null)
            {
                float val = GameMath.AngleRadDistance(BodyYawLimits.CenterRad, value);
                base.BodyYaw = BodyYawLimits.CenterRad + GameMath.Clamp(val, 0f - BodyYawLimits.RangeRad, BodyYawLimits.RangeRad);
            }
            else
            {
                base.BodyYaw = value;
            }
        }
    }

    public double LastReviveTotalHours
    {
        get
        {
            if (!WatchedAttributes.attributes.TryGetValue("lastReviveTotalHours", out var value))
            {
                return -9999.0;
            }

            return (value as DoubleAttribute).value;
        }
        set
        {
            WatchedAttributes.SetDouble("lastReviveTotalHours", value);
        }
    }

    public override bool StoreWithChunk => false;

    //
    // Summary:
    //     The player's internal Universal ID. Available on the client and the server.
    public string PlayerUID => WatchedAttributes.GetString("playerUID");

    //
    // Summary:
    //     The players right hand contents. Available on the client and the server.
    public override ItemSlot RightHandItemSlot => World.PlayerByUid(PlayerUID)?.InventoryManager.ActiveHotbarSlot;

    //
    // Summary:
    //     The playres left hand contents. Available on the client and the server.
    public override ItemSlot LeftHandItemSlot => World.PlayerByUid(PlayerUID)?.InventoryManager?.GetHotbarInventory()?[11];

    public override byte[] LightHsv
    {
        get
        {
            IPlayer player = Player;
            if (player != null && player.WorldData.CurrentGameMode == EnumGameMode.Spectator)
            {
                return null;
            }

            byte[] array = RightHandItemSlot?.Itemstack?.Collectible?.GetLightHsv(World.BlockAccessor, null, RightHandItemSlot.Itemstack);
            byte[] array2 = LeftHandItemSlot?.Itemstack?.Collectible?.GetLightHsv(World.BlockAccessor, null, LeftHandItemSlot.Itemstack);
            if ((array == null || array[2] == 0) && (array2 == null || array2[2] == 0))
            {
                double num = Api.World.Calendar.TotalHours - LastReviveTotalHours;
                if (num < 2.0)
                {
                    newSpawnGlow = true;
                    base.Properties.Client.GlowLevel = (int)GameMath.Clamp(100.0 * (2.0 - num), 0.0, 255.0);
                }

                if (num < 1.5)
                {
                    newSpawnGlow = true;
                    return new byte[3]
                    {
                        33,
                        7,
                        (byte)Math.Min(10.0, 11.0 * (1.5 - num))
                    };
                }
            }
            else if (newSpawnGlow)
            {
                base.Properties.Client.GlowLevel = 0;
                newSpawnGlow = false;
            }

            if (array == null)
            {
                return array2;
            }

            if (array2 == null)
            {
                return array;
            }

            float num2 = array[2] + array2[2];
            float num3 = (float)(int)array2[2] / num2;
            return new byte[3]
            {
                (byte)((float)(int)array2[0] * num3 + (float)(int)array[0] * (1f - num3)),
                (byte)((float)(int)array2[1] * num3 + (float)(int)array[1] * (1f - num3)),
                Math.Max(array2[2], array[2])
            };
        }
    }

    public override bool AlwaysActive => true;

    public override bool ShouldDespawn => false;

    internal override bool LoadControlsFromServer
    {
        get
        {
            if (World is IClientWorldAccessor)
            {
                return ((IClientWorldAccessor)World).Player.Entity.EntityId != EntityId;
            }

            return true;
        }
    }

    public override bool IsInteractable
    {
        get
        {
            IWorldPlayerData worldPlayerData = World?.PlayerByUid(PlayerUID)?.WorldData;
            if (worldPlayerData == null || worldPlayerData.CurrentGameMode != EnumGameMode.Spectator)
            {
                if (worldPlayerData == null)
                {
                    return true;
                }

                return !worldPlayerData.NoClip;
            }

            return false;
        }
    }

    public override double LadderFixDelta => base.Properties.SpawnCollisionBox.Y2 - SelectionBox.YSize;

    //
    // Summary:
    //     The base player attached to this EntityPlayer.
    public IPlayer Player => World?.PlayerByUid(PlayerUID);

    private bool IsSelf => PlayerUID == (Api as ICoreClientAPI)?.Settings.String["playeruid"];

    public override IAnimationManager AnimManager
    {
        get
        {
            ICoreClientAPI coreClientAPI = Api as ICoreClientAPI;
            if (IsSelf && coreClientAPI.Render.CameraType == EnumCameraMode.FirstPerson && !selfNowShadowPass)
            {
                return selfFpAnimManager;
            }

            return animManager;
        }
        set
        {
        }
    }

    public IAnimationManager OtherAnimManager
    {
        get
        {
            ICoreClientAPI coreClientAPI = Api as ICoreClientAPI;
            if (IsSelf && coreClientAPI.Render.CameraType == EnumCameraMode.FirstPerson && !selfNowShadowPass)
            {
                return animManager;
            }

            return selfFpAnimManager;
        }
    }

    public PlayerAnimationManager TpAnimManager => animManager;

    public PlayerAnimationManager SelfFpAnimManager => selfFpAnimManager;

    public float HeadBobbingAmplitude { get; set; } = 1f;


    //
    // Summary:
    //     Set this to hook into the foot step sound creator thingy. Currently used by the
    //     armor system to create armor step sounds. Called by the game client and server.
    public event Action OnFootStep;

    //
    // Summary:
    //     Called when the player falls onto the ground. Called by the game client and server.
    public event Action<double> OnImpact;

    public void UpdatePartitioning()
    {
        (Api.ModLoader.GetModSystem("Vintagestory.GameContent.EntityPartitioning") as IEntityPartitioning)?.RePartitionPlayer(this);
    }

    public EntityPlayer()
    {
        animManager = new PlayerAnimationManager();
        animManager.UseFpAnmations = false;
        selfFpAnimManager = new PlayerAnimationManager();
        requirePosesOnServer = true;
        Stats.Register("healingeffectivness").Register("maxhealthExtraPoints").Register("walkspeed")
            .Register("hungerrate")
            .Register("rangedWeaponsAcc")
            .Register("rangedWeaponsSpeed")
            .Register("rangedWeaponsDamage")
            .Register("meleeWeaponsDamage")
            .Register("mechanicalsDamage")
            .Register("animalLootDropRate")
            .Register("forageDropRate")
            .Register("wildCropDropRate")
            .Register("vesselContentsDropRate")
            .Register("oreDropRate")
            .Register("rustyGearDropRate")
            .Register("miningSpeedMul")
            .Register("animalSeekingRange")
            .Register("armorDurabilityLoss")
            .Register("armorWalkSpeedAffectedness")
            .Register("bowDrawingStrength")
            .Register("wholeVesselLootChance", EnumStatBlendType.FlatSum)
            .Register("temporalGearTLRepairCost", EnumStatBlendType.FlatSum)
            .Register("animalHarvestingTime")
            .Register("gliderLiftMax")
            .Register("gliderSpeedMax")
            .Register("jumpHeightMul");
    }

    public override void Initialize(EntityProperties properties, ICoreAPI api, long chunkindex3d)
    {
        controls.StopAllMovement();
        talkUtil = new EntityTalkUtil(api, this, isMultiSoundVoice: false);
        if (api.Side == EnumAppSide.Client)
        {
            talkUtil.soundName = new AssetLocation("sounds/voice/altoflute");
            talkUtil.idleTalkChance = 0f;
            talkUtil.AddSoundLengthChordDelay = true;
            talkUtil.volumneModifier = 0.8f;
        }

        base.Initialize(properties, api, chunkindex3d);
        if (api.Side == EnumAppSide.Server && !WatchedAttributes.attributes.ContainsKey("lastReviveTotalHours"))
        {
            double value = (((double)Api.World.Calendar.GetDayLightStrength(ServerPos.X, ServerPos.Z) < 0.5) ? Api.World.Calendar.TotalHours : (-9999.0));
            WatchedAttributes.SetDouble("lastReviveTotalHours", value);
        }

        if (IsSelf)
        {
            OtherAnimManager.Init(api, this);
        }
    }

    public override double GetWalkSpeedMultiplier(double groundDragFactor = 0.3)
    {
        double num = base.GetWalkSpeedMultiplier(groundDragFactor);
        IPlayer player = Player;
        if (player != null && player.WorldData.CurrentGameMode == EnumGameMode.Creative)
        {
            int num2 = (int)(base.SidedPos.InternalY - 0.05000000074505806);
            int num3 = (int)(base.SidedPos.InternalY + 0.009999999776482582);
            Block blockRaw = World.BlockAccessor.GetBlockRaw((int)base.SidedPos.X, num2, (int)base.SidedPos.Z);
            num /= (double)(blockRaw.WalkSpeedMultiplier * ((num2 == num3) ? 1f : insideBlock.WalkSpeedMultiplier));
        }

        num *= (double)GameMath.Clamp(walkSpeed, 0f, 999f);
        if (!servercontrols.Sneak && !PrevFrameCanStandUp)
        {
            num *= (double)GlobalConstants.SneakSpeedMultiplier;
        }

        return num;
    }

    public void OnSelfBeforeRender(float dt)
    {
        updateEyeHeight(dt);
        HandleSeraphHandAnimations(dt);
    }

    public override void OnTesselation(ref Shape entityShape, string shapePathForLogging)
    {
        tesselating = true;
        base.OnTesselation(ref entityShape, shapePathForLogging);
        AnimManager.HeadController = new PlayerHeadController(AnimManager, this, entityShape);
        if (IsSelf)
        {
            OtherAnimManager.LoadAnimator(World.Api, this, entityShape, OtherAnimManager.Animator?.Animations, true, "head");
            OtherAnimManager.HeadController = new PlayerHeadController(OtherAnimManager, this, entityShape);
        }
    }

    public override void OnTesselated()
    {
        tesselating = false;
    }

    private void updateEyeHeight(float dt)
    {
        IPlayer player = World.PlayerByUid(PlayerUID);
        PrevFrameCanStandUp = true;
        if (tesselating || player == null || (player != null && (player.WorldData?.CurrentGameMode).GetValueOrDefault() == EnumGameMode.Spectator))
        {
            return;
        }

        EntityControls entityControls = ((base.MountedOn != null) ? base.MountedOn.Controls : servercontrols);
        PrevFrameCanStandUp = !entityControls.Sneak && canStandUp();
        int num;
        int num2;
        if (entityControls.TriesToMove && base.SidedPos.Motion.LengthSq() > 1E-05 && !entityControls.NoClip)
        {
            num = ((!entityControls.DetachedMode) ? 1 : 0);
            if (num != 0)
            {
                num2 = (OnGround ? 1 : 0);
                goto IL_00d6;
            }
        }
        else
        {
            num = 0;
        }

        num2 = 0;
        goto IL_00d6;
    IL_00d6:
        bool flag = (byte)num2 != 0;
        double num3 = base.Properties.EyeHeight;
        double num4 = base.Properties.CollisionBoxSize.Y;
        if (entityControls.FloorSitting)
        {
            num3 *= 0.5;
            num4 *= 0.550000011920929;
        }
        else if ((entityControls.Sneak || !PrevFrameCanStandUp) && !entityControls.IsClimbing && !entityControls.IsFlying)
        {
            num3 *= 0.800000011920929;
            num4 *= 0.800000011920929;
        }
        else if (!Alive)
        {
            num3 *= 0.25;
            num4 *= 0.25;
        }

        double num5 = (num3 - LocalEyePos.Y) * 5.0 * (double)dt;
        LocalEyePos.Y = ((num5 > 0.0) ? Math.Min(LocalEyePos.Y + num5, num3) : Math.Max(LocalEyePos.Y + num5, num3));
        num5 = (num4 - (double)OriginSelectionBox.Y2) * 5.0 * (double)dt;
        OriginSelectionBox.Y2 = (SelectionBox.Y2 = (float)((num5 > 0.0) ? Math.Min((double)SelectionBox.Y2 + num5, num4) : Math.Max((double)SelectionBox.Y2 + num5, num4)));
        num5 = (num4 - (double)OriginCollisionBox.Y2) * 5.0 * (double)dt;
        OriginCollisionBox.Y2 = (CollisionBox.Y2 = (float)((num5 > 0.0) ? Math.Min((double)CollisionBox.Y2 + num5, num4) : Math.Max((double)CollisionBox.Y2 + num5, num4)));
        LocalEyePos.X = 0.0;
        LocalEyePos.Z = 0.0;
        bool flag2 = false;
        if (base.MountedOn != null)
        {
            flag2 = base.MountedOn.SuggestedAnimation?.Code == "sleep";
            if (base.MountedOn.LocalEyePos != null)
            {
                LocalEyePos.Set(base.MountedOn.LocalEyePos);
            }
        }

        if (player.ImmersiveFpMode && !flag2)
        {
            secondsDead = (Alive ? 0f : (secondsDead + dt));
            updateLocalEyePosImmersiveFpMode(dt);
        }

        double num6 = (double)(dt * entityControls.MovespeedMultiplier) * GetWalkSpeedMultiplier() * (entityControls.Sprint ? 0.9 : 1.2) * (double)(entityControls.Sneak ? 1.2f : 1f);
        walkCounter = (flag ? (walkCounter + num6) : 0.0);
        walkCounter %= 6.2831854820251465;
        double num7 = (entityControls.Sneak ? 5.0 : 1.8);
        double num8 = (FeetInLiquid ? 0.8 : (1.0 + (entityControls.Sprint ? 0.07 : 0.0))) / (3.0 * num7) * (double)HeadBobbingAmplitude;
        double num9 = -0.2 / num7;
        double num10 = 0.0 - Math.Max(0.0, Math.Abs(GameMath.Sin(5.5 * walkCounter) * num8) + num9);
        if (World.Side == EnumAppSide.Client)
        {
            ICoreClientAPI coreClientAPI = World.Api as ICoreClientAPI;
            if (coreClientAPI.Settings.Bool["viewBobbing"] && coreClientAPI.Render.CameraType == EnumCameraMode.FirstPerson)
            {
                LocalEyePos.Y += num10 / 3.0 * (double)dt * 60.0;
            }
        }

        if (num != 0)
        {
            bool playingInsideSound = PlayInsideSound(player);
            if (flag)
            {
                if (num10 > prevStepHeight)
                {
                    if (direction == -1)
                    {
                        PlayStepSound(player, playingInsideSound);
                    }

                    direction = 1;
                }
                else
                {
                    direction = -1;
                }
            }
        }

        prevStepHeight = num10;
    }

    public virtual Block GetInsideTorsoBlockSoundSource(BlockPos tmpPos)
    {
        return GetNearestBlockSoundSource(tmpPos, 1.1, 1, usecollisionboxes: false);
    }

    public virtual Block GetInsideLegsBlockSoundSource(BlockPos tmpPos)
    {
        return GetNearestBlockSoundSource(tmpPos, 0.2, 1, usecollisionboxes: false);
    }

    public virtual bool PlayInsideSound(IPlayer player)
    {
        if (Swimming)
        {
            return false;
        }

        BlockPos blockPos = new BlockPos((int)Pos.X, (int)Pos.Y, (int)Pos.Z, Pos.Dimension);
        BlockSelection blockSel = new BlockSelection
        {
            Position = blockPos,
            Face = null
        };
        AssetLocation assetLocation = GetInsideTorsoBlockSoundSource(blockPos)?.GetSounds(Api.World.BlockAccessor, blockSel).Inside;
        AssetLocation assetLocation2 = GetInsideLegsBlockSoundSource(blockPos)?.GetSounds(Api.World.BlockAccessor, blockSel).Inside;
        bool result = false;
        if (assetLocation != null)
        {
            if (Api.Side == EnumAppSide.Client)
            {
                long num = Environment.TickCount;
                if (num > lastInsideSoundTimeFinishTorso)
                {
                    float volume = (controls.Sneak ? 0.25f : 1f);
                    int num2 = PlaySound(player, assetLocation, 12, volume, 1.4);
                    lastInsideSoundTimeFinishTorso = num + num2 * 90 / 100;
                }
            }

            result = true;
        }

        if (assetLocation2 != null && assetLocation2 != assetLocation)
        {
            if (Api.Side == EnumAppSide.Client)
            {
                long num3 = Environment.TickCount;
                if (num3 > lastInsideSoundTimeFinishLegs)
                {
                    float volume2 = (controls.Sneak ? 0.35f : 1f);
                    int num4 = PlaySound(player, assetLocation2, 12, volume2, 0.6);
                    lastInsideSoundTimeFinishLegs = num3 + num4 * 90 / 100;
                }
            }

            result = true;
        }

        return result;
    }

    public virtual void PlayStepSound(IPlayer player, bool playingInsideSound)
    {
        float num = (controls.Sneak ? 0.5f : 1f);
        EntityPos sidedPos = base.SidedPos;
        BlockPos blockPos = new BlockPos((int)sidedPos.X, (int)sidedPos.Y, (int)sidedPos.Z, sidedPos.Dimension);
        BlockSelection blockSel = new BlockSelection
        {
            Position = blockPos,
            Face = BlockFacing.UP
        };
        AssetLocation assetLocation = GetNearestBlockSoundSource(blockPos, -0.03, 4, usecollisionboxes: true)?.GetSounds(Api.World.BlockAccessor, blockSel)?.Walk ?? GetNearestBlockSoundSource(blockPos, -0.7, 4, usecollisionboxes: true)?.GetSounds(Api.World.BlockAccessor, blockSel)?.Walk;
        blockPos.Set((int)sidedPos.X, (int)(sidedPos.Y + 0.10000000149011612), (int)sidedPos.Z);
        AssetLocation assetLocation2 = World.BlockAccessor.GetBlock(blockPos, 2).GetSounds(Api.World.BlockAccessor, blockSel)?.Inside;
        if (assetLocation2 != null)
        {
            PlaySound(player, assetLocation2, 12, num, 1.0);
        }

        if (!Swimming && assetLocation != null)
        {
            PlaySound(player, assetLocation, 12, playingInsideSound ? (num * 0.5f) : num, 0.0);
            this.OnFootStep?.Invoke();
        }
    }

    private int PlaySound(IPlayer player, AssetLocation sound, int range, float volume, double yOffset)
    {
        bool num = player.PlayerUID == (Api as ICoreClientAPI)?.World.Player?.PlayerUID;
        IServerPlayer serverPlayer = player as IServerPlayer;
        double num2 = 0.0;
        double num3 = 0.0;
        double num4 = 0.0;
        if (!num)
        {
            num2 = Pos.X;
            num3 = Pos.InternalY + yOffset;
            num4 = Pos.Z;
        }

        if (Api.Side == EnumAppSide.Client)
        {
            return ((IClientWorldAccessor)World).PlaySoundAtAndGetDuration(sound, num2, num3, num4, serverPlayer, randomizePitch: true, range, volume);
        }

        World.PlaySoundAt(sound, num2, num3, num4, serverPlayer, randomizePitch: true, range, volume);
        return 0;
    }

    public override void OnGameTick(float dt)
    {
        walkSpeed = Stats.GetBlended("walkspeed");
        if (World.Side == EnumAppSide.Client)
        {
            talkUtil.OnGameTick(dt);
        }
        else
        {
            HandleSeraphHandAnimations(dt);
        }

        bool flag = (Api as ICoreClientAPI)?.World.Player.PlayerUID == PlayerUID;
        if (Api.Side == EnumAppSide.Server || !flag)
        {
            updateEyeHeight(dt);
        }

        if (flag)
        {
            alwaysRunIdle = (Api as ICoreClientAPI).Render.CameraType == EnumCameraMode.FirstPerson && !selfNowShadowPass;
        }

        climateCondAccum += dt;
        if (flag && World.Side == EnumAppSide.Client && climateCondAccum > 0.5f)
        {
            climateCondAccum = 0f;
            selfClimateCond = Api.World.BlockAccessor.GetClimateAt(Pos.XYZ.AsBlockPos);
        }

        if (!servercontrols.Sneak && !PrevFrameCanStandUp)
        {
            servercontrols.Sneak = true;
            base.OnGameTick(dt);
            servercontrols.Sneak = false;
        }
        else
        {
            base.OnGameTick(dt);
        }
    }

    public override void OnAsyncParticleTick(float dt, IAsyncParticleManager manager)
    {
        base.OnAsyncParticleTick(dt, manager);
        bool flag = (((Api as ICoreClientAPI).World.Player.Entity.EntityId == EntityId) ? Pos : ServerPos).Motion.LengthSq() > 1E-05 && !servercontrols.NoClip;
        if ((FeetInLiquid || Swimming) && flag && base.Properties.Habitat != EnumHabitat.Underwater)
        {
            SpawnFloatingSediment(manager);
        }
    }

    private void updateLocalEyePosImmersiveFpMode(float dt)
    {
        if (Api.Side == EnumAppSide.Server || AnimManager.Animator == null || ((Api as ICoreClientAPI).Render.CameraType != 0 && Alive))
        {
            return;
        }

        AttachmentPointAndPose attachmentPointPose = AnimManager.Animator.GetAttachmentPointPose("Eyes");
        bool flag = holdPosition;
        holdPosition = false;
        for (int i = 0; i < AnimManager.Animator.Animations.Length; i++)
        {
            RunningAnimation runningAnimation = AnimManager.Animator.Animations[i];
            if (runningAnimation.Running && runningAnimation.EasingFactor >= runningAnimation.meta.HoldEyePosAfterEasein)
            {
                if (!flag)
                {
                    prevAnimModelMatrix = (float[])attachmentPointPose.AnimModelMatrix.Clone();
                }

                holdPosition = true;
                break;
            }
        }

        updateLocalEyePos(attachmentPointPose, holdPosition ? prevAnimModelMatrix : attachmentPointPose.AnimModelMatrix);
    }

    private void updateLocalEyePos(AttachmentPointAndPose apap, float[] animModelMatrix)
    {
        AttachmentPoint attachPoint = apap.AttachPoint;
        float[] values = Mat4f.Create();
        float bodyYaw = BodyYaw;
        float num = ((base.Properties.Client.Shape != null) ? base.Properties.Client.Shape.rotateX : 0f);
        float num2 = ((base.Properties.Client.Shape != null) ? base.Properties.Client.Shape.rotateY : 0f);
        float num3 = ((base.Properties.Client.Shape != null) ? base.Properties.Client.Shape.rotateZ : 0f);
        float walkPitch = WalkPitch;
        float num4 = (base.SidedPos.Pitch - MathF.PI) / 9f;
        if (!Alive)
        {
            num4 /= secondsDead * 10f;
        }

        Matrixf matrixf = new Matrixf();
        matrixf.Set(values).RotateX(base.SidedPos.Roll + num * (MathF.PI / 180f)).RotateY(bodyYaw + (90f + num2) * (MathF.PI / 180f))
            .RotateZ(walkPitch + num3 * (MathF.PI / 180f))
            .Scale(base.Properties.Client.Size, base.Properties.Client.Size, base.Properties.Client.Size)
            .Translate(-0.5f, 0f, -0.5f)
            .RotateX(sidewaysSwivelAngle)
            .Translate(attachPoint.PosX / 16.0 - (double)(num4 * 1.3f), attachPoint.PosY / 16.0, attachPoint.PosZ / 16.0)
            .Mul(animModelMatrix)
            .Translate(0.07f, Alive ? 0f : (0.2f * Math.Min(1f, secondsDead)), 0f);
        float[] vec = new float[4] { 0f, 0f, 0f, 1f };
        float[] array = Mat4f.MulWithVec4(matrixf.Values, vec);
        LocalEyePos.Set(array[0], array[1], array[2]);
    }

    public void HandleSeraphHandAnimations(float dt)
    {
        protectEyesFromWind(dt);
        if (Api is ICoreClientAPI coreClientAPI && coreClientAPI.World.Player.PlayerUID != PlayerUID)
        {
            return;
        }

        ItemStack itemStack = RightHandItemSlot?.Itemstack;
        if (RightHandItemSlot is ItemSlotSkill)
        {
            itemStack = null;
        }

        EnumHandInteract handUse = servercontrols.HandUse;
        PlayerAnimationManager playerAnimationManager = AnimManager as PlayerAnimationManager;
        bool flag = handUse == EnumHandInteract.BlockInteract || handUse == EnumHandInteract.HeldItemInteract || (servercontrols.RightMouseDown && !servercontrols.LeftMouseDown);
        bool flag2 = playerAnimationManager.IsHeldUseActive();
        bool flag3 = handUse == EnumHandInteract.HeldItemAttack || servercontrols.LeftMouseDown;
        bool flag4 = playerAnimationManager.IsHeldHitActive(1f);
        string text = itemStack?.Collectible.GetHeldTpUseAnimation(RightHandItemSlot, this);
        string text2 = itemStack?.Collectible.GetHeldTpHitAnimation(RightHandItemSlot, this);
        string text3 = itemStack?.Collectible.GetHeldTpIdleAnimation(RightHandItemSlot, this, EnumHand.Right);
        string text4 = LeftHandItemSlot?.Itemstack?.Collectible.GetHeldTpIdleAnimation(LeftHandItemSlot, this, EnumHand.Left);
        string heldReadyAnim = itemStack?.Collectible.GetHeldReadyAnimation(RightHandItemSlot, this, EnumHand.Right);
        bool flag5 = haveHandUseOrHit && !servercontrols.LeftMouseDown && !servercontrols.RightMouseDown && !playerAnimationManager.IsAnimationActiveOrRunning(playerAnimationManager.lastRunningHeldHitAnimation) && !playerAnimationManager.IsAnimationActiveOrRunning(playerAnimationManager.lastRunningHeldUseAnimation);
        bool flag6 = playerAnimationManager.IsRightHeldReadyActive();
        bool flag7 = text3 != null && !flag && !flag3 && !flag5 && !playerAnimationManager.IsAnimationActiveOrRunning(playerAnimationManager.lastRunningHeldHitAnimation) && !playerAnimationManager.IsAnimationActiveOrRunning(playerAnimationManager.lastRunningHeldUseAnimation);
        bool flag8 = playerAnimationManager.IsRightHeldActive();
        bool flag9 = text4 != null;
        bool flag10 = playerAnimationManager.IsLeftHeldActive();
        if (itemStack == null)
        {
            text2 = "breakhand";
            text = "interactstatic";
            if (EntitySelection != null && EntitySelection.Entity.Pos.DistanceTo(Pos) <= 1.15)
            {
                IPettable @interface = EntitySelection.Entity.GetInterface<IPettable>();
                if (@interface == null || @interface.CanPet(this))
                {
                    if ((double)EntitySelection.Entity.SelectionBox.Y2 > 0.8)
                    {
                        text = "petlarge";
                    }

                    if ((double)EntitySelection.Entity.SelectionBox.Y2 <= 0.8 && controls.Sneak)
                    {
                        text = "petsmall";
                    }

                    if (EntitySelection.Entity is EntityPlayer entityPlayer && !entityPlayer.controls.FloorSitting)
                    {
                        text = "petseraph";
                    }
                }
            }
        }

        if (flag5 && !flag6)
        {
            playerAnimationManager.StopRightHeldIdleAnim();
            playerAnimationManager.StartHeldReadyAnim(heldReadyAnim);
            haveHandUseOrHit = false;
        }

        if ((flag != flag2 || playerAnimationManager.HeldUseAnimChanged(text)) && !flag3)
        {
            playerAnimationManager.StopHeldUseAnim();
            if (flag)
            {
                playerAnimationManager.StartHeldUseAnim(text);
                haveHandUseOrHit = true;
            }
        }

        if (flag3 != flag4 || playerAnimationManager.HeldHitAnimChanged(text2))
        {
            bool flag11 = playerAnimationManager.IsAuthoritative(text2);
            bool flag12 = playerAnimationManager.IsHeldHitAuthoritative();
            if (!flag12)
            {
                playerAnimationManager.StopHeldAttackAnim();
                playerAnimationManager.StopAnimation(playerAnimationManager.lastRunningHeldHitAnimation);
            }

            if (playerAnimationManager.lastRunningHeldHitAnimation != null && flag12)
            {
                if (servercontrols.LeftMouseDown)
                {
                    playerAnimationManager.ResetAnimation(text2);
                    controls.HandUse = EnumHandInteract.None;
                    playerAnimationManager.StartHeldHitAnim(text2);
                    haveHandUseOrHit = true;
                    RightHandItemSlot.Itemstack?.Collectible.OnHeldActionAnimStart(RightHandItemSlot, this, EnumHandInteract.HeldItemAttack);
                }
            }
            else
            {
                if (flag11)
                {
                    flag3 = servercontrols.LeftMouseDown;
                }

                if (!flag12 && flag3)
                {
                    playerAnimationManager.StartHeldHitAnim(text2);
                    haveHandUseOrHit = true;
                    RightHandItemSlot.Itemstack?.Collectible.OnHeldActionAnimStart(RightHandItemSlot, this, EnumHandInteract.HeldItemAttack);
                }
            }
        }

        if (flag7 != flag8 || playerAnimationManager.RightHeldIdleChanged(text3))
        {
            playerAnimationManager.StopRightHeldIdleAnim();
            if (flag7)
            {
                playerAnimationManager.StartRightHeldIdleAnim(text3);
            }
        }

        if (flag9 != flag10 || playerAnimationManager.LeftHeldIdleChanged(text4))
        {
            playerAnimationManager.StopLeftHeldIdleAnim();
            if (flag9)
            {
                playerAnimationManager.StartLeftHeldIdleAnim(text4);
            }
        }
    }

    protected void protectEyesFromWind(float dt)
    {
        ICoreAPI api = Api;
        if (api == null || api.Side != EnumAppSide.Client || AnimManager == null)
        {
            return;
        }

        ClimateCondition climateCondition = selfClimateCond;
        float val = ((climateCondition == null) ? 0f : (GlobalConstants.CurrentWindSpeedClient.Length() * (1f - climateCondition.WorldgenRainfall) * (1f - climateCondition.Rainfall)));
        float val2 = ((climateCondition == null) ? 0f : (GlobalConstants.CurrentWindSpeedClient.Length() * climateCondition.Rainfall * Math.Max(0f, (1f - climateCondition.Temperature) / 5f)));
        float num = Math.Max(val, val2);
        strongWindAccum = (((double)num > 0.75 && !Swimming) ? (strongWindAccum + dt) : 0f);
        bool flag = Math.Abs(GameMath.AngleRadDistance((float)Math.Atan2(GlobalConstants.CurrentWindSpeedClient.X, GlobalConstants.CurrentWindSpeedClient.Z), Pos.Yaw - MathF.PI / 2f)) < MathF.PI / 4f;
        if (GlobalConstants.CurrentDistanceToRainfallClient < 6f && flag)
        {
            ItemSlot rightHandItemSlot = RightHandItemSlot;
            if (rightHandItemSlot != null && rightHandItemSlot.Empty && strongWindAccum > 2f && Player.WorldData.CurrentGameMode != EnumGameMode.Creative && !hasEyeProtectiveGear())
            {
                AnimManager.StartAnimation("protecteyes");
                return;
            }
        }

        if (AnimManager.IsAnimationActive("protecteyes"))
        {
            AnimManager.StopAnimation("protecteyes");
        }
    }

    private bool hasEyeProtectiveGear()
    {
        return Attributes.GetBool("hasProtectiveEyeGear");
    }

    private bool canStandUp()
    {
        tmpCollBox.Set(SelectionBox);
        bool flag = World.CollisionTester.IsColliding(World.BlockAccessor, tmpCollBox, Pos.XYZ, alsoCheckTouch: false);
        tmpCollBox.Y2 = base.Properties.CollisionBoxSize.Y;
        tmpCollBox.Y1 += 1f;
        return !World.CollisionTester.IsColliding(World.BlockAccessor, tmpCollBox, Pos.XYZ, alsoCheckTouch: false) || flag;
    }

    protected override bool onAnimControls(AnimationMetaData anim, bool wasActive, bool nowActive)
    {
        AnimationTrigger triggeredBy = anim.TriggeredBy;
        if (triggeredBy != null && triggeredBy.MatchExact && anim.Animation == "sitflooridle")
        {
            bool flag = canPlayEdgeSitAnim();
            bool flag2 = AnimManager.IsAnimationActive("sitidle");
            wasActive = wasActive || flag2;
            ICoreClientAPI coreClientAPI = Api as ICoreClientAPI;
            if (nowActive)
            {
                bool flag3 = AnimManager.IsAnimationActive(anim.Code);
                if (flag)
                {
                    if (flag3)
                    {
                        AnimManager.StopAnimation(anim.Animation);
                    }

                    if (!flag2)
                    {
                        AnimManager.StartAnimation("sitflooredge");
                        coreClientAPI.Network.SendEntityPacket(EntityId, 296, SerializerUtil.Serialize(1));
                        BodyYaw = (float)Math.Round(BodyYaw * (180f / MathF.PI) / 90f) * 90f * (MathF.PI / 180f);
                        BodyYawLimits = new AngleConstraint(BodyYaw, 0.2f);
                        HeadYawLimits = new AngleConstraint(BodyYaw, MathF.PI * 19f / 40f);
                    }

                    return true;
                }

                if (flag2 && !flag && !flag3)
                {
                    AnimManager.StopAnimation("sitidle");
                    coreClientAPI.Network.SendEntityPacket(EntityId, 296, SerializerUtil.Serialize(0));
                    BodyYawLimits = null;
                    HeadYawLimits = null;
                }
            }
            else if (wasActive)
            {
                AnimManager.StopAnimation("sitidle");
                coreClientAPI.Network.SendEntityPacket(EntityId, 296, SerializerUtil.Serialize(0));
                BodyYawLimits = null;
                HeadYawLimits = null;
            }

            return flag;
        }

        return false;
    }

    protected bool canPlayEdgeSitAnim()
    {
        IBlockAccessor blockAccessor = Api.World.BlockAccessor;
        Vec3d xYZ = Pos.XYZ;
        float num = ((BodyYawLimits == null) ? Pos.Yaw : BodyYawLimits.CenterRad);
        float num2 = GameMath.Cos(num + MathF.PI / 2f);
        float num3 = GameMath.Sin(num + MathF.PI / 2f);
        BlockPos asBlockPos = new Vec3d(Pos.X + (double)(num3 * 0.3f), Pos.Y - 1.0, Pos.Z + (double)(num2 * 0.3f)).AsBlockPos;
        Cuboidf[] collisionBoxes = blockAccessor.GetBlock(asBlockPos).GetCollisionBoxes(blockAccessor, asBlockPos);
        if (collisionBoxes == null || collisionBoxes.Length == 0)
        {
            return true;
        }

        return xYZ.Y - (double)((float)asBlockPos.Y + collisionBoxes.Max((Cuboidf box) => box.Y2)) >= 0.45;
    }

    public virtual bool CanSpawnNearby(EntityProperties type, Vec3d spawnPosition, RuntimeSpawnConditions sc)
    {
        if (OnCanSpawnNearby != null)
        {
            return OnCanSpawnNearby(type, spawnPosition, sc);
        }

        return true;
    }

    public override void OnFallToGround(double motionY)
    {
        IPlayer player = World.PlayerByUid(PlayerUID);
        if ((player == null || (player.WorldData?.CurrentGameMode).GetValueOrDefault() != EnumGameMode.Spectator) && motionY < -0.1)
        {
            EntityPos sidedPos = base.SidedPos;
            BlockPos blockPos = new BlockPos((int)sidedPos.X, (int)(sidedPos.Y - 0.10000000149011612), (int)sidedPos.Z, sidedPos.Dimension);
            BlockSelection blockSel = new BlockSelection
            {
                Position = blockPos,
                Face = BlockFacing.UP
            };
            GetNearestBlockSoundSource(blockPos, -0.1, 4, usecollisionboxes: true);
            AssetLocation assetLocation = GetNearestBlockSoundSource(blockPos, -0.1, 4, usecollisionboxes: true)?.GetSounds(Api.World.BlockAccessor, blockSel)?.Walk ?? GetNearestBlockSoundSource(blockPos, -0.7, 4, usecollisionboxes: true)?.GetSounds(Api.World.BlockAccessor, blockSel)?.Walk;
            if (assetLocation != null && !Swimming)
            {
                World.PlaySoundAt(assetLocation, this, player, randomizePitch: true, 12f, 1.5f);
            }

            this.OnImpact?.Invoke(motionY);
        }

        base.OnFallToGround(motionY);
    }

    //
    // Summary:
    //     Returns null if there is no nearby sound source
    //
    // Parameters:
    //   tmpPos:
    //     Might get intentionally modified if the nearest sound source the player is intersecting
    //     with is in an adjacent block
    //
    //   yOffset:
    //
    //   blockLayer:
    //
    //   usecollisionboxes:
    public Block GetNearestBlockSoundSource(BlockPos tmpPos, double yOffset, int blockLayer, bool usecollisionboxes)
    {
        EntityPos sidedPos = base.SidedPos;
        Cuboidd cuboidd = new Cuboidd();
        Cuboidf collisionBox = CollisionBox;
        cuboidd.SetAndTranslate(collisionBox, sidedPos.X, sidedPos.Y + yOffset, sidedPos.Z);
        cuboidd.GrowBy(-0.001, 0.0, -0.001);
        int y = (int)(sidedPos.Y + yOffset);
        tmpPos.Set(sidedPos.XInt, y, sidedPos.ZInt);
        BlockSelection blockSelection = new BlockSelection
        {
            Position = tmpPos,
            Face = BlockFacing.DOWN
        };
        Block soundSourceBlockAt = getSoundSourceBlockAt(cuboidd, blockSelection, blockLayer, usecollisionboxes);
        if (soundSourceBlockAt != null)
        {
            return soundSourceBlockAt;
        }

        double value = GameMath.Mod(sidedPos.X, 1.0) - 0.5;
        double value2 = GameMath.Mod(sidedPos.Z, 1.0) - 0.5;
        int num = sidedPos.XInt + Math.Sign(value);
        int num2 = sidedPos.ZInt + Math.Sign(value2);
        int x;
        int z;
        int x2;
        int z2;
        if (Math.Abs(value) > Math.Abs(value2))
        {
            x = num;
            z = sidedPos.ZInt;
            x2 = sidedPos.XInt;
            z2 = num2;
        }
        else
        {
            x = sidedPos.XInt;
            z = num2;
            x2 = num;
            z2 = sidedPos.ZInt;
        }

        return getSoundSourceBlockAt(cuboidd, blockSelection.SetPos(x, y, z), blockLayer, usecollisionboxes) ?? getSoundSourceBlockAt(cuboidd, blockSelection.SetPos(x2, y, z2), blockLayer, usecollisionboxes) ?? getSoundSourceBlockAt(cuboidd, blockSelection.SetPos(num, y, num2), blockLayer, usecollisionboxes);
    }

    protected Block getSoundSourceBlockAt(Cuboidd entityBox, BlockSelection blockSel, int blockLayer, bool usecollisionboxes)
    {
        Block block = World.BlockAccessor.GetBlock(blockSel.Position, blockLayer);
        if (!usecollisionboxes && block.GetSounds(Api.World.BlockAccessor, blockSel)?.Inside == null)
        {
            return null;
        }

        Cuboidf[] array = (usecollisionboxes ? block.GetCollisionBoxes(World.BlockAccessor, blockSel.Position) : block.GetSelectionBoxes(World.BlockAccessor, blockSel.Position));
        if (array == null)
        {
            return null;
        }

        foreach (Cuboidf cuboidf in array)
        {
            if (cuboidf != null && entityBox.Intersects(cuboidf, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z))
            {
                return block;
            }
        }

        return null;
    }

    public override bool TryGiveItemStack(ItemStack itemstack)
    {
        return World.PlayerByUid(PlayerUID)?.InventoryManager.TryGiveItemstack(itemstack, slotNotifyEffect: true) ?? false;
    }

    public override void Die(EnumDespawnReason reason = EnumDespawnReason.Death, DamageSource damageSourceForDeath = null)
    {
        base.Die(reason, damageSourceForDeath);
        DeathReason = damageSourceForDeath;
        DespawnReason = null;
        DeadNotify = true;
        TryStopHandAction(forceStop: true, EnumItemUseCancelReason.Death);
        TryUnmount();
        WatchedAttributes.SetFloat("intoxication", 0f);
    }

    public override bool TryMount(IMountableSeat onmount)
    {
        bool num = base.TryMount(onmount);
        if (num && Alive && Player != null)
        {
            Player.WorldData.FreeMove = false;
        }

        return num;
    }

    public override void Revive()
    {
        base.Revive();
        LastReviveTotalHours = Api.World.Calendar.TotalHours;
        (Api as ICoreServerAPI).Network.SendEntityPacket(Api.World.PlayerByUid(PlayerUID) as IServerPlayer, EntityId, 196);
    }

    public override void PlayEntitySound(string type, IPlayer dualCallByPlayer = null, bool randomizePitch = true, float range = 24f)
    {
        if (type == "hurt")
        {
            talkUtil?.Talk(EnumTalkType.Hurt2);
        }
        else if (type == "death")
        {
            talkUtil?.Talk(EnumTalkType.Death);
        }
        else
        {
            base.PlayEntitySound(type, dualCallByPlayer, randomizePitch, range);
        }
    }

    public override void OnReceivedServerPacket(int packetid, byte[] data)
    {
        switch (packetid)
        {
            case 196:
                AnimManager?.StopAnimation("die");
                return;
            case 197:
                {
                    string text = SerializerUtil.Deserialize<string>(data);
                    StartAnimation(text);
                    if (talkTypeByAnimation.TryGetValue(text, out var value))
                    {
                        talkUtil?.Talk(value);
                    }

                    break;
                }
        }

        if (packetid == 198)
        {
            TryStopHandAction(forceStop: true, EnumItemUseCancelReason.Death);
        }

        if (packetid == 1231)
        {
            EnumTalkType enumTalkType = SerializerUtil.Deserialize<EnumTalkType>(data);
            if (enumTalkType != EnumTalkType.Death && !Alive)
            {
                return;
            }

            talkUtil.Talk(enumTalkType);
        }

        if (packetid == 200)
        {
            AnimManager.StartAnimation(SerializerUtil.Deserialize<string>(data));
        }

        if (packetid == 1 && base.MountedOn?.Entity != null)
        {
            Entity entity = base.MountedOn.Entity;
            Vec3d posWithDimension = SerializerUtil.Deserialize<Vec3d>(data);
            if ((Api as ICoreClientAPI).World.Player.Entity.EntityId == EntityId)
            {
                entity.Pos.SetPosWithDimension(posWithDimension);
            }

            entity.ServerPos.SetPosWithDimension(posWithDimension);
        }

        base.OnReceivedServerPacket(packetid, data);
    }

    public override void OnReceivedClientPacket(IServerPlayer player, int packetid, byte[] data)
    {
        base.OnReceivedClientPacket(player, packetid, data);
        if (packetid == 296)
        {
            if (SerializerUtil.Deserialize<int>(data) > 0)
            {
                AnimManager.StopAnimation("sitidle");
                AnimManager.StartAnimation("sitflooredge");
            }
            else
            {
                AnimManager.StopAnimation("sitflooredge");
            }
        }
    }

    public override bool ShouldReceiveDamage(DamageSource damageSource, float damage)
    {
        EnumGameMode valueOrDefault = (World?.PlayerByUid(PlayerUID)?.WorldData?.CurrentGameMode).GetValueOrDefault(EnumGameMode.Survival);
        if ((valueOrDefault == EnumGameMode.Creative || valueOrDefault == EnumGameMode.Spectator) && (damageSource == null || damageSource.Type != EnumDamageType.Heal))
        {
            return false;
        }

        return base.ShouldReceiveDamage(damageSource, damage);
    }

    public override void Ignite()
    {
        EnumGameMode valueOrDefault = (World?.PlayerByUid(PlayerUID)?.WorldData?.CurrentGameMode).GetValueOrDefault(EnumGameMode.Survival);
        if (valueOrDefault != EnumGameMode.Creative && valueOrDefault != EnumGameMode.Spectator)
        {
            base.Ignite();
        }
    }

    public override void OnHurt(DamageSource damageSource, float damage)
    {
        if (damage > 0f && World != null && World.Side == EnumAppSide.Client && (World as IClientWorldAccessor).Player.Entity.EntityId == EntityId)
        {
            (World as IClientWorldAccessor).AddCameraShake(0.3f);
        }

        if (damage == 0f)
        {
            return;
        }

        IWorldAccessor world = World;
        if (world != null && world.Side == EnumAppSide.Server)
        {
            bool flag = damageSource.Type == EnumDamageType.Heal;
            string text = Lang.Get("damagetype-" + damageSource.Type.ToString().ToLowerInvariant());
            string message = ((damageSource.Type != EnumDamageType.BluntAttack && damageSource.Type != EnumDamageType.PiercingAttack && damageSource.Type != EnumDamageType.SlashingAttack) ? Lang.Get(flag ? "damagelog-heal" : "damagelog-damage", damage, text) : Lang.Get(flag ? "damagelog-heal-attack" : "damagelog-damage-attack", damage, text, damageSource.Source));
            if (damageSource.Source == EnumDamageSource.Player)
            {
                EntityPlayer entityPlayer = damageSource.GetCauseEntity() as EntityPlayer;
                message = Lang.Get(flag ? "damagelog-heal-byplayer" : "damagelog-damage-byplayer", damage, World.PlayerByUid(entityPlayer.PlayerUID).PlayerName);
            }

            if (damageSource.Source == EnumDamageSource.Entity)
            {
                string text2 = Lang.Get("prefixandcreature-" + damageSource.GetCauseEntity().Code.Path.Replace("-", ""));
                message = Lang.Get(flag ? "damagelog-heal-byentity" : "damagelog-damage-byentity", damage, text2);
            }

            (World.PlayerByUid(PlayerUID) as IServerPlayer).SendMessage(GlobalConstants.DamageLogChatGroup, message, EnumChatType.Notification);
        }
    }

    public override bool TryStopHandAction(bool forceStop, EnumItemUseCancelReason cancelReason = EnumItemUseCancelReason.ReleasedMouse)
    {
        if (controls.HandUse == EnumHandInteract.None || RightHandItemSlot?.Itemstack == null)
        {
            return true;
        }

        IPlayer player = World.PlayerByUid(PlayerUID);
        float secondsPassed = (float)(World.ElapsedMilliseconds - controls.UsingBeginMS) / 1000f;
        controls.HandUse = RightHandItemSlot.Itemstack.Collectible.OnHeldUseCancel(secondsPassed, RightHandItemSlot, this, player.CurrentBlockSelection, player.CurrentEntitySelection, cancelReason);
        if (forceStop)
        {
            controls.HandUse = EnumHandInteract.None;
        }

        if (controls.HandUse == EnumHandInteract.None)
        {
            RightHandItemSlot.Itemstack.Collectible.OnHeldUseStop(secondsPassed, RightHandItemSlot, this, player.CurrentBlockSelection, player.CurrentEntitySelection, controls.HandUse);
        }

        return controls.HandUse == EnumHandInteract.None;
    }

    public override void WalkInventory(OnInventorySlot handler)
    {
        IPlayer player = World.PlayerByUid(PlayerUID);
        foreach (InventoryBase item in player.InventoryManager.InventoriesOrdered)
        {
            if (item.ClassName == "creative" || !item.HasOpened(player))
            {
                continue;
            }

            int count = item.Count;
            for (int i = 0; i < count; i++)
            {
                if (!handler(item[i]))
                {
                    return;
                }
            }
        }
    }

    //
    // Summary:
    //     Sets the current player.
    public void SetCurrentlyControlledPlayer()
    {
        servercontrols = controls;
    }

    public override void OnCollideWithLiquid()
    {
        if (World?.PlayerByUid(PlayerUID)?.WorldData != null && World.PlayerByUid(PlayerUID).WorldData.CurrentGameMode != EnumGameMode.Spectator)
        {
            base.OnCollideWithLiquid();
        }
    }

    public override void TeleportToDouble(double x, double y, double z, Action onTeleported = null)
    {
        if (ignoreTeleportCall)
        {
            return;
        }

        ICoreServerAPI sapi = World.Api as ICoreServerAPI;
        if (sapi == null)
        {
            return;
        }

        ignoreTeleportCall = true;
        if (base.MountedOn != null)
        {
            if (base.MountedOn.Entity != null)
            {
                base.MountedOn.Entity.TeleportToDouble(x, y, z, delegate
                {
                    onplrteleported(x, y, z, onTeleported, sapi);
                    onTeleported?.Invoke();
                });
                ignoreTeleportCall = false;
                return;
            }

            TryUnmount();
        }

        Teleporting = true;
        sapi.WorldManager.LoadChunkColumnPriority((int)x / 32, (int)z / 32, new ChunkLoadOptions
        {
            OnLoaded = delegate
            {
                onplrteleported(x, y, z, onTeleported, sapi);
                Teleporting = false;
            }
        });
        ignoreTeleportCall = false;
    }

    private void onplrteleported(double x, double y, double z, Action onTeleported, ICoreServerAPI sapi)
    {
        int xInt = ServerPos.XInt;
        int zInt = ServerPos.ZInt;
        Pos.SetPos(x, y, z);
        ServerPos.SetPos(x, y, z);
        PreviousServerPos.SetPos(-99, -99, -99);
        PositionBeforeFalling.Set(x, y, z);
        Pos.Motion.Set(0.0, 0.0, 0.0);
        sapi.Network.BroadcastEntityPacket(EntityId, 1, SerializerUtil.Serialize(ServerPos.XYZ));
        UpdatePartitioning();
        IServerPlayer player = Player as IServerPlayer;
        int chunksize = 32;
        player.CurrentChunkSentRadius = 0;
        sapi.Event.RegisterCallback(delegate
        {
            if (player.ConnectionState != 0)
            {
                if (!sapi.WorldManager.HasChunk((int)x / chunksize, (int)y / chunksize, (int)z / chunksize, player))
                {
                    sapi.WorldManager.SendChunk((int)x / chunksize, (int)y / chunksize, (int)z / chunksize, player, onlyIfInRange: false);
                }

                player.CurrentChunkSentRadius = 0;
            }
        }, 50);
        WatchedAttributes.SetInt("positionVersionNumber", WatchedAttributes.GetInt("positionVersionNumber") + 1);
        WatchedAttributes.MarkAllDirty();
        if (((double)(xInt / 32) != x / 32.0 || (double)(zInt / 32) != z / 32.0) && player.ClientId != 0)
        {
            sapi.Event.PlayerChunkTransition(player);
        }

        onTeleported?.Invoke();
    }

    public void SetName(string name)
    {
        ITreeAttribute treeAttribute = WatchedAttributes.GetTreeAttribute("nametag");
        if (treeAttribute == null)
        {
            WatchedAttributes["nametag"] = new TreeAttribute();
        }

        treeAttribute.SetString("name", name);
    }

    public override string GetInfoText()
    {
        StringBuilder stringBuilder = new StringBuilder();
        if (!Alive)
        {
            stringBuilder.AppendLine(Lang.Get(Code.Domain + ":item-dead-creature-" + Code.Path));
        }
        else
        {
            stringBuilder.AppendLine(Lang.Get(Code.Domain + ":item-creature-" + Code.Path));
        }

        string @string = WatchedAttributes.GetString("characterClass");
        if (Lang.HasTranslation("characterclass-" + @string))
        {
            stringBuilder.AppendLine(Lang.Get("characterclass-" + @string));
        }

        return stringBuilder.ToString();
    }

    public override void FromBytes(BinaryReader reader, bool forClient)
    {
        base.FromBytes(reader, forClient);
        walkSpeed = Stats.GetBlended("walkspeed");
    }

    public override void UpdateDebugAttributes()
    {
        base.UpdateDebugAttributes();
        string text = "";
        int num = 0;
        foreach (string key in OtherAnimManager.ActiveAnimationsByAnimCode.Keys)
        {
            if (num++ > 0)
            {
                text += ",";
            }

            text += key;
        }

        num = 0;
        StringBuilder stringBuilder = new StringBuilder();
        if (OtherAnimManager.Animator == null)
        {
            return;
        }

        RunningAnimation[] animations = OtherAnimManager.Animator.Animations;
        foreach (RunningAnimation runningAnimation in animations)
        {
            if (runningAnimation.Active)
            {
                if (num++ > 0)
                {
                    stringBuilder.Append(",");
                }

                stringBuilder.Append(runningAnimation.Animation.Code);
            }
        }

        DebugAttributes.SetString("Other Active Animations", (text.Length > 0) ? text : "-");
        DebugAttributes.SetString("Other Running Animations", (stringBuilder.Length > 0) ? stringBuilder.ToString() : "-");
    }

    public void ChangeDimension(int dim)
    {
        _ = InChunkIndex3d;
        Pos.Dimension = dim;
        ServerPos.Dimension = dim;
        Api.Event.TriggerPlayerDimensionChanged(Player);
        long newChunkIndex3d = Api.World.ChunkProvider.ChunkIndex3D(Pos);
        Api.World.UpdateEntityChunk(this, newChunkIndex3d);
    }

    public bool CanPet(Entity byEntity)
    {
        return true;
    }
}
#if false // Decompilation log
'182' items in cache
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
Found single assembly: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\Newtonsoft.Json.dll'
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
