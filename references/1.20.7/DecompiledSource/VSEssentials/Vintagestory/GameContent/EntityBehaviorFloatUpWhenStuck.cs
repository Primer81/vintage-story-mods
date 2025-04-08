using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class EntityBehaviorFloatUpWhenStuck : EntityBehavior
{
	private bool onlyWhenDead;

	private int counter;

	private bool stuckInBlock;

	private float pushVelocityMul = 1f;

	private Vec3d tmpPos = new Vec3d();

	public EntityBehaviorFloatUpWhenStuck(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
		onlyWhenDead = attributes["onlyWhenDead"].AsBool();
		pushVelocityMul = attributes["pushVelocityMul"].AsFloat(1f);
		counter = (int)entity.EntityId / 10 % 10;
	}

	public override void OnTesselated()
	{
		base.OnTesselated();
		ensureCenterAPExists();
	}

	private void ensureCenterAPExists()
	{
		if (entity.AnimManager != null && entity.World.Side == EnumAppSide.Client && entity.AnimManager.Animator?.GetAttachmentPointPose("Center") == null)
		{
			HashSet<AssetLocation> hashse = ObjectCacheUtil.GetOrCreate(entity.Api, "missingCenterApEntityCodes", () => new HashSet<AssetLocation>());
			if (!hashse.Contains(entity.Code))
			{
				hashse.Add(entity.Code);
				entity.World.Logger.Warning(string.Concat("Entity ", entity.Code, " with shape ", entity.Properties.Client.Shape?.ToString(), " seems to be missing attachment point center but also has the FloatUpWhenStuck behavior - it might not work correctly with the center point lacking"));
			}
		}
	}

	public override void OnGameTick(float deltaTime)
	{
		if (entity.World.ElapsedMilliseconds < 2000 || entity.World.Side == EnumAppSide.Client || (counter++ <= 10 && (!stuckInBlock || counter <= 1)))
		{
			return;
		}
		counter = 0;
		if ((onlyWhenDead && entity.Alive) || entity.Properties.CanClimbAnywhere)
		{
			return;
		}
		stuckInBlock = false;
		entity.Properties.Habitat = EnumHabitat.Land;
		if (!entity.Swimming)
		{
			tmpPos.Set(entity.SidedPos.X, entity.SidedPos.Y, entity.SidedPos.Z);
			Cuboidd collbox = entity.World.CollisionTester.GetCollidingCollisionBox(entity.World.BlockAccessor, entity.CollisionBox.Clone().ShrinkBy(0.01f), tmpPos, alsoCheckTouch: false);
			if (collbox != null)
			{
				PushoutOfCollisionbox(deltaTime, collbox);
				stuckInBlock = true;
			}
		}
	}

	private void PushoutOfCollisionbox(float dt, Cuboidd collBox)
	{
		double posX = entity.SidedPos.X;
		double posY = entity.SidedPos.Y;
		double posZ = entity.SidedPos.Z;
		IBlockAccessor ba = entity.World.BlockAccessor;
		Vec3i pushDir = null;
		double shortestDist = 99.0;
		for (int i = 0; i < Cardinal.ALL.Length; i++)
		{
			if (shortestDist <= 0.25)
			{
				break;
			}
			Cardinal cardinal = Cardinal.ALL[i];
			for (int dist = 1; dist <= 4; dist++)
			{
				float r = (float)dist / 4f;
				if (!entity.World.CollisionTester.IsColliding(ba, entity.CollisionBox, tmpPos.Set(posX + (double)((float)cardinal.Normali.X * r), posY, posZ + (double)((float)cardinal.Normali.Z * r)), alsoCheckTouch: false) && (double)r < shortestDist)
				{
					shortestDist = r + (cardinal.IsDiagnoal ? 0.1f : 0f);
					pushDir = cardinal.Normali;
					break;
				}
			}
		}
		if (pushDir == null)
		{
			pushDir = BlockFacing.UP.Normali;
		}
		dt = Math.Min(dt, 0.1f);
		float rndx = ((float)entity.World.Rand.NextDouble() - 0.5f) / 600f;
		float rndz = ((float)entity.World.Rand.NextDouble() - 0.5f) / 600f;
		entity.SidedPos.X += (float)pushDir.X * dt * 0.4f;
		entity.SidedPos.Y += (float)pushDir.Y * dt * 0.4f;
		entity.SidedPos.Z += (float)pushDir.Z * dt * 0.4f;
		entity.SidedPos.Motion.X = pushVelocityMul * (float)pushDir.X * dt + rndx;
		entity.SidedPos.Motion.Y = pushVelocityMul * (float)pushDir.Y * dt * 2f;
		entity.SidedPos.Motion.Z = pushVelocityMul * (float)pushDir.Z * dt + rndz;
		entity.Properties.Habitat = EnumHabitat.Air;
	}

	public override string PropertyName()
	{
		return "floatupwhenstuck";
	}
}
