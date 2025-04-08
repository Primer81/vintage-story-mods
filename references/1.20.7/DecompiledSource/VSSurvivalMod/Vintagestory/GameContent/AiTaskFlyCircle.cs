using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskFlyCircle : AiTaskTargetableAt
{
	private bool stayNearSpawn;

	private float minRadius;

	private float maxRadius;

	private float height;

	protected double desiredYPos;

	protected float moveSpeed = 0.04f;

	private double dir = 1.0;

	private float dirchangeCoolDown;

	private double nowRadius;

	private bool wasOutsideLoaded;

	public AiTaskFlyCircle(EntityAgent entity)
		: base(entity)
	{
	}

	public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
	{
		base.LoadConfig(taskConfig, aiConfig);
		stayNearSpawn = taskConfig["stayNearSpawn"].AsBool();
		minRadius = taskConfig["minRadius"].AsFloat(10f);
		maxRadius = taskConfig["maxRadius"].AsFloat(20f);
		height = taskConfig["height"].AsFloat(5f);
		moveSpeed = taskConfig["moveSpeed"].AsFloat(0.04f);
	}

	public override bool ShouldExecute()
	{
		return true;
	}

	public override void StartExecute()
	{
		nowRadius = minRadius + (float)world.Rand.NextDouble() * (maxRadius - minRadius);
		float yaw = (float)world.Rand.NextDouble() * ((float)Math.PI * 2f);
		double rndx = nowRadius * Math.Sin(yaw);
		double rndz = nowRadius * Math.Cos(yaw);
		if (stayNearSpawn)
		{
			CenterPos = SpawnPos;
		}
		else
		{
			CenterPos = entity.ServerPos.XYZ.Add(rndx, 0.0, rndz);
		}
		base.StartExecute();
	}

	public override bool ContinueExecute(float dt)
	{
		if ((int)CenterPos.Y / 32768 != entity.Pos.Dimension)
		{
			return false;
		}
		if (entity.OnGround || entity.World.Rand.NextDouble() < 0.03)
		{
			ReadjustFlyHeight();
		}
		double yMot = GameMath.Clamp(desiredYPos - entity.ServerPos.Y, -0.33, 0.33);
		double num = entity.ServerPos.X - CenterPos.X;
		double dz = entity.ServerPos.Z - CenterPos.Z;
		double rad = Math.Sqrt(num * num + dz * dz);
		double offs = nowRadius - rad;
		float targetYaw = (float)Math.Atan2(num, dz) + (float)Math.PI / 2f + 0.1f * (float)dir;
		entity.ServerPos.Yaw += GameMath.AngleRadDistance(entity.ServerPos.Yaw, targetYaw) * dt;
		float bla = (float)GameMath.Clamp(offs / 20.0, -1.0, 1.0);
		double cosYaw = Math.Cos(entity.ServerPos.Yaw - bla);
		double sinYaw = Math.Sin(entity.ServerPos.Yaw - bla);
		entity.Controls.WalkVector.Set(sinYaw, yMot, cosYaw);
		entity.Controls.WalkVector.Mul(moveSpeed);
		if (yMot < 0.0)
		{
			entity.Controls.WalkVector.Mul(0.5);
		}
		if (entity.Swimming)
		{
			entity.Controls.WalkVector.Y = 2f * moveSpeed;
			entity.Controls.FlyVector.Y = 2f * moveSpeed;
		}
		dirchangeCoolDown = Math.Max(0f, dirchangeCoolDown - dt);
		if (entity.CollidedHorizontally && dirchangeCoolDown <= 0f)
		{
			dirchangeCoolDown = 2f;
			dir *= -1.0;
		}
		return entity.Alive;
	}

	protected void ReadjustFlyHeight()
	{
		EntityPos pos = entity.ServerPos;
		bool outsideLoaded = entity.World.BlockAccessor.IsNotTraversable(pos.X, pos.Y, pos.Z, pos.Dimension);
		if (outsideLoaded)
		{
			desiredYPos = Math.Max((float)entity.World.SeaLevel + height, desiredYPos);
			return;
		}
		if (wasOutsideLoaded)
		{
			entity.ServerPos.Y = (float)entity.World.BlockAccessor.GetTerrainMapheightAt(entity.ServerPos.AsBlockPos) + height;
			wasOutsideLoaded = false;
			return;
		}
		wasOutsideLoaded = outsideLoaded;
		int terrainYPos = entity.World.BlockAccessor.GetTerrainMapheightAt(entity.SidedPos.AsBlockPos);
		int tries = 10;
		while (tries-- > 0 && entity.World.BlockAccessor.GetBlock((int)entity.ServerPos.X, terrainYPos, (int)entity.ServerPos.Z, 2).IsLiquid())
		{
			terrainYPos++;
		}
		desiredYPos = (float)terrainYPos + height;
	}
}
