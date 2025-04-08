using System;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[ProtoContract]
public class ClothPoint
{
	public static bool PushingPhysics;

	[ProtoMember(1)]
	public int PointIndex;

	[ProtoMember(2)]
	public float Mass;

	[ProtoMember(3)]
	public float InvMass;

	[ProtoMember(4)]
	public Vec3d Pos;

	[ProtoMember(5)]
	public Vec3f Velocity = new Vec3f();

	[ProtoMember(6)]
	public Vec3f Tension = new Vec3f();

	[ProtoMember(7)]
	private float GravityStrength = 1f;

	[ProtoMember(8)]
	private bool pinned;

	[ProtoMember(9)]
	public long pinnedToEntityId;

	[ProtoMember(10)]
	private BlockPos pinnedToBlockPos;

	[ProtoMember(11)]
	public Vec3f pinnedToOffset;

	[ProtoMember(12)]
	private float pinnedToOffsetStartYaw;

	[ProtoMember(13)]
	private string pinnedToPlayerUid;

	public EnumCollideFlags CollideFlags;

	public float YCollideRestMul;

	private Vec4f tmpvec = new Vec4f();

	private ClothSystem cs;

	private Entity pinnedTo;

	private Matrixf pinOffsetTransform;

	public Vec3d TensionDirection = new Vec3d();

	public double extension;

	private float dampFactor = 0.9f;

	private float accum1s;

	public bool Dirty { get; internal set; }

	public Entity PinnedToEntity => pinnedTo;

	public BlockPos PinnedToBlockPos => pinnedToBlockPos;

	public bool Pinned => pinned;

	public ClothPoint(ClothSystem cs)
	{
		this.cs = cs;
		Pos = new Vec3d();
		init();
	}

	protected ClothPoint()
	{
	}

	public ClothPoint(ClothSystem cs, int pointIndex, double x, double y, double z)
	{
		this.cs = cs;
		PointIndex = pointIndex;
		Pos = new Vec3d(x, y, z);
		init();
	}

	public void setMass(float mass)
	{
		Mass = mass;
		InvMass = 1f / mass;
	}

	private void init()
	{
		setMass(1f);
	}

	public void PinTo(Entity toEntity, Vec3f pinOffset)
	{
		pinned = true;
		pinnedTo = toEntity;
		pinnedToEntityId = toEntity.EntityId;
		pinnedToOffset = pinOffset;
		pinnedToOffsetStartYaw = toEntity.SidedPos.Yaw;
		pinOffsetTransform = Matrixf.Create();
		pinnedToBlockPos = null;
		if (toEntity is EntityPlayer eplr)
		{
			pinnedToPlayerUid = eplr.PlayerUID;
		}
		MarkDirty();
	}

	public void PinTo(BlockPos blockPos, Vec3f offset)
	{
		pinnedToBlockPos = blockPos;
		pinnedToOffset = offset;
		pinnedToPlayerUid = null;
		pinned = true;
		Pos.Set(pinnedToBlockPos).Add(pinnedToOffset);
		pinnedTo = null;
		pinnedToEntityId = 0L;
		MarkDirty();
	}

	public void UnPin()
	{
		pinned = false;
		pinnedTo = null;
		pinnedToPlayerUid = null;
		pinnedToEntityId = 0L;
		MarkDirty();
	}

	public void MarkDirty()
	{
		Dirty = true;
	}

	public void update(float dt, IWorldAccessor world)
	{
		if (pinnedTo == null && pinnedToPlayerUid != null)
		{
			EntityPlayer eplr2 = world.PlayerByUid(pinnedToPlayerUid)?.Entity;
			if (eplr2?.World != null)
			{
				PinTo(eplr2, pinnedToOffset);
			}
		}
		if (pinned)
		{
			if (pinnedTo != null)
			{
				Entity pinnedToMounted = pinnedTo;
				if (pinnedToMounted is EntityAgent pinnedToAgent && pinnedToAgent.MountedOn?.Entity != null)
				{
					pinnedToMounted = pinnedToAgent.MountedOn.Entity;
				}
				if (pinnedToMounted.ShouldDespawn)
				{
					EntityDespawnData despawnReason = pinnedToMounted.DespawnReason;
					if (despawnReason == null || despawnReason.Reason != EnumDespawnReason.Unload)
					{
						UnPin();
						return;
					}
				}
				float weight = pinnedToMounted.Properties.Weight;
				float counterTensionStrength = GameMath.Clamp(50f / weight, 0.1f, 2f);
				EntityAgent obj = pinnedToMounted as EntityAgent;
				int num;
				if (obj == null || !obj.Controls.Sneak)
				{
					if (pinnedToMounted is EntityPlayer)
					{
						IAnimationManager animManager = pinnedToMounted.AnimManager;
						num = (((animManager != null && animManager.IsAnimationActive("sit")) || (pinnedToMounted.AnimManager?.IsAnimationActive("sleep") ?? false)) ? 1 : 0);
					}
					else
					{
						num = 0;
					}
				}
				else
				{
					num = 1;
				}
				bool extraResist = (byte)num != 0;
				float tensionResistStrength = weight / 10f * (float)((!extraResist) ? 1 : 200);
				EntityPlayer eplr = pinnedTo as EntityPlayer;
				EntityAgent eagent = pinnedTo as EntityAgent;
				AttachmentPointAndPose apap = eplr?.AnimManager?.Animator?.GetAttachmentPointPose("RightHand");
				if (apap == null)
				{
					apap = pinnedTo?.AnimManager?.Animator?.GetAttachmentPointPose("rope");
				}
				Vec4f outvec;
				if (apap != null)
				{
					Matrixf modelmat = new Matrixf();
					if (eplr != null)
					{
						modelmat.RotateY(eagent.BodyYaw + (float)Math.PI / 2f);
					}
					else
					{
						modelmat.RotateY(pinnedTo.SidedPos.Yaw + (float)Math.PI / 2f);
					}
					modelmat.Translate(-0.5, 0.0, -0.5);
					apap.MulUncentered(modelmat);
					outvec = modelmat.TransformVector(new Vec4f(0f, 0f, 0f, 1f));
				}
				else
				{
					pinOffsetTransform.Identity();
					pinOffsetTransform.RotateY(pinnedTo.SidedPos.Yaw - pinnedToOffsetStartYaw);
					tmpvec.Set(pinnedToOffset.X, pinnedToOffset.Y, pinnedToOffset.Z, 1f);
					outvec = pinOffsetTransform.TransformVector(tmpvec);
				}
				EntityPos pos = pinnedTo.SidedPos;
				Pos.Set(pos.X + (double)outvec.X, pos.Y + (double)outvec.Y, pos.Z + (double)outvec.Z);
				if (true && extension > 0.0)
				{
					float f = counterTensionStrength * dt * 0.006f;
					pinnedToMounted.SidedPos.Motion.Add(GameMath.Clamp(Math.Abs(TensionDirection.X) - (double)tensionResistStrength, 0.0, 400.0) * (double)f * (double)Math.Sign(TensionDirection.X), GameMath.Clamp(Math.Abs(TensionDirection.Y) - (double)tensionResistStrength, 0.0, 400.0) * (double)f * (double)Math.Sign(TensionDirection.Y), GameMath.Clamp(Math.Abs(TensionDirection.Z) - (double)tensionResistStrength, 0.0, 400.0) * (double)f * (double)Math.Sign(TensionDirection.Z));
				}
				Velocity.Set(0f, 0f, 0f);
				return;
			}
			Velocity.Set(0f, 0f, 0f);
			if (!(pinnedToBlockPos != null))
			{
				return;
			}
			accum1s += dt;
			if (accum1s >= 1f)
			{
				accum1s = 0f;
				if (!cs.api.World.BlockAccessor.GetBlock(PinnedToBlockPos).HasBehavior<BlockBehaviorRopeTieable>())
				{
					UnPin();
				}
			}
		}
		else
		{
			Vec3f vec3f = Tension.Clone();
			vec3f.Y -= GravityStrength * 10f;
			Vec3f acceleration = vec3f * InvMass;
			if (CollideFlags == (EnumCollideFlags)0)
			{
				acceleration.X += (float)cs.windSpeed.X * InvMass;
			}
			Vec3f nextVelocity = Velocity + acceleration * dt;
			nextVelocity *= dampFactor;
			float size = 0.1f;
			cs.pp.HandleBoyancy(Pos, nextVelocity, cs.boyant, GravityStrength, dt, size);
			CollideFlags = cs.pp.UpdateMotion(Pos, nextVelocity, size);
			dt *= 0.99f;
			Pos.Add(nextVelocity.X * dt, nextVelocity.Y * dt, nextVelocity.Z * dt);
			Velocity.Set(nextVelocity);
			Tension.Set(0f, 0f, 0f);
		}
	}

	public void restoreReferences(ClothSystem cs, IWorldAccessor world)
	{
		this.cs = cs;
		if (pinnedToEntityId != 0L)
		{
			pinnedTo = world.GetEntityById(pinnedToEntityId);
			if (pinnedTo != null)
			{
				PinTo(pinnedTo, pinnedToOffset);
			}
		}
		if (pinnedToBlockPos != null)
		{
			PinTo(pinnedToBlockPos, pinnedToOffset);
		}
	}

	public void restoreReferences(Entity entity)
	{
		if (pinnedToEntityId == entity.EntityId)
		{
			PinTo(entity, pinnedToOffset);
		}
	}

	public void updateFromPoint(ClothPoint point, IWorldAccessor world)
	{
		PointIndex = point.PointIndex;
		Mass = point.Mass;
		InvMass = point.InvMass;
		Pos.Set(point.Pos);
		Velocity.Set(point.Pos);
		Tension.Set(point.Tension);
		GravityStrength = point.GravityStrength;
		pinned = point.pinned;
		pinnedToEntityId = point.pinnedToEntityId;
		pinnedToPlayerUid = point.pinnedToPlayerUid;
		if (pinnedToEntityId != 0L)
		{
			pinnedTo = world.GetEntityById(pinnedToEntityId);
			if (pinnedTo != null)
			{
				PinTo(pinnedTo, pinnedToOffset);
			}
			else
			{
				UnPin();
			}
		}
		else
		{
			pinnedTo = null;
		}
		pinnedToBlockPos = pinnedToBlockPos.SetOrCreate(point.pinnedToBlockPos);
		pinnedToOffset = pinnedToOffset.SetOrCreate(point.pinnedToOffset);
		pinnedToOffsetStartYaw = point.pinnedToOffsetStartYaw;
	}
}
