using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityBed : BlockEntity, IMountableSeat, IMountable
{
	private long restingListener;

	private static Vec3f eyePos = new Vec3f(0f, 0.3f, 0f);

	private float sleepEfficiency = 0.5f;

	private BlockFacing facing;

	private float y2 = 0.5f;

	private double hoursTotal;

	public EntityAgent MountedBy;

	private bool blockBroken;

	private long mountedByEntityId;

	private string mountedByPlayerUid;

	private EntityControls controls = new EntityControls();

	private EntityPos mountPos = new EntityPos();

	private AnimationMetaData meta = new AnimationMetaData
	{
		Code = "sleep",
		Animation = "lie"
	}.Init();

	public bool DoTeleportOnUnmount { get; set; } = true;


	public EntityPos SeatPosition => Position;

	public EntityPos Position
	{
		get
		{
			BlockFacing facing = this.facing.Opposite;
			mountPos.SetPos(Pos);
			mountPos.Yaw = (float)this.facing.HorizontalAngleIndex * ((float)Math.PI / 2f) + (float)Math.PI / 2f;
			if (facing == BlockFacing.NORTH)
			{
				return mountPos.Add(0.5, y2, 1.0);
			}
			if (facing == BlockFacing.EAST)
			{
				return mountPos.Add(0.0, y2, 0.5);
			}
			if (facing == BlockFacing.SOUTH)
			{
				return mountPos.Add(0.5, y2, 0.0);
			}
			if (facing == BlockFacing.WEST)
			{
				return mountPos.Add(1.0, y2, 0.5);
			}
			return null;
		}
	}

	public AnimationMetaData SuggestedAnimation => meta;

	public EntityControls Controls => controls;

	public IMountable MountSupplier => this;

	public EnumMountAngleMode AngleMode => EnumMountAngleMode.FixateYaw;

	public Vec3f LocalEyePos => eyePos;

	Entity IMountableSeat.Passenger => MountedBy;

	public bool CanControl => false;

	public Entity Entity => null;

	public Matrixf RenderTransform => null;

	public IMountableSeat[] Seats => new IMountableSeat[1] { this };

	public bool SkipIdleAnimation => false;

	public float FpHandPitchFollow => 1f;

	public string SeatId
	{
		get
		{
			return "bed-0";
		}
		set
		{
		}
	}

	public SeatConfig Config
	{
		get
		{
			return null;
		}
		set
		{
		}
	}

	public long PassengerEntityIdForInit
	{
		get
		{
			return mountedByEntityId;
		}
		set
		{
			mountedByEntityId = value;
		}
	}

	public Entity Controller => MountedBy;

	public Entity OnEntity => null;

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		controls.OnAction = onControls;
		if (base.Block.Attributes != null)
		{
			sleepEfficiency = base.Block.Attributes["sleepEfficiency"].AsFloat(0.5f);
		}
		Cuboidf[] collboxes = base.Block.GetCollisionBoxes(api.World.BlockAccessor, Pos);
		if (collboxes != null && collboxes.Length != 0)
		{
			y2 = collboxes[0].Y2;
		}
		facing = BlockFacing.FromCode(base.Block.LastCodePart());
		if (MountedBy == null && (mountedByEntityId != 0L || mountedByPlayerUid != null))
		{
			EntityAgent entity = ((mountedByPlayerUid == null) ? (api.World.GetEntityById(mountedByEntityId) as EntityAgent) : api.World.PlayerByUid(mountedByPlayerUid)?.Entity);
			if (entity?.SidedProperties != null)
			{
				entity.TryMount(this);
			}
		}
	}

	private void onControls(EnumEntityAction action, bool on, ref EnumHandling handled)
	{
		if (action == EnumEntityAction.Sneak && on)
		{
			MountedBy?.TryUnmount();
			controls.StopAllMovement();
			handled = EnumHandling.PassThrough;
		}
	}

	private void RestPlayer(float dt)
	{
		double hoursPassed = Api.World.Calendar.TotalHours - hoursTotal;
		float sleepEff = sleepEfficiency - 1f / 12f;
		if (!(hoursPassed > 0.0))
		{
			return;
		}
		if (Api.World.Config.GetString("temporalStormSleeping", "0").ToInt() == 0 && Api.ModLoader.GetModSystem<SystemTemporalStability>().StormStrength > 0f)
		{
			MountedBy.TryUnmount();
			return;
		}
		if (MountedBy?.GetBehavior("tiredness") is EntityBehaviorTiredness ebt)
		{
			float newval = (ebt.Tiredness = Math.Max(0f, ebt.Tiredness - (float)hoursPassed / sleepEff));
			if (newval <= 0f)
			{
				MountedBy.TryUnmount();
			}
		}
		hoursTotal = Api.World.Calendar.TotalHours;
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
		base.OnBlockBroken(byPlayer);
		blockBroken = true;
		MountedBy?.TryUnmount();
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		mountedByEntityId = tree.GetLong("mountedByEntityId", 0L);
		mountedByPlayerUid = tree.GetString("mountedByPlayerUid");
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetLong("mountedByEntityId", mountedByEntityId);
		tree.SetString("mountedByPlayerUid", mountedByPlayerUid);
	}

	public void MountableToTreeAttributes(TreeAttribute tree)
	{
		tree.SetString("className", "bed");
		tree.SetInt("posx", Pos.X);
		tree.SetInt("posy", Pos.InternalY);
		tree.SetInt("posz", Pos.Z);
	}

	public void DidUnmount(EntityAgent entityAgent)
	{
		if (MountedBy?.GetBehavior("tiredness") is EntityBehaviorTiredness ebt)
		{
			ebt.IsSleeping = false;
		}
		MountedBy = null;
		if (!blockBroken)
		{
			BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
			foreach (BlockFacing facing in hORIZONTALS)
			{
				Vec3d placepos = Pos.ToVec3d().AddCopy(facing).Add(0.5, 0.001, 0.5);
				if (!Api.World.CollisionTester.IsColliding(Api.World.BlockAccessor, entityAgent.SelectionBox, placepos, alsoCheckTouch: false))
				{
					entityAgent.TeleportTo(placepos);
					break;
				}
			}
		}
		mountedByEntityId = 0L;
		mountedByPlayerUid = null;
		UnregisterGameTickListener(restingListener);
		restingListener = 0L;
	}

	public void DidMount(EntityAgent entityAgent)
	{
		if (MountedBy != null && MountedBy != entityAgent)
		{
			entityAgent.TryUnmount();
		}
		else
		{
			if (MountedBy == entityAgent)
			{
				return;
			}
			MountedBy = entityAgent;
			mountedByPlayerUid = (entityAgent as EntityPlayer)?.PlayerUID;
			mountedByEntityId = MountedBy.EntityId;
			ICoreAPI api = entityAgent.Api;
			if (api != null && api.Side == EnumAppSide.Server)
			{
				if (restingListener == 0L)
				{
					ICoreAPI oldapi = Api;
					Api = entityAgent.Api;
					restingListener = RegisterGameTickListener(RestPlayer, 200);
					Api = oldapi;
				}
				hoursTotal = entityAgent.Api.World.Calendar.TotalHours;
			}
			if (MountedBy != null)
			{
				entityAgent.Api.Event.EnqueueMainThreadTask(delegate
				{
					if (MountedBy != null && MountedBy.GetBehavior("tiredness") is EntityBehaviorTiredness entityBehaviorTiredness)
					{
						entityBehaviorTiredness.IsSleeping = true;
					}
				}, "issleeping");
			}
			MarkDirty();
		}
	}

	public bool IsMountedBy(Entity entity)
	{
		return MountedBy == entity;
	}

	public bool IsBeingControlled()
	{
		return false;
	}

	public bool CanUnmount(EntityAgent entityAgent)
	{
		return true;
	}

	public bool CanMount(EntityAgent entityAgent)
	{
		return !AnyMounted();
	}

	public bool AnyMounted()
	{
		return MountedBy != null;
	}
}
