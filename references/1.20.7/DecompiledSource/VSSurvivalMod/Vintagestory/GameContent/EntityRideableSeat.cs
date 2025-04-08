using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityRideableSeat : EntitySeat
{
	protected EntityPos seatPos = new EntityPos();

	protected Matrixf modelmat = new Matrixf();

	protected string RideableClassName = "rideableanimal";

	public override EnumMountAngleMode AngleMode => EnumMountAngleMode.FixateYaw;

	public override AnimationMetaData SuggestedAnimation => (mountedEntity as EntityBehaviorRideable).curAnim;

	public override Vec3f LocalEyePos
	{
		get
		{
			modelmat.Identity();
			if (Entity.AnimManager?.Animator?.GetAttachmentPointPose(config.APName) != null)
			{
				modelmat.RotateY((float)Math.PI / 2f + Entity.Pos.Yaw);
				modelmat.RotateX((Entity.Properties.Client.Renderer as EntityShapeRenderer)?.nowSwivelRad ?? 0f);
				modelmat.Translate(0f, config.EyeHeight, 0f);
				modelmat.RotateY(-(float)Math.PI / 2f - Entity.Pos.Yaw);
			}
			return modelmat.TransformVector(new Vec4f(0f, 0f, 0f, 1f)).XYZ;
		}
	}

	public override EntityPos SeatPosition
	{
		get
		{
			loadAttachPointTransform();
			Vec4f rotvec = modelmat.TransformVector(new Vec4f(0f, 0f, 0f, 1f));
			seatPos.Yaw = Entity.Pos.Yaw;
			return seatPos.SetFrom(mountedEntity.Position).Add(rotvec.X, rotvec.Y, rotvec.Z);
		}
	}

	public override Matrixf RenderTransform
	{
		get
		{
			loadAttachPointTransform();
			Vec4f rotvec = modelmat.TransformVector(new Vec4f(0f, 0f, 0f, 1f));
			return new Matrixf().RotateDeg(config.MountRotation).Translate(0f - rotvec.X, 0f - rotvec.Y, 0f - rotvec.Z).Mul(modelmat);
		}
	}

	public override float FpHandPitchFollow => 0.2f;

	private void loadAttachPointTransform()
	{
		modelmat.Identity();
		AttachmentPointAndPose apap = Entity.AnimManager?.Animator?.GetAttachmentPointPose(config.APName);
		if (apap != null)
		{
			EntityShapeRenderer esr = Entity.Properties.Client.Renderer as EntityShapeRenderer;
			modelmat.RotateY(-(float)Math.PI / 2f + Entity.Pos.Yaw + (float)Math.PI);
			modelmat.Translate(0.0, 0.6, 0.0);
			if (esr != null)
			{
				modelmat.RotateX(esr.nowSwivelRad + esr.xangle);
				modelmat.RotateY(esr.yangle);
				modelmat.RotateZ(esr.zangle);
			}
			modelmat.Translate(0.0, -0.6, 0.0);
			apap.Mul(modelmat);
			if (config.MountOffset != null)
			{
				modelmat.Translate(config.MountOffset);
			}
			modelmat.Translate(-0.5, 0.0, -0.5);
			modelmat.RotateY((float)Math.PI / 2f - Entity.Pos.Yaw);
		}
	}

	public EntityRideableSeat(IMountable mountablesupplier, string seatId, SeatConfig config)
		: base(mountablesupplier, seatId, config)
	{
	}

	public override bool CanMount(EntityAgent entityAgent)
	{
		if (!(entityAgent is EntityPlayer player))
		{
			return false;
		}
		EntityBehaviorOwnable ebho = Entity.GetBehavior<EntityBehaviorOwnable>();
		if (ebho != null && !ebho.IsOwner(player))
		{
			(player.World.Api as ICoreClientAPI)?.TriggerIngameError(this, "requiersownership", Lang.Get("mount-interact-requiresownership"));
			return false;
		}
		return true;
	}

	public static IMountableSeat GetMountable(IWorldAccessor world, TreeAttribute tree)
	{
		return (world.GetEntityById(tree.GetLong("entityIdMount", 0L))?.GetBehavior<EntityBehaviorSeatable>())?.Seats.FirstOrDefault((IMountableSeat seat) => seat.SeatId == tree.GetString("seatId"));
	}

	public override void MountableToTreeAttributes(TreeAttribute tree)
	{
		base.MountableToTreeAttributes(tree);
		tree.SetLong("entityIdMount", Entity.EntityId);
		tree.SetString("className", RideableClassName);
	}

	public override void DidMount(EntityAgent entityAgent)
	{
		base.DidMount(entityAgent);
		if (Entity != null)
		{
			Entity.GetBehavior<EntityBehaviorTaskAI>()?.TaskManager.StopTasks();
			Entity.StartAnimation("idle");
			if (entityAgent.Api is ICoreClientAPI capi && capi.World.Player.Entity.EntityId == entityAgent.EntityId)
			{
				capi.Input.MouseYaw = Entity.Pos.Yaw;
			}
		}
		(mountedEntity as IMountableListener)?.DidMount(entityAgent);
		(Entity as IMountableListener)?.DidMount(entityAgent);
	}

	public override void DidUnmount(EntityAgent entityAgent)
	{
		if (entityAgent.World.Side == EnumAppSide.Server && base.DoTeleportOnUnmount)
		{
			tryTeleportToFreeLocation();
		}
		if (entityAgent is EntityPlayer eplr)
		{
			eplr.BodyYawLimits = null;
			eplr.HeadYawLimits = null;
		}
		base.DidUnmount(entityAgent);
		(mountedEntity as IMountableListener)?.DidUnnmount(entityAgent);
		(Entity as IMountableListener)?.DidUnnmount(entityAgent);
	}

	protected virtual void tryTeleportToFreeLocation()
	{
		IWorldAccessor world = base.Passenger.World;
		IBlockAccessor ba = base.Passenger.World.BlockAccessor;
		Vec3d rightPos = Entity.Pos.XYZ.Add(EntityPos.GetViewVector(0f, Entity.Pos.Yaw + (float)Math.PI / 2f)).Add(0.0, 0.01, 0.0);
		Vec3d leftPos = Entity.Pos.XYZ.Add(EntityPos.GetViewVector(0f, Entity.Pos.Yaw - (float)Math.PI / 2f)).Add(0.0, 0.01, 0.0);
		if (GameMath.AngleRadDistance(base.Passenger.Pos.Yaw, Entity.Pos.Yaw + (float)Math.PI / 2f) < (float)Math.PI / 2f)
		{
			Vec3d vec3d = leftPos;
			leftPos = rightPos;
			rightPos = vec3d;
		}
		if (ba.GetMostSolidBlock((int)rightPos.X, (int)(rightPos.Y - 0.1), (int)rightPos.Z).SideSolid[BlockFacing.UP.Index] && !world.CollisionTester.IsColliding(ba, base.Passenger.CollisionBox, rightPos, alsoCheckTouch: false))
		{
			base.Passenger.TeleportTo(rightPos);
		}
		else if (ba.GetMostSolidBlock((int)leftPos.X, (int)(leftPos.Y - 0.1), (int)leftPos.Z).SideSolid[BlockFacing.UP.Index] && !world.CollisionTester.IsColliding(ba, base.Passenger.CollisionBox, leftPos, alsoCheckTouch: false))
		{
			base.Passenger.TeleportTo(leftPos);
		}
	}
}
