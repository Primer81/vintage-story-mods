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
	/// <summary>
	/// The block position previously selected by the player
	/// </summary>
	public BlockPos PreviousBlockSelection;

	/// <summary>
	/// The block or blocks currently selected by the player
	/// </summary>
	public BlockSelection BlockSelection;

	/// <summary>
	/// The entity or entities selected by the player
	/// </summary>
	public EntitySelection EntitySelection;

	/// <summary>
	/// The reason the player died (if the player did die). Set only by the game server.
	/// </summary>
	public DamageSource DeathReason;

	/// <summary>
	/// The camera position of the player's view. Set only by the game client.
	/// </summary>
	public Vec3d CameraPos = new Vec3d();

	/// <summary>
	/// An offset which can be applied to the camera position to achieve certain special effects or special features, for example Timeswitch feature. Set only by the game client.
	/// </summary>
	public Vec3d CameraPosOffset = new Vec3d();

	/// <summary>
	/// The yaw the player currently wants to walk towards to. Value set by the PlayerPhysics system. Set by the game client and server.
	/// </summary>
	public float WalkYaw;

	/// <summary>
	/// The pitch the player currently wants to move to. Only relevant while swimming. Value set by the PlayerPhysics system. Set by the game client and server.
	/// </summary>
	public float WalkPitch;

	/// <summary>
	/// Called whenever the game wants to spawn new creatures around the player. Called only by the game server.
	/// </summary>
	public CanSpawnNearbyDelegate OnCanSpawnNearby;

	public EntityTalkUtil talkUtil;

	public AngleConstraint BodyYawLimits;

	public AngleConstraint HeadYawLimits;

	/// <summary>
	/// Used to assist if this EntityPlayer needs to be repartitioned
	/// </summary>
	public List<Entity> entityListForPartitioning;

	/// <summary>
	/// This is not walkspeed per se, it is the walkspeed modifier as a result of armor and other gear.  It corresponds to Stats.GetBlended("walkspeed") and gets updated every tick
	/// </summary>
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
				float range = GameMath.AngleRadDistance(BodyYawLimits.CenterRad, value);
				base.BodyYaw = BodyYawLimits.CenterRad + GameMath.Clamp(range, 0f - BodyYawLimits.RangeRad, BodyYawLimits.RangeRad);
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
			if (!WatchedAttributes.attributes.TryGetValue("lastReviveTotalHours", out var hrsAttr))
			{
				return -9999.0;
			}
			return (hrsAttr as DoubleAttribute).value;
		}
		set
		{
			WatchedAttributes.SetDouble("lastReviveTotalHours", value);
		}
	}

	public override bool StoreWithChunk => false;

	/// <summary>
	/// The player's internal Universal ID. Available on the client and the server.
	/// </summary>
	public string PlayerUID => WatchedAttributes.GetString("playerUID");

	/// <summary>
	/// The players right hand contents. Available on the client and the server.
	/// </summary>
	public override ItemSlot RightHandItemSlot => World.PlayerByUid(PlayerUID)?.InventoryManager.ActiveHotbarSlot;

	/// <summary>
	/// The playres left hand contents. Available on the client and the server.
	/// </summary>
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
			byte[] rightHsv = RightHandItemSlot?.Itemstack?.Collectible?.GetLightHsv(World.BlockAccessor, null, RightHandItemSlot.Itemstack);
			byte[] leftHsv = LeftHandItemSlot?.Itemstack?.Collectible?.GetLightHsv(World.BlockAccessor, null, LeftHandItemSlot.Itemstack);
			if ((rightHsv == null || rightHsv[2] == 0) && (leftHsv == null || leftHsv[2] == 0))
			{
				double hoursAlive = Api.World.Calendar.TotalHours - LastReviveTotalHours;
				if (hoursAlive < 2.0)
				{
					newSpawnGlow = true;
					base.Properties.Client.GlowLevel = (int)GameMath.Clamp(100.0 * (2.0 - hoursAlive), 0.0, 255.0);
				}
				if (hoursAlive < 1.5)
				{
					newSpawnGlow = true;
					return new byte[3]
					{
						33,
						7,
						(byte)Math.Min(10.0, 11.0 * (1.5 - hoursAlive))
					};
				}
			}
			else if (newSpawnGlow)
			{
				base.Properties.Client.GlowLevel = 0;
				newSpawnGlow = false;
			}
			if (rightHsv == null)
			{
				return leftHsv;
			}
			if (leftHsv == null)
			{
				return rightHsv;
			}
			float totalval = rightHsv[2] + leftHsv[2];
			float t = (float)(int)leftHsv[2] / totalval;
			return new byte[3]
			{
				(byte)((float)(int)leftHsv[0] * t + (float)(int)rightHsv[0] * (1f - t)),
				(byte)((float)(int)leftHsv[1] * t + (float)(int)rightHsv[1] * (1f - t)),
				Math.Max(leftHsv[2], rightHsv[2])
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
			IWorldPlayerData worldData = World?.PlayerByUid(PlayerUID)?.WorldData;
			if (worldData == null || worldData.CurrentGameMode != EnumGameMode.Spectator)
			{
				if (worldData == null)
				{
					return true;
				}
				return !worldData.NoClip;
			}
			return false;
		}
	}

	public override double LadderFixDelta => base.Properties.SpawnCollisionBox.Y2 - SelectionBox.YSize;

	/// <summary>
	/// The base player attached to this EntityPlayer.
	/// </summary>
	public IPlayer Player => World?.PlayerByUid(PlayerUID);

	private bool IsSelf => PlayerUID == (Api as ICoreClientAPI)?.Settings.String["playeruid"];

	public override IAnimationManager AnimManager
	{
		get
		{
			ICoreClientAPI capi = Api as ICoreClientAPI;
			if (IsSelf && capi.Render.CameraType == EnumCameraMode.FirstPerson && !selfNowShadowPass)
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
			ICoreClientAPI capi = Api as ICoreClientAPI;
			if (IsSelf && capi.Render.CameraType == EnumCameraMode.FirstPerson && !selfNowShadowPass)
			{
				return animManager;
			}
			return selfFpAnimManager;
		}
	}

	public PlayerAnimationManager TpAnimManager => animManager;

	public PlayerAnimationManager SelfFpAnimManager => selfFpAnimManager;

	public float HeadBobbingAmplitude { get; set; } = 1f;


	/// <summary>
	/// Set this to hook into the foot step sound creator thingy. Currently used by the armor system to create armor step sounds. Called by the game client and server.
	/// </summary>
	public event Action OnFootStep;

	/// <summary>
	/// Called when the player falls onto the ground. Called by the game client and server.
	/// </summary>
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
			double hrs = (((double)Api.World.Calendar.GetDayLightStrength(ServerPos.X, ServerPos.Z) < 0.5) ? Api.World.Calendar.TotalHours : (-9999.0));
			WatchedAttributes.SetDouble("lastReviveTotalHours", hrs);
		}
		if (IsSelf)
		{
			OtherAnimManager.Init(api, this);
		}
	}

	public override double GetWalkSpeedMultiplier(double groundDragFactor = 0.3)
	{
		double mul = base.GetWalkSpeedMultiplier(groundDragFactor);
		IPlayer player = Player;
		if (player != null && player.WorldData.CurrentGameMode == EnumGameMode.Creative)
		{
			int y1 = (int)(base.SidedPos.InternalY - 0.05000000074505806);
			int y2 = (int)(base.SidedPos.InternalY + 0.009999999776482582);
			Block belowBlock = World.BlockAccessor.GetBlockRaw((int)base.SidedPos.X, y1, (int)base.SidedPos.Z);
			mul /= (double)(belowBlock.WalkSpeedMultiplier * ((y1 == y2) ? 1f : insideBlock.WalkSpeedMultiplier));
		}
		mul *= (double)GameMath.Clamp(walkSpeed, 0f, 999f);
		if (!servercontrols.Sneak && !PrevFrameCanStandUp)
		{
			mul *= (double)GlobalConstants.SneakSpeedMultiplier;
		}
		return mul;
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
		EntityControls controls = ((base.MountedOn != null) ? base.MountedOn.Controls : servercontrols);
		PrevFrameCanStandUp = !controls.Sneak && canStandUp();
		int num;
		int num2;
		if (controls.TriesToMove && base.SidedPos.Motion.LengthSq() > 1E-05 && !controls.NoClip)
		{
			num = ((!controls.DetachedMode) ? 1 : 0);
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
		bool walking = (byte)num2 != 0;
		double newEyeheight = base.Properties.EyeHeight;
		double newModelHeight = base.Properties.CollisionBoxSize.Y;
		if (controls.FloorSitting)
		{
			newEyeheight *= 0.5;
			newModelHeight *= 0.550000011920929;
		}
		else if ((controls.Sneak || !PrevFrameCanStandUp) && !controls.IsClimbing && !controls.IsFlying)
		{
			newEyeheight *= 0.800000011920929;
			newModelHeight *= 0.800000011920929;
		}
		else if (!Alive)
		{
			newEyeheight *= 0.25;
			newModelHeight *= 0.25;
		}
		double diff = (newEyeheight - LocalEyePos.Y) * 5.0 * (double)dt;
		LocalEyePos.Y = ((diff > 0.0) ? Math.Min(LocalEyePos.Y + diff, newEyeheight) : Math.Max(LocalEyePos.Y + diff, newEyeheight));
		diff = (newModelHeight - (double)OriginSelectionBox.Y2) * 5.0 * (double)dt;
		OriginSelectionBox.Y2 = (SelectionBox.Y2 = (float)((diff > 0.0) ? Math.Min((double)SelectionBox.Y2 + diff, newModelHeight) : Math.Max((double)SelectionBox.Y2 + diff, newModelHeight)));
		diff = (newModelHeight - (double)OriginCollisionBox.Y2) * 5.0 * (double)dt;
		OriginCollisionBox.Y2 = (CollisionBox.Y2 = (float)((diff > 0.0) ? Math.Min((double)CollisionBox.Y2 + diff, newModelHeight) : Math.Max((double)CollisionBox.Y2 + diff, newModelHeight)));
		LocalEyePos.X = 0.0;
		LocalEyePos.Z = 0.0;
		bool skipIfpEyePos = false;
		if (base.MountedOn != null)
		{
			skipIfpEyePos = base.MountedOn.SuggestedAnimation?.Code == "sleep";
			if (base.MountedOn.LocalEyePos != null)
			{
				LocalEyePos.Set(base.MountedOn.LocalEyePos);
			}
		}
		if (player.ImmersiveFpMode && !skipIfpEyePos)
		{
			secondsDead = (Alive ? 0f : (secondsDead + dt));
			updateLocalEyePosImmersiveFpMode(dt);
		}
		double frequency = (double)(dt * controls.MovespeedMultiplier) * GetWalkSpeedMultiplier() * (controls.Sprint ? 0.9 : 1.2) * (double)(controls.Sneak ? 1.2f : 1f);
		walkCounter = (walking ? (walkCounter + frequency) : 0.0);
		walkCounter %= 6.2831854820251465;
		double sneakDiv = (controls.Sneak ? 5.0 : 1.8);
		double amplitude = (FeetInLiquid ? 0.8 : (1.0 + (controls.Sprint ? 0.07 : 0.0))) / (3.0 * sneakDiv) * (double)HeadBobbingAmplitude;
		double offset = -0.2 / sneakDiv;
		double stepHeight = 0.0 - Math.Max(0.0, Math.Abs(GameMath.Sin(5.5 * walkCounter) * amplitude) + offset);
		if (World.Side == EnumAppSide.Client)
		{
			ICoreClientAPI capi = World.Api as ICoreClientAPI;
			if (capi.Settings.Bool["viewBobbing"] && capi.Render.CameraType == EnumCameraMode.FirstPerson)
			{
				LocalEyePos.Y += stepHeight / 3.0 * (double)dt * 60.0;
			}
		}
		if (num != 0)
		{
			bool playingInsideSound = PlayInsideSound(player);
			if (walking)
			{
				if (stepHeight > prevStepHeight)
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
		prevStepHeight = stepHeight;
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
		BlockPos tmpPos = new BlockPos((int)Pos.X, (int)Pos.Y, (int)Pos.Z, Pos.Dimension);
		BlockSelection blockSel = new BlockSelection
		{
			Position = tmpPos,
			Face = null
		};
		AssetLocation soundinsideTorso = GetInsideTorsoBlockSoundSource(tmpPos)?.GetSounds(Api.World.BlockAccessor, blockSel).Inside;
		AssetLocation soundinsideLegs = GetInsideLegsBlockSoundSource(tmpPos)?.GetSounds(Api.World.BlockAccessor, blockSel).Inside;
		bool makingSound = false;
		if (soundinsideTorso != null)
		{
			if (Api.Side == EnumAppSide.Client)
			{
				long timeNow2 = Environment.TickCount;
				if (timeNow2 > lastInsideSoundTimeFinishTorso)
				{
					float volume2 = (controls.Sneak ? 0.25f : 1f);
					int duration2 = PlaySound(player, soundinsideTorso, 12, volume2, 1.4);
					lastInsideSoundTimeFinishTorso = timeNow2 + duration2 * 90 / 100;
				}
			}
			makingSound = true;
		}
		if (soundinsideLegs != null && soundinsideLegs != soundinsideTorso)
		{
			if (Api.Side == EnumAppSide.Client)
			{
				long timeNow = Environment.TickCount;
				if (timeNow > lastInsideSoundTimeFinishLegs)
				{
					float volume = (controls.Sneak ? 0.35f : 1f);
					int duration = PlaySound(player, soundinsideLegs, 12, volume, 0.6);
					lastInsideSoundTimeFinishLegs = timeNow + duration * 90 / 100;
				}
			}
			makingSound = true;
		}
		return makingSound;
	}

	public virtual void PlayStepSound(IPlayer player, bool playingInsideSound)
	{
		float volume = (controls.Sneak ? 0.5f : 1f);
		EntityPos pos = base.SidedPos;
		BlockPos tmpPos = new BlockPos((int)pos.X, (int)pos.Y, (int)pos.Z, pos.Dimension);
		BlockSelection blockSel = new BlockSelection
		{
			Position = tmpPos,
			Face = BlockFacing.UP
		};
		AssetLocation soundWalkLoc = GetNearestBlockSoundSource(tmpPos, -0.03, 4, usecollisionboxes: true)?.GetSounds(Api.World.BlockAccessor, blockSel)?.Walk ?? GetNearestBlockSoundSource(tmpPos, -0.7, 4, usecollisionboxes: true)?.GetSounds(Api.World.BlockAccessor, blockSel)?.Walk;
		tmpPos.Set((int)pos.X, (int)(pos.Y + 0.10000000149011612), (int)pos.Z);
		AssetLocation soundinsideliquid = World.BlockAccessor.GetBlock(tmpPos, 2).GetSounds(Api.World.BlockAccessor, blockSel)?.Inside;
		if (soundinsideliquid != null)
		{
			PlaySound(player, soundinsideliquid, 12, volume, 1.0);
		}
		if (!Swimming && soundWalkLoc != null)
		{
			PlaySound(player, soundWalkLoc, 12, playingInsideSound ? (volume * 0.5f) : volume, 0.0);
			this.OnFootStep?.Invoke();
		}
	}

	private int PlaySound(IPlayer player, AssetLocation sound, int range, float volume, double yOffset)
	{
		bool num = player.PlayerUID == (Api as ICoreClientAPI)?.World.Player?.PlayerUID;
		IServerPlayer srvplayer = player as IServerPlayer;
		double x = 0.0;
		double y = 0.0;
		double z = 0.0;
		if (!num)
		{
			x = Pos.X;
			y = Pos.InternalY + yOffset;
			z = Pos.Z;
		}
		if (Api.Side == EnumAppSide.Client)
		{
			return ((IClientWorldAccessor)World).PlaySoundAtAndGetDuration(sound, x, y, z, srvplayer, randomizePitch: true, range, volume);
		}
		World.PlaySoundAt(sound, x, y, z, srvplayer, randomizePitch: true, range, volume);
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
		bool isSelf = (Api as ICoreClientAPI)?.World.Player.PlayerUID == PlayerUID;
		if (Api.Side == EnumAppSide.Server || !isSelf)
		{
			updateEyeHeight(dt);
		}
		if (isSelf)
		{
			alwaysRunIdle = (Api as ICoreClientAPI).Render.CameraType == EnumCameraMode.FirstPerson && !selfNowShadowPass;
		}
		climateCondAccum += dt;
		if (isSelf && World.Side == EnumAppSide.Client && climateCondAccum > 0.5f)
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
		bool moving = (((Api as ICoreClientAPI).World.Player.Entity.EntityId == EntityId) ? Pos : ServerPos).Motion.LengthSq() > 1E-05 && !servercontrols.NoClip;
		if ((FeetInLiquid || Swimming) && moving && base.Properties.Habitat != EnumHabitat.Underwater)
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
		AttachmentPointAndPose apap = AnimManager.Animator.GetAttachmentPointPose("Eyes");
		bool wasHoldPos = holdPosition;
		holdPosition = false;
		for (int i = 0; i < AnimManager.Animator.Animations.Length; i++)
		{
			RunningAnimation anim = AnimManager.Animator.Animations[i];
			if (anim.Running && anim.EasingFactor >= anim.meta.HoldEyePosAfterEasein)
			{
				if (!wasHoldPos)
				{
					prevAnimModelMatrix = (float[])apap.AnimModelMatrix.Clone();
				}
				holdPosition = true;
				break;
			}
		}
		updateLocalEyePos(apap, holdPosition ? prevAnimModelMatrix : apap.AnimModelMatrix);
	}

	private void updateLocalEyePos(AttachmentPointAndPose apap, float[] animModelMatrix)
	{
		AttachmentPoint ap = apap.AttachPoint;
		float[] ModelMat = Mat4f.Create();
		float bodyYaw = BodyYaw;
		float rotX = ((base.Properties.Client.Shape != null) ? base.Properties.Client.Shape.rotateX : 0f);
		float rotY = ((base.Properties.Client.Shape != null) ? base.Properties.Client.Shape.rotateY : 0f);
		float rotZ = ((base.Properties.Client.Shape != null) ? base.Properties.Client.Shape.rotateZ : 0f);
		float bodyPitch = WalkPitch;
		float lookOffset = (base.SidedPos.Pitch - (float)Math.PI) / 9f;
		if (!Alive)
		{
			lookOffset /= secondsDead * 10f;
		}
		Matrixf tmpModelMat = new Matrixf();
		tmpModelMat.Set(ModelMat).RotateX(base.SidedPos.Roll + rotX * ((float)Math.PI / 180f)).RotateY(bodyYaw + (90f + rotY) * ((float)Math.PI / 180f))
			.RotateZ(bodyPitch + rotZ * ((float)Math.PI / 180f))
			.Scale(base.Properties.Client.Size, base.Properties.Client.Size, base.Properties.Client.Size)
			.Translate(-0.5f, 0f, -0.5f)
			.RotateX(sidewaysSwivelAngle)
			.Translate(ap.PosX / 16.0 - (double)(lookOffset * 1.3f), ap.PosY / 16.0, ap.PosZ / 16.0)
			.Mul(animModelMatrix)
			.Translate(0.07f, Alive ? 0f : (0.2f * Math.Min(1f, secondsDead)), 0f);
		float[] pos = new float[4] { 0f, 0f, 0f, 1f };
		float[] endVec = Mat4f.MulWithVec4(tmpModelMat.Values, pos);
		LocalEyePos.Set(endVec[0], endVec[1], endVec[2]);
	}

	public void HandleSeraphHandAnimations(float dt)
	{
		protectEyesFromWind(dt);
		if (Api is ICoreClientAPI capi && capi.World.Player.PlayerUID != PlayerUID)
		{
			return;
		}
		ItemStack rightstack = RightHandItemSlot?.Itemstack;
		if (RightHandItemSlot is ItemSlotSkill)
		{
			rightstack = null;
		}
		EnumHandInteract interact = servercontrols.HandUse;
		PlayerAnimationManager plrAnimMngr = AnimManager as PlayerAnimationManager;
		bool nowUseStack = interact == EnumHandInteract.BlockInteract || interact == EnumHandInteract.HeldItemInteract || (servercontrols.RightMouseDown && !servercontrols.LeftMouseDown);
		bool wasUseStack = plrAnimMngr.IsHeldUseActive();
		bool nowHitStack = interact == EnumHandInteract.HeldItemAttack || servercontrols.LeftMouseDown;
		bool wasHitStack = plrAnimMngr.IsHeldHitActive(1f);
		string nowHeldRightUseAnim = rightstack?.Collectible.GetHeldTpUseAnimation(RightHandItemSlot, this);
		string nowHeldRightHitAnim = rightstack?.Collectible.GetHeldTpHitAnimation(RightHandItemSlot, this);
		string nowHeldRightIdleAnim = rightstack?.Collectible.GetHeldTpIdleAnimation(RightHandItemSlot, this, EnumHand.Right);
		string nowHeldLeftIdleAnim = LeftHandItemSlot?.Itemstack?.Collectible.GetHeldTpIdleAnimation(LeftHandItemSlot, this, EnumHand.Left);
		string nowHeldRightReadyAnim = rightstack?.Collectible.GetHeldReadyAnimation(RightHandItemSlot, this, EnumHand.Right);
		bool shouldRightReadyStack = haveHandUseOrHit && !servercontrols.LeftMouseDown && !servercontrols.RightMouseDown && !plrAnimMngr.IsAnimationActiveOrRunning(plrAnimMngr.lastRunningHeldHitAnimation) && !plrAnimMngr.IsAnimationActiveOrRunning(plrAnimMngr.lastRunningHeldUseAnimation);
		bool isRightReadyStack = plrAnimMngr.IsRightHeldReadyActive();
		bool shouldRightIdleStack = nowHeldRightIdleAnim != null && !nowUseStack && !nowHitStack && !shouldRightReadyStack && !plrAnimMngr.IsAnimationActiveOrRunning(plrAnimMngr.lastRunningHeldHitAnimation) && !plrAnimMngr.IsAnimationActiveOrRunning(plrAnimMngr.lastRunningHeldUseAnimation);
		bool isRightIdleStack = plrAnimMngr.IsRightHeldActive();
		bool shouldLeftIdleStack = nowHeldLeftIdleAnim != null;
		bool isLeftIdleStack = plrAnimMngr.IsLeftHeldActive();
		if (rightstack == null)
		{
			nowHeldRightHitAnim = "breakhand";
			nowHeldRightUseAnim = "interactstatic";
			if (EntitySelection != null && EntitySelection.Entity.Pos.DistanceTo(Pos) <= 1.15)
			{
				IPettable @interface = EntitySelection.Entity.GetInterface<IPettable>();
				if (@interface == null || @interface.CanPet(this))
				{
					if ((double)EntitySelection.Entity.SelectionBox.Y2 > 0.8)
					{
						nowHeldRightUseAnim = "petlarge";
					}
					if ((double)EntitySelection.Entity.SelectionBox.Y2 <= 0.8 && controls.Sneak)
					{
						nowHeldRightUseAnim = "petsmall";
					}
					if (EntitySelection.Entity is EntityPlayer eplr && !eplr.controls.FloorSitting)
					{
						nowHeldRightUseAnim = "petseraph";
					}
				}
			}
		}
		if (shouldRightReadyStack && !isRightReadyStack)
		{
			plrAnimMngr.StopRightHeldIdleAnim();
			plrAnimMngr.StartHeldReadyAnim(nowHeldRightReadyAnim);
			haveHandUseOrHit = false;
		}
		if ((nowUseStack != wasUseStack || plrAnimMngr.HeldUseAnimChanged(nowHeldRightUseAnim)) && !nowHitStack)
		{
			plrAnimMngr.StopHeldUseAnim();
			if (nowUseStack)
			{
				plrAnimMngr.StartHeldUseAnim(nowHeldRightUseAnim);
				haveHandUseOrHit = true;
			}
		}
		if (nowHitStack != wasHitStack || plrAnimMngr.HeldHitAnimChanged(nowHeldRightHitAnim))
		{
			bool nowauthoritative = plrAnimMngr.IsAuthoritative(nowHeldRightHitAnim);
			bool curauthoritative = plrAnimMngr.IsHeldHitAuthoritative();
			if (!curauthoritative)
			{
				plrAnimMngr.StopHeldAttackAnim();
				plrAnimMngr.StopAnimation(plrAnimMngr.lastRunningHeldHitAnimation);
			}
			if (plrAnimMngr.lastRunningHeldHitAnimation != null && curauthoritative)
			{
				if (servercontrols.LeftMouseDown)
				{
					plrAnimMngr.ResetAnimation(nowHeldRightHitAnim);
					controls.HandUse = EnumHandInteract.None;
					plrAnimMngr.StartHeldHitAnim(nowHeldRightHitAnim);
					haveHandUseOrHit = true;
					RightHandItemSlot.Itemstack?.Collectible.OnHeldActionAnimStart(RightHandItemSlot, this, EnumHandInteract.HeldItemAttack);
				}
			}
			else
			{
				if (nowauthoritative)
				{
					nowHitStack = servercontrols.LeftMouseDown;
				}
				if (!curauthoritative && nowHitStack)
				{
					plrAnimMngr.StartHeldHitAnim(nowHeldRightHitAnim);
					haveHandUseOrHit = true;
					RightHandItemSlot.Itemstack?.Collectible.OnHeldActionAnimStart(RightHandItemSlot, this, EnumHandInteract.HeldItemAttack);
				}
			}
		}
		if (shouldRightIdleStack != isRightIdleStack || plrAnimMngr.RightHeldIdleChanged(nowHeldRightIdleAnim))
		{
			plrAnimMngr.StopRightHeldIdleAnim();
			if (shouldRightIdleStack)
			{
				plrAnimMngr.StartRightHeldIdleAnim(nowHeldRightIdleAnim);
			}
		}
		if (shouldLeftIdleStack != isLeftIdleStack || plrAnimMngr.LeftHeldIdleChanged(nowHeldLeftIdleAnim))
		{
			plrAnimMngr.StopLeftHeldIdleAnim();
			if (shouldLeftIdleStack)
			{
				plrAnimMngr.StartLeftHeldIdleAnim(nowHeldLeftIdleAnim);
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
		ClimateCondition climate = selfClimateCond;
		float val = ((climate == null) ? 0f : (GlobalConstants.CurrentWindSpeedClient.Length() * (1f - climate.WorldgenRainfall) * (1f - climate.Rainfall)));
		float discomfortColdSnowStorm = ((climate == null) ? 0f : (GlobalConstants.CurrentWindSpeedClient.Length() * climate.Rainfall * Math.Max(0f, (1f - climate.Temperature) / 5f)));
		float discomfortLevel = Math.Max(val, discomfortColdSnowStorm);
		strongWindAccum = (((double)discomfortLevel > 0.75 && !Swimming) ? (strongWindAccum + dt) : 0f);
		bool lookingIntoWind = Math.Abs(GameMath.AngleRadDistance((float)Math.Atan2(GlobalConstants.CurrentWindSpeedClient.X, GlobalConstants.CurrentWindSpeedClient.Z), Pos.Yaw - (float)Math.PI / 2f)) < (float)Math.PI / 4f;
		if (GlobalConstants.CurrentDistanceToRainfallClient < 6f && lookingIntoWind)
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
		bool collideSneaking = World.CollisionTester.IsColliding(World.BlockAccessor, tmpCollBox, Pos.XYZ, alsoCheckTouch: false);
		tmpCollBox.Y2 = base.Properties.CollisionBoxSize.Y;
		tmpCollBox.Y1 += 1f;
		return !World.CollisionTester.IsColliding(World.BlockAccessor, tmpCollBox, Pos.XYZ, alsoCheckTouch: false) || collideSneaking;
	}

	protected override bool onAnimControls(AnimationMetaData anim, bool wasActive, bool nowActive)
	{
		AnimationTrigger triggeredBy = anim.TriggeredBy;
		if (triggeredBy != null && triggeredBy.MatchExact && anim.Animation == "sitflooridle")
		{
			bool canDoEdgeSit = canPlayEdgeSitAnim();
			bool edgeSitActive = AnimManager.IsAnimationActive("sitidle");
			wasActive = wasActive || edgeSitActive;
			ICoreClientAPI capi = Api as ICoreClientAPI;
			if (nowActive)
			{
				bool floorSitActive = AnimManager.IsAnimationActive(anim.Code);
				if (canDoEdgeSit)
				{
					if (floorSitActive)
					{
						AnimManager.StopAnimation(anim.Animation);
					}
					if (!edgeSitActive)
					{
						AnimManager.StartAnimation("sitflooredge");
						capi.Network.SendEntityPacket(EntityId, 296, SerializerUtil.Serialize(1));
						BodyYaw = (float)Math.Round(BodyYaw * (180f / (float)Math.PI) / 90f) * 90f * ((float)Math.PI / 180f);
						BodyYawLimits = new AngleConstraint(BodyYaw, 0.2f);
						HeadYawLimits = new AngleConstraint(BodyYaw, (float)Math.PI * 19f / 40f);
					}
					return true;
				}
				if (edgeSitActive && !canDoEdgeSit && !floorSitActive)
				{
					AnimManager.StopAnimation("sitidle");
					capi.Network.SendEntityPacket(EntityId, 296, SerializerUtil.Serialize(0));
					BodyYawLimits = null;
					HeadYawLimits = null;
				}
			}
			else if (wasActive)
			{
				AnimManager.StopAnimation("sitidle");
				capi.Network.SendEntityPacket(EntityId, 296, SerializerUtil.Serialize(0));
				BodyYawLimits = null;
				HeadYawLimits = null;
			}
			return canDoEdgeSit;
		}
		return false;
	}

	protected bool canPlayEdgeSitAnim()
	{
		IBlockAccessor bl = Api.World.BlockAccessor;
		Vec3d pos = Pos.XYZ;
		float num = ((BodyYawLimits == null) ? Pos.Yaw : BodyYawLimits.CenterRad);
		float cosYaw = GameMath.Cos(num + (float)Math.PI / 2f);
		float sinYaw = GameMath.Sin(num + (float)Math.PI / 2f);
		BlockPos frontBelowPos = new Vec3d(Pos.X + (double)(sinYaw * 0.3f), Pos.Y - 1.0, Pos.Z + (double)(cosYaw * 0.3f)).AsBlockPos;
		Cuboidf[] frontBellowCollBoxes = bl.GetBlock(frontBelowPos).GetCollisionBoxes(bl, frontBelowPos);
		if (frontBellowCollBoxes == null || frontBellowCollBoxes.Length == 0)
		{
			return true;
		}
		return pos.Y - (double)((float)frontBelowPos.Y + frontBellowCollBoxes.Max((Cuboidf box) => box.Y2)) >= 0.45;
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
			EntityPos pos = base.SidedPos;
			BlockPos tmpPos = new BlockPos((int)pos.X, (int)(pos.Y - 0.10000000149011612), (int)pos.Z, pos.Dimension);
			BlockSelection blockSel = new BlockSelection
			{
				Position = tmpPos,
				Face = BlockFacing.UP
			};
			GetNearestBlockSoundSource(tmpPos, -0.1, 4, usecollisionboxes: true);
			AssetLocation soundWalkLoc = GetNearestBlockSoundSource(tmpPos, -0.1, 4, usecollisionboxes: true)?.GetSounds(Api.World.BlockAccessor, blockSel)?.Walk ?? GetNearestBlockSoundSource(tmpPos, -0.7, 4, usecollisionboxes: true)?.GetSounds(Api.World.BlockAccessor, blockSel)?.Walk;
			if (soundWalkLoc != null && !Swimming)
			{
				World.PlaySoundAt(soundWalkLoc, this, player, randomizePitch: true, 12f, 1.5f);
			}
			this.OnImpact?.Invoke(motionY);
		}
		base.OnFallToGround(motionY);
	}

	/// <summary>
	/// Returns null if there is no nearby sound source
	/// </summary>
	/// <param name="tmpPos">Might get intentionally modified if the nearest sound source the player is intersecting with is in an adjacent block</param>
	/// <param name="yOffset"></param>
	/// <param name="blockLayer"></param>
	/// <param name="usecollisionboxes"></param>
	/// <returns></returns>
	public Block GetNearestBlockSoundSource(BlockPos tmpPos, double yOffset, int blockLayer, bool usecollisionboxes)
	{
		EntityPos pos = base.SidedPos;
		Cuboidd entityBox = new Cuboidd();
		Cuboidf colBox = CollisionBox;
		entityBox.SetAndTranslate(colBox, pos.X, pos.Y + yOffset, pos.Z);
		entityBox.GrowBy(-0.001, 0.0, -0.001);
		int yo = (int)(pos.Y + yOffset);
		tmpPos.Set(pos.XInt, yo, pos.ZInt);
		BlockSelection blockSel = new BlockSelection
		{
			Position = tmpPos,
			Face = BlockFacing.DOWN
		};
		Block block = getSoundSourceBlockAt(entityBox, blockSel, blockLayer, usecollisionboxes);
		if (block != null)
		{
			return block;
		}
		double xdistToBlockCenter = GameMath.Mod(pos.X, 1.0) - 0.5;
		double zdistToBlockCenter = GameMath.Mod(pos.Z, 1.0) - 0.5;
		int adjacentX = pos.XInt + Math.Sign(xdistToBlockCenter);
		int adjacentZ = pos.ZInt + Math.Sign(zdistToBlockCenter);
		int nearerNeibX;
		int nearerNeibZ;
		int furtherNeibX;
		int furtherNeibZ;
		if (Math.Abs(xdistToBlockCenter) > Math.Abs(zdistToBlockCenter))
		{
			nearerNeibX = adjacentX;
			nearerNeibZ = pos.ZInt;
			furtherNeibX = pos.XInt;
			furtherNeibZ = adjacentZ;
		}
		else
		{
			nearerNeibX = pos.XInt;
			nearerNeibZ = adjacentZ;
			furtherNeibX = adjacentX;
			furtherNeibZ = pos.ZInt;
		}
		return getSoundSourceBlockAt(entityBox, blockSel.SetPos(nearerNeibX, yo, nearerNeibZ), blockLayer, usecollisionboxes) ?? getSoundSourceBlockAt(entityBox, blockSel.SetPos(furtherNeibX, yo, furtherNeibZ), blockLayer, usecollisionboxes) ?? getSoundSourceBlockAt(entityBox, blockSel.SetPos(adjacentX, yo, adjacentZ), blockLayer, usecollisionboxes);
	}

	protected Block getSoundSourceBlockAt(Cuboidd entityBox, BlockSelection blockSel, int blockLayer, bool usecollisionboxes)
	{
		Block block = World.BlockAccessor.GetBlock(blockSel.Position, blockLayer);
		if (!usecollisionboxes && block.GetSounds(Api.World.BlockAccessor, blockSel)?.Inside == null)
		{
			return null;
		}
		Cuboidf[] blockBoxes = (usecollisionboxes ? block.GetCollisionBoxes(World.BlockAccessor, blockSel.Position) : block.GetSelectionBoxes(World.BlockAccessor, blockSel.Position));
		if (blockBoxes == null)
		{
			return null;
		}
		foreach (Cuboidf blockBox in blockBoxes)
		{
			if (blockBox != null && entityBox.Intersects(blockBox, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z))
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
			string animation = SerializerUtil.Deserialize<string>(data);
			StartAnimation(animation);
			if (talkTypeByAnimation.TryGetValue(animation, out var talktype))
			{
				talkUtil?.Talk(talktype);
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
			EnumTalkType tt = SerializerUtil.Deserialize<EnumTalkType>(data);
			if (tt != EnumTalkType.Death && !Alive)
			{
				return;
			}
			talkUtil.Talk(tt);
		}
		if (packetid == 200)
		{
			AnimManager.StartAnimation(SerializerUtil.Deserialize<string>(data));
		}
		if (packetid == 1 && base.MountedOn?.Entity != null)
		{
			Entity mountentity = base.MountedOn.Entity;
			Vec3d newPos = SerializerUtil.Deserialize<Vec3d>(data);
			if ((Api as ICoreClientAPI).World.Player.Entity.EntityId == EntityId)
			{
				mountentity.Pos.SetPosWithDimension(newPos);
			}
			mountentity.ServerPos.SetPosWithDimension(newPos);
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
		EnumGameMode mode = (World?.PlayerByUid(PlayerUID)?.WorldData?.CurrentGameMode).GetValueOrDefault(EnumGameMode.Survival);
		if ((mode == EnumGameMode.Creative || mode == EnumGameMode.Spectator) && (damageSource == null || damageSource.Type != EnumDamageType.Heal))
		{
			return false;
		}
		return base.ShouldReceiveDamage(damageSource, damage);
	}

	public override void Ignite()
	{
		EnumGameMode mode = (World?.PlayerByUid(PlayerUID)?.WorldData?.CurrentGameMode).GetValueOrDefault(EnumGameMode.Survival);
		if (mode != EnumGameMode.Creative && mode != EnumGameMode.Spectator)
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
			bool heal = damageSource.Type == EnumDamageType.Heal;
			string damageTypeLocalised = Lang.Get("damagetype-" + damageSource.Type.ToString().ToLowerInvariant());
			string msg = ((damageSource.Type != EnumDamageType.BluntAttack && damageSource.Type != EnumDamageType.PiercingAttack && damageSource.Type != EnumDamageType.SlashingAttack) ? Lang.Get(heal ? "damagelog-heal" : "damagelog-damage", damage, damageTypeLocalised) : Lang.Get(heal ? "damagelog-heal-attack" : "damagelog-damage-attack", damage, damageTypeLocalised, damageSource.Source));
			if (damageSource.Source == EnumDamageSource.Player)
			{
				EntityPlayer eplr = damageSource.GetCauseEntity() as EntityPlayer;
				msg = Lang.Get(heal ? "damagelog-heal-byplayer" : "damagelog-damage-byplayer", damage, World.PlayerByUid(eplr.PlayerUID).PlayerName);
			}
			if (damageSource.Source == EnumDamageSource.Entity)
			{
				string creatureName = Lang.Get("prefixandcreature-" + damageSource.GetCauseEntity().Code.Path.Replace("-", ""));
				msg = Lang.Get(heal ? "damagelog-heal-byentity" : "damagelog-damage-byentity", damage, creatureName);
			}
			(World.PlayerByUid(PlayerUID) as IServerPlayer).SendMessage(GlobalConstants.DamageLogChatGroup, msg, EnumChatType.Notification);
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
		foreach (InventoryBase inv in player.InventoryManager.InventoriesOrdered)
		{
			if (inv.ClassName == "creative" || !inv.HasOpened(player))
			{
				continue;
			}
			int q = inv.Count;
			for (int i = 0; i < q; i++)
			{
				if (!handler(inv[i]))
				{
					return;
				}
			}
		}
	}

	/// <summary>
	/// Sets the current player.
	/// </summary>
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
		int oldZ = ServerPos.ZInt;
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
		if (((double)(xInt / 32) != x / 32.0 || (double)(oldZ / 32) != z / 32.0) && player.ClientId != 0)
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
		StringBuilder sb = new StringBuilder();
		if (!Alive)
		{
			sb.AppendLine(Lang.Get(Code.Domain + ":item-dead-creature-" + Code.Path));
		}
		else
		{
			sb.AppendLine(Lang.Get(Code.Domain + ":item-creature-" + Code.Path));
		}
		string charClass = WatchedAttributes.GetString("characterClass");
		if (Lang.HasTranslation("characterclass-" + charClass))
		{
			sb.AppendLine(Lang.Get("characterclass-" + charClass));
		}
		return sb.ToString();
	}

	public override void FromBytes(BinaryReader reader, bool forClient)
	{
		base.FromBytes(reader, forClient);
		walkSpeed = Stats.GetBlended("walkspeed");
	}

	public override void UpdateDebugAttributes()
	{
		base.UpdateDebugAttributes();
		string anims = "";
		int i = 0;
		foreach (string anim2 in OtherAnimManager.ActiveAnimationsByAnimCode.Keys)
		{
			if (i++ > 0)
			{
				anims += ",";
			}
			anims += anim2;
		}
		i = 0;
		StringBuilder runninganims = new StringBuilder();
		if (OtherAnimManager.Animator == null)
		{
			return;
		}
		RunningAnimation[] animations = OtherAnimManager.Animator.Animations;
		foreach (RunningAnimation anim in animations)
		{
			if (anim.Active)
			{
				if (i++ > 0)
				{
					runninganims.Append(",");
				}
				runninganims.Append(anim.Animation.Code);
			}
		}
		DebugAttributes.SetString("Other Active Animations", (anims.Length > 0) ? anims : "-");
		DebugAttributes.SetString("Other Running Animations", (runninganims.Length > 0) ? runninganims.ToString() : "-");
	}

	public void ChangeDimension(int dim)
	{
		_ = InChunkIndex3d;
		Pos.Dimension = dim;
		ServerPos.Dimension = dim;
		Api.Event.TriggerPlayerDimensionChanged(Player);
		long newchunkindex3d = Api.World.ChunkProvider.ChunkIndex3D(Pos);
		Api.World.UpdateEntityChunk(this, newchunkindex3d);
	}

	public bool CanPet(Entity byEntity)
	{
		return true;
	}
}
