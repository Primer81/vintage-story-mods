using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityBehaviorRepulseAgents : EntityBehavior
{
	protected Vec3d pushVector = new Vec3d();

	protected EntityPartitioning partitionUtil;

	protected bool movable = true;

	protected bool ignorePlayers;

	protected EntityAgent selfEagent;

	protected double touchdist;

	private IClientWorldAccessor cworld;

	protected double ownPosRepulseX;

	protected double ownPosRepulseY;

	protected double ownPosRepulseZ;

	protected float mySize;

	public EntityBehaviorRepulseAgents(Entity entity)
		: base(entity)
	{
		entity.hasRepulseBehavior = true;
		selfEagent = entity as EntityAgent;
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
		movable = attributes["movable"].AsBool(defaultValue: true);
		partitionUtil = entity.Api.ModLoader.GetModSystem<EntityPartitioning>();
		ignorePlayers = entity is EntityPlayer && entity.World.Config.GetAsBool("player2PlayerCollisions", defaultValue: true);
		cworld = entity.World as IClientWorldAccessor;
	}

	public override void AfterInitialized(bool onFirstSpawn)
	{
		touchdist = entity.SelectionBox.XSize * 2f;
	}

	public override void UpdateColSelBoxes()
	{
		touchdist = entity.SelectionBox.XSize * 2f;
	}

	public override void OnGameTick(float deltaTime)
	{
		if (entity.State != EnumEntityState.Inactive && entity.IsInteractable && movable && !(entity is EntityAgent { MountedOn: not null }) && entity.World.ElapsedMilliseconds >= 2000)
		{
			pushVector.Set(0.0, 0.0, 0.0);
			ownPosRepulseX = entity.ownPosRepulse.X;
			ownPosRepulseY = entity.ownPosRepulse.Y + (double)entity.Pos.DimensionYAdjustment;
			ownPosRepulseZ = entity.ownPosRepulse.Z;
			mySize = entity.SelectionBox.Length * entity.SelectionBox.Height;
			if (selfEagent != null && selfEagent.Controls.Sneak)
			{
				mySize *= 2f;
			}
			if (cworld != null && entity != cworld.Player.Entity)
			{
				WalkEntity(cworld.Player.Entity);
			}
			else
			{
				partitionUtil.WalkEntityPartitions(entity.ownPosRepulse, touchdist + partitionUtil.LargestTouchDistance + 0.1, WalkEntity);
			}
			pushVector.X = GameMath.Clamp(pushVector.X, -3.0, 3.0);
			pushVector.Y = GameMath.Clamp(pushVector.Y, -3.0, 0.5);
			pushVector.Z = GameMath.Clamp(pushVector.Z, -3.0, 3.0);
			entity.SidedPos.Motion.Add(pushVector.X / 30.0, pushVector.Y / 30.0, pushVector.Z / 30.0);
		}
	}

	private bool WalkEntity(Entity e)
	{
		if (!e.hasRepulseBehavior || !e.IsInteractable || e == entity || (ignorePlayers && e is EntityPlayer))
		{
			return true;
		}
		if (e is EntityAgent eagent && eagent.MountedOn?.Entity == entity)
		{
			return true;
		}
		if (e.customRepulseBehavior)
		{
			return e.GetInterface<ICustomRepulseBehavior>().Repulse(entity, pushVector);
		}
		double dx = ownPosRepulseX - e.ownPosRepulse.X;
		double dy = ownPosRepulseY - e.ownPosRepulse.Y;
		double dz = ownPosRepulseZ - e.ownPosRepulse.Z;
		double distSq = dx * dx + dy * dy + dz * dz;
		double minDistSq = entity.touchDistanceSq + e.touchDistanceSq;
		if (distSq >= minDistSq)
		{
			return true;
		}
		double pushForce = (1.0 - distSq / minDistSq) / (double)Math.Max(0.001f, GameMath.Sqrt(distSq));
		double px = dx * pushForce;
		double py = dy * pushForce;
		double pz = dz * pushForce;
		float pushDiff = GameMath.Clamp(e.SelectionBox.Length * e.SelectionBox.Height / mySize, 0f, 1f);
		if (entity.OnGround)
		{
			pushDiff *= 3f;
		}
		pushVector.Add(px * (double)pushDiff, py * (double)pushDiff * 0.75, pz * (double)pushDiff);
		return true;
	}

	public override string PropertyName()
	{
		return "repulseagents";
	}
}
