using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public abstract class EntitySeat : IMountableSeat
{
	public EntityControls controls = new EntityControls();

	private string SeatIdForInit;

	protected IMountable mountedEntity;

	protected Vec3f eyePos = new Vec3f(0f, 1f, 0f);

	protected SeatConfig config;

	public Entity Passenger { get; set; }

	public long PassengerEntityIdForInit { get; set; }

	public bool DoTeleportOnUnmount { get; set; } = true;


	public string SeatId
	{
		get
		{
			return config?.SeatId ?? SeatIdForInit;
		}
		set
		{
			if (config != null)
			{
				config.SeatId = value;
			}
			SeatIdForInit = value;
		}
	}

	public virtual SeatConfig Config
	{
		get
		{
			return config;
		}
		set
		{
			config = value;
			if (config != null)
			{
				SeatIdForInit = config.SeatId;
			}
		}
	}

	public abstract AnimationMetaData SuggestedAnimation { get; }

	public EntityControls Controls => controls;

	public IMountable MountSupplier => mountedEntity;

	public virtual EnumMountAngleMode AngleMode => EnumMountAngleMode.Push;

	public virtual Vec3f LocalEyePos => eyePos;

	public bool CanControl => Config?.Controllable ?? false;

	public virtual Entity Entity => (mountedEntity as EntityBehaviorSeatable).entity;

	public abstract EntityPos SeatPosition { get; }

	public abstract Matrixf RenderTransform { get; }

	public bool SkipIdleAnimation => true;

	public abstract float FpHandPitchFollow { get; }

	public EntitySeat(IMountable mountedEntity, string seatId, SeatConfig config)
	{
		controls.OnAction = onControls;
		this.mountedEntity = mountedEntity;
		this.config = config;
		SeatId = seatId;
	}

	public virtual bool CanMount(EntityAgent entityAgent)
	{
		return true;
	}

	public virtual bool CanUnmount(EntityAgent entityAgent)
	{
		return true;
	}

	public virtual void DidMount(EntityAgent entityAgent)
	{
		if (Passenger != null && Passenger != entityAgent)
		{
			(Passenger as EntityAgent)?.TryUnmount();
		}
		else
		{
			Passenger = entityAgent;
		}
	}

	public virtual void DidUnmount(EntityAgent entityAgent)
	{
		if (Passenger != null)
		{
			if (Passenger.Properties?.Client.Renderer is EntityShapeRenderer pesr)
			{
				pesr.xangle = 0f;
				pesr.yangle = 0f;
				pesr.zangle = 0f;
			}
			Passenger.Pos.Roll = 0f;
		}
		Passenger = null;
	}

	public virtual void MountableToTreeAttributes(TreeAttribute tree)
	{
		tree.SetString("seatId", SeatId);
	}

	internal void onControls(EnumEntityAction action, bool on, ref EnumHandling handled)
	{
		if (action == EnumEntityAction.Sneak && on)
		{
			(Passenger as EntityAgent)?.TryUnmount();
			controls.StopAllMovement();
		}
	}
}
