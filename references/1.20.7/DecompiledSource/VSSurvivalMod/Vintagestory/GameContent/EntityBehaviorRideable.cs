using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityBehaviorRideable : EntityBehaviorSeatable, IMountable, IRenderer, IDisposable, IMountableListener
{
	public double ForwardSpeed;

	public double AngularVelocity;

	public bool ShouldSprint;

	public bool IsInMidJump;

	protected ICoreAPI api;

	protected float coyoteTimer;

	protected long lastJumpMs;

	protected bool jumpNow;

	protected EntityAgent eagent;

	protected RideableConfig rideableconfig;

	protected ILoadedSound trotSound;

	protected ILoadedSound gallopSound;

	protected ICoreClientAPI capi;

	private ControlMeta curControlMeta;

	private bool shouldMove;

	public AnimationMetaData curAnim;

	private string curTurnAnim;

	private EnumControlScheme scheme;

	private bool wasPaused;

	private bool prevForwardKey;

	private bool prevBackwardKey;

	private bool prevSprintKey;

	private bool forward;

	private bool backward;

	private bool sprint;

	private float notOnGroundAccum;

	public Vec3f MountAngle { get; set; } = new Vec3f();


	public EntityPos SeatPosition => entity.SidedPos;

	public double RenderOrder => 1.0;

	public int RenderRange => 100;

	public virtual float SpeedMultiplier => 1f;

	public Entity Mount => entity;

	public double lastDismountTotalHours
	{
		get
		{
			return entity.WatchedAttributes.GetDouble("lastDismountTotalHours");
		}
		set
		{
			entity.WatchedAttributes.SetDouble("lastDismountTotalHours", value);
		}
	}

	public event CanRideDelegate CanRide;

	public event CanRideDelegate CanTurn;

	public EntityBehaviorRideable(Entity entity)
		: base(entity)
	{
		eagent = entity as EntityAgent;
	}

	protected override IMountableSeat CreateSeat(string seatId, SeatConfig config)
	{
		return new EntityRideableSeat(this, seatId, config);
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
		rideableconfig = attributes.AsObject<RideableConfig>();
		foreach (ControlMeta value in rideableconfig.Controls.Values)
		{
			value.RiderAnim?.Init();
		}
		api = entity.Api;
		capi = api as ICoreClientAPI;
		curAnim = rideableconfig.Controls["idle"].RiderAnim;
		if (capi != null)
		{
			capi.Event.RegisterRenderer(this, EnumRenderStage.Before, "rideablesim");
		}
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		base.OnEntityDespawn(despawn);
		capi?.Event.UnregisterRenderer(this, EnumRenderStage.Before);
	}

	public void UnmnountPassengers()
	{
		IMountableSeat[] seats = base.Seats;
		for (int i = 0; i < seats.Length; i++)
		{
			(seats[i].Passenger as EntityAgent)?.TryUnmount();
		}
	}

	public override void OnEntityLoaded()
	{
		setupTaskBlocker();
	}

	public override void OnEntitySpawn()
	{
		setupTaskBlocker();
	}

	private void setupTaskBlocker()
	{
		EntityBehaviorAttachable ebc = entity.GetBehavior<EntityBehaviorAttachable>();
		if (api.Side == EnumAppSide.Server)
		{
			entity.GetBehavior<EntityBehaviorTaskAI>().TaskManager.OnShouldExecuteTask += TaskManager_OnShouldExecuteTask;
			if (ebc != null)
			{
				ebc.Inventory.SlotModified += Inventory_SlotModified;
			}
		}
		else if (ebc != null)
		{
			entity.WatchedAttributes.RegisterModifiedListener(ebc.InventoryClassName, updateControlScheme);
		}
	}

	private void Inventory_SlotModified(int obj)
	{
		updateControlScheme();
	}

	private void updateControlScheme()
	{
		EntityBehaviorAttachable ebc = entity.GetBehavior<EntityBehaviorAttachable>();
		if (ebc == null)
		{
			return;
		}
		scheme = EnumControlScheme.Hold;
		foreach (ItemSlot slot in ebc.Inventory)
		{
			if (slot.Empty)
			{
				continue;
			}
			string sch = slot.Itemstack.ItemAttributes?["controlScheme"].AsString();
			if (sch != null)
			{
				if (Enum.TryParse<EnumControlScheme>(sch, out scheme))
				{
					break;
				}
				scheme = EnumControlScheme.Hold;
			}
		}
	}

	private bool TaskManager_OnShouldExecuteTask(IAiTask task)
	{
		if (task is AiTaskWander && api.World.Calendar.TotalHours - lastDismountTotalHours < 24.0)
		{
			return false;
		}
		return !base.Seats.Any((IMountableSeat seat) => seat.Passenger != null);
	}

	public void OnRenderFrame(float dt, EnumRenderStage stage)
	{
		if (!wasPaused && capi.IsGamePaused)
		{
			trotSound?.Pause();
			gallopSound?.Pause();
		}
		if (wasPaused && !capi.IsGamePaused)
		{
			ILoadedSound loadedSound = trotSound;
			if (loadedSound != null && loadedSound.IsPaused)
			{
				trotSound?.Start();
			}
			ILoadedSound loadedSound2 = gallopSound;
			if (loadedSound2 != null && loadedSound2.IsPaused)
			{
				gallopSound?.Start();
			}
		}
		wasPaused = capi.IsGamePaused;
		if (!capi.IsGamePaused)
		{
			updateAngleAndMotion(dt);
		}
	}

	protected virtual void updateAngleAndMotion(float dt)
	{
		dt = Math.Min(0.5f, dt);
		float step = GlobalConstants.PhysicsFrameTime;
		Vec2d motion = SeatsToMotion(step);
		if (jumpNow)
		{
			updateRidingState();
		}
		ForwardSpeed = Math.Sign(motion.X);
		AngularVelocity = motion.Y * 1.5;
		if (!eagent.Controls.Sprint)
		{
			AngularVelocity *= 3.0;
		}
		entity.SidedPos.Yaw += (float)motion.Y * dt * 30f;
		entity.SidedPos.Yaw = entity.SidedPos.Yaw % ((float)Math.PI * 2f);
		if (entity.World.ElapsedMilliseconds - lastJumpMs < 2000 && entity.World.ElapsedMilliseconds - lastJumpMs > 200 && entity.OnGround)
		{
			eagent.StopAnimation("jump");
		}
	}

	public virtual Vec2d SeatsToMotion(float dt)
	{
		int seatsRowing = 0;
		double linearMotion = 0.0;
		double angularMotion = 0.0;
		jumpNow = false;
		coyoteTimer -= dt;
		bool shouldSprint = false;
		base.Controller = null;
		IMountableSeat[] seats = base.Seats;
		foreach (IMountableSeat seat in seats)
		{
			if (entity.OnGround)
			{
				coyoteTimer = 0.15f;
			}
			if (seat.Passenger == null || !seat.Config.Controllable)
			{
				continue;
			}
			if (seat.Passenger is EntityPlayer eplr)
			{
				eplr.Controls.LeftMouseDown = seat.Controls.LeftMouseDown;
				eplr.HeadYawLimits = new AngleConstraint(entity.Pos.Yaw + seat.Config.MountRotation.Y * ((float)Math.PI / 180f), (float)Math.PI / 2f);
				eplr.BodyYawLimits = new AngleConstraint(entity.Pos.Yaw + seat.Config.MountRotation.Y * ((float)Math.PI / 180f), (float)Math.PI / 2f);
			}
			if (base.Controller != null)
			{
				continue;
			}
			base.Controller = seat.Passenger;
			EntityControls controls = seat.Controls;
			bool canride = true;
			bool canturn = true;
			if (this.CanRide != null && (controls.Jump || controls.TriesToMove))
			{
				Delegate[] invocationList = this.CanRide.GetInvocationList();
				for (int j = 0; j < invocationList.Length; j++)
				{
					if (!((CanRideDelegate)invocationList[j])(seat, out var errMsg2))
					{
						if (capi != null && seat.Passenger == capi.World.Player.Entity)
						{
							capi.TriggerIngameError(this, "cantride", Lang.Get("cantride-" + errMsg2));
						}
						canride = false;
						break;
					}
				}
			}
			if (this.CanTurn != null && (controls.Left || controls.Right))
			{
				Delegate[] invocationList = this.CanTurn.GetInvocationList();
				for (int j = 0; j < invocationList.Length; j++)
				{
					if (!((CanRideDelegate)invocationList[j])(seat, out var errMsg))
					{
						if (capi != null && seat.Passenger == capi.World.Player.Entity)
						{
							capi.TriggerIngameError(this, "cantride", Lang.Get("cantride-" + errMsg));
						}
						canturn = false;
						break;
					}
				}
			}
			if (!canride)
			{
				continue;
			}
			if (controls.Jump && entity.World.ElapsedMilliseconds - lastJumpMs > 1500 && entity.Alive && (entity.OnGround || coyoteTimer > 0f))
			{
				lastJumpMs = entity.World.ElapsedMilliseconds;
				jumpNow = true;
			}
			if (scheme == EnumControlScheme.Hold && !controls.TriesToMove)
			{
				continue;
			}
			float str = ((++seatsRowing == 1) ? 1f : 0.5f);
			if (scheme == EnumControlScheme.Hold)
			{
				forward = controls.Forward;
				backward = controls.Backward;
				shouldSprint |= controls.Sprint && !entity.Swimming;
			}
			else
			{
				bool nowForwards = controls.Forward;
				bool nowBackwards = controls.Backward;
				bool nowSprint = controls.Sprint;
				if (!forward && !backward && nowForwards && !prevForwardKey)
				{
					forward = true;
				}
				else if (forward && nowBackwards && !prevBackwardKey)
				{
					forward = false;
					sprint = false;
				}
				else if (!backward && nowBackwards && !prevBackwardKey)
				{
					backward = true;
					sprint = false;
				}
				else if (backward && nowForwards && !prevForwardKey)
				{
					backward = false;
				}
				if (nowSprint && !prevSprintKey && !sprint)
				{
					sprint = true;
				}
				else if (nowSprint && !prevSprintKey && sprint)
				{
					sprint = false;
				}
				prevForwardKey = nowForwards;
				prevBackwardKey = nowBackwards;
				prevSprintKey = nowSprint;
				shouldSprint = sprint && !entity.Swimming;
			}
			if (canturn && (controls.Left || controls.Right))
			{
				float dir2 = (controls.Left ? 1 : (-1));
				angularMotion += (double)(str * dir2 * dt);
			}
			if (forward || backward)
			{
				float dir = (forward ? 1 : (-1));
				linearMotion += (double)(str * dir * dt * 2f);
			}
		}
		ShouldSprint = shouldSprint;
		return new Vec2d(linearMotion, angularMotion);
	}

	protected void updateRidingState()
	{
		if (!AnyMounted())
		{
			return;
		}
		bool isInMidJump = IsInMidJump;
		IsInMidJump &= (entity.World.ElapsedMilliseconds - lastJumpMs < 500 || !entity.OnGround) && !entity.Swimming;
		if (isInMidJump && !IsInMidJump)
		{
			ControlMeta meta = rideableconfig.Controls["jump"];
			IMountableSeat[] seats = base.Seats;
			for (int i = 0; i < seats.Length; i++)
			{
				seats[i].Passenger?.AnimManager?.StopAnimation(meta.RiderAnim.Animation);
			}
			eagent.AnimManager.StopAnimation(meta.Animation);
		}
		eagent.Controls.Backward = ForwardSpeed < 0.0;
		eagent.Controls.Forward = ForwardSpeed >= 0.0;
		eagent.Controls.Sprint = ShouldSprint && ForwardSpeed > 0.0;
		string nowTurnAnim = null;
		if (ForwardSpeed >= 0.0)
		{
			if (AngularVelocity > 0.001)
			{
				nowTurnAnim = "turn-left";
			}
			else if (AngularVelocity < -0.001)
			{
				nowTurnAnim = "turn-right";
			}
		}
		if (nowTurnAnim != curTurnAnim)
		{
			if (curTurnAnim != null)
			{
				eagent.StopAnimation(curTurnAnim);
			}
			eagent.StartAnimation(((ForwardSpeed == 0.0) ? "idle-" : "") + (curTurnAnim = nowTurnAnim));
		}
		shouldMove = ForwardSpeed != 0.0;
		ControlMeta nowControlMeta;
		if (!shouldMove && !jumpNow)
		{
			if (curControlMeta != null)
			{
				Stop();
			}
			curAnim = rideableconfig.Controls[eagent.Swimming ? "swim" : "idle"].RiderAnim;
			nowControlMeta = ((!eagent.Swimming) ? null : rideableconfig.Controls["swim"]);
		}
		else
		{
			string controlCode = (eagent.Controls.Backward ? "walkback" : "walk");
			if (eagent.Controls.Sprint)
			{
				controlCode = "sprint";
			}
			if (eagent.Swimming)
			{
				controlCode = "swim";
			}
			nowControlMeta = rideableconfig.Controls[controlCode];
			eagent.Controls.Jump = jumpNow;
			if (jumpNow)
			{
				IsInMidJump = true;
				jumpNow = false;
				if (eagent.Properties.Client.Renderer is EntityShapeRenderer esr)
				{
					esr.LastJumpMs = capi.InWorldEllapsedMilliseconds;
				}
				nowControlMeta = rideableconfig.Controls["jump"];
				nowControlMeta.EaseOutSpeed = ((ForwardSpeed != 0.0) ? 30 : 40);
				IMountableSeat[] seats = base.Seats;
				for (int i = 0; i < seats.Length; i++)
				{
					seats[i].Passenger?.AnimManager?.StartAnimation(nowControlMeta.RiderAnim);
				}
				IPlayer player = ((entity is EntityPlayer entityPlayer) ? entityPlayer.World.PlayerByUid(entityPlayer.PlayerUID) : null);
				entity.PlayEntitySound("jump", player, randomizePitch: false);
			}
			else
			{
				curAnim = nowControlMeta.RiderAnim;
			}
		}
		if (nowControlMeta != curControlMeta)
		{
			if (curControlMeta != null && curControlMeta.Animation != "jump")
			{
				eagent.StopAnimation(curControlMeta.Animation);
			}
			curControlMeta = nowControlMeta;
			eagent.AnimManager.StartAnimation(nowControlMeta);
		}
		if (api.Side == EnumAppSide.Server)
		{
			eagent.Controls.Sprint = false;
		}
	}

	public void Stop()
	{
		eagent.Controls.StopAllMovement();
		eagent.Controls.WalkVector.Set(0.0, 0.0, 0.0);
		eagent.Controls.FlyVector.Set(0.0, 0.0, 0.0);
		shouldMove = false;
		if (curControlMeta != null && curControlMeta.Animation != "jump")
		{
			eagent.StopAnimation(curControlMeta.Animation);
		}
		curControlMeta = null;
		eagent.StartAnimation("idle");
	}

	public override void OnGameTick(float dt)
	{
		if (api.Side == EnumAppSide.Server)
		{
			updateAngleAndMotion(dt);
		}
		updateRidingState();
		if (!AnyMounted() && eagent.Controls.TriesToMove && eagent?.MountedOn != null)
		{
			eagent.TryUnmount();
		}
		if (shouldMove)
		{
			move(dt, eagent.Controls, curControlMeta.MoveSpeed);
		}
		else if (entity.Swimming)
		{
			eagent.Controls.FlyVector.Y = 0.2;
		}
		updateSoundState(dt);
	}

	private void updateSoundState(float dt)
	{
		if (capi == null)
		{
			return;
		}
		if (eagent.OnGround)
		{
			notOnGroundAccum = 0f;
		}
		else
		{
			notOnGroundAccum += dt;
		}
		bool nowtrot = shouldMove && !eagent.Controls.Sprint && (double)notOnGroundAccum < 0.2;
		bool nowgallop = shouldMove && eagent.Controls.Sprint && (double)notOnGroundAccum < 0.2;
		bool wastrot = trotSound != null && trotSound.IsPlaying;
		bool wasgallop = gallopSound != null && gallopSound.IsPlaying;
		trotSound?.SetPosition((float)entity.Pos.X, (float)entity.Pos.Y, (float)entity.Pos.Z);
		gallopSound?.SetPosition((float)entity.Pos.X, (float)entity.Pos.Y, (float)entity.Pos.Z);
		if (nowtrot != wastrot)
		{
			if (nowtrot)
			{
				if (trotSound == null)
				{
					trotSound = capi.World.LoadSound(new SoundParams
					{
						Location = new AssetLocation("sounds/creature/hooved/trot"),
						DisposeOnFinish = false,
						Position = entity.Pos.XYZ.ToVec3f(),
						ShouldLoop = true
					});
				}
				trotSound.Start();
			}
			else
			{
				trotSound.Stop();
			}
		}
		if (nowgallop == wasgallop)
		{
			return;
		}
		if (nowgallop)
		{
			if (gallopSound == null)
			{
				gallopSound = capi.World.LoadSound(new SoundParams
				{
					Location = new AssetLocation("sounds/creature/hooved/gallop"),
					DisposeOnFinish = false,
					Position = entity.Pos.XYZ.ToVec3f(),
					ShouldLoop = true
				});
			}
			gallopSound.Start();
		}
		else
		{
			gallopSound.Stop();
		}
	}

	private void move(float dt, EntityControls controls, float nowMoveSpeed)
	{
		double cosYaw = Math.Cos(entity.Pos.Yaw);
		double sinYaw = Math.Sin(entity.Pos.Yaw);
		controls.WalkVector.Set(sinYaw, 0.0, cosYaw);
		controls.WalkVector.Mul((double)(nowMoveSpeed * GlobalConstants.OverallSpeedMultiplier) * ForwardSpeed);
		if (entity.Properties.RotateModelOnClimb && controls.IsClimbing && entity.ClimbingOnFace != null && entity.Alive)
		{
			BlockFacing climbingOnFace = entity.ClimbingOnFace;
			if (Math.Sign(climbingOnFace.Normali.X) == Math.Sign(controls.WalkVector.X))
			{
				controls.WalkVector.X = 0.0;
			}
			if (Math.Sign(climbingOnFace.Normali.Z) == Math.Sign(controls.WalkVector.Z))
			{
				controls.WalkVector.Z = 0.0;
			}
		}
		if (entity.Swimming)
		{
			controls.FlyVector.Set(controls.WalkVector);
			Vec3d pos = entity.Pos.XYZ;
			Block inblock = entity.World.BlockAccessor.GetBlock((int)pos.X, (int)pos.Y, (int)pos.Z, 2);
			Block aboveblock = entity.World.BlockAccessor.GetBlock((int)pos.X, (int)(pos.Y + 1.0), (int)pos.Z, 2);
			float swimlineSubmergedness = GameMath.Clamp((float)(int)pos.Y + (float)inblock.LiquidLevel / 8f + (aboveblock.IsLiquid() ? 1.125f : 0f) - (float)pos.Y - (float)entity.SwimmingOffsetY, 0f, 1f);
			swimlineSubmergedness = Math.Min(1f, swimlineSubmergedness + 0.075f);
			controls.FlyVector.Y = GameMath.Clamp(controls.FlyVector.Y, 0.0020000000949949026, 0.004000000189989805) * (double)swimlineSubmergedness * 3.0;
			if (entity.CollidedHorizontally)
			{
				controls.FlyVector.Y = 0.05000000074505806;
			}
			eagent.Pos.Motion.Y += ((double)swimlineSubmergedness - 0.1) / 300.0;
		}
	}

	public override string PropertyName()
	{
		return "rideable";
	}

	public void Dispose()
	{
	}

	public void DidUnnmount(EntityAgent entityAgent)
	{
		Stop();
		lastDismountTotalHours = entity.World.Calendar.TotalHours;
		foreach (ControlMeta meta in rideableconfig.Controls.Values)
		{
			if (meta.RiderAnim?.Animation != null)
			{
				entityAgent.StopAnimation(meta.RiderAnim.Animation);
			}
		}
		if (eagent.Swimming)
		{
			eagent.StartAnimation("swim");
		}
	}

	public void DidMount(EntityAgent entityAgent)
	{
		updateControlScheme();
	}
}
