using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskGetOutOfWater : AiTaskBase
{
	private Vec3d target = new Vec3d();

	private BlockPos pos = new BlockPos();

	private bool done;

	private float moveSpeed = 0.03f;

	private int searchattempts;

	public AiTaskGetOutOfWater(EntityAgent entity)
		: base(entity)
	{
	}

	public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
	{
		base.LoadConfig(taskConfig, aiConfig);
		moveSpeed = taskConfig["movespeed"].AsFloat(0.06f);
	}

	public override bool ShouldExecute()
	{
		if (!entity.Swimming)
		{
			return false;
		}
		if (base.rand.NextDouble() > 0.03999999910593033)
		{
			return false;
		}
		int range = GameMath.Min(50, 30 + searchattempts * 2);
		target.Y = entity.ServerPos.Y;
		int tries = 10;
		int px = (int)entity.ServerPos.X;
		int pz = (int)entity.ServerPos.Z;
		IBlockAccessor blockAccessor = entity.World.BlockAccessor;
		Vec3d tmpPos = new Vec3d();
		while (tries-- > 0)
		{
			pos.X = px + base.rand.Next(range + 1) - range / 2;
			pos.Z = pz + base.rand.Next(range + 1) - range / 2;
			pos.Y = blockAccessor.GetTerrainMapheightAt(pos) + 1;
			if (!blockAccessor.GetBlock(pos, 2).IsLiquid())
			{
				blockAccessor.GetBlock(pos);
				if (!entity.World.CollisionTester.IsColliding(blockAccessor, entity.CollisionBox, tmpPos.Set((double)pos.X + 0.5, (float)pos.Y + 0.1f, (double)pos.Z + 0.5)) && entity.World.CollisionTester.IsColliding(blockAccessor, entity.CollisionBox, tmpPos.Set((double)pos.X + 0.5, (float)pos.Y - 0.1f, (double)pos.Z + 0.5)))
				{
					target.Set((double)pos.X + 0.5, pos.Y + 1, (double)pos.Z + 0.5);
					return true;
				}
			}
		}
		searchattempts++;
		return false;
	}

	public override void StartExecute()
	{
		base.StartExecute();
		searchattempts = 0;
		done = false;
		pathTraverser.WalkTowards(target, moveSpeed, 0.5f, OnGoalReached, OnStuck);
	}

	public override bool ContinueExecute(float dt)
	{
		if (base.rand.NextDouble() < 0.10000000149011612 && !entity.FeetInLiquid)
		{
			return false;
		}
		return !done;
	}

	public override void FinishExecute(bool cancelled)
	{
		base.FinishExecute(cancelled);
		pathTraverser.Stop();
	}

	private void OnStuck()
	{
		done = true;
	}

	private void OnGoalReached()
	{
		done = true;
	}
}
