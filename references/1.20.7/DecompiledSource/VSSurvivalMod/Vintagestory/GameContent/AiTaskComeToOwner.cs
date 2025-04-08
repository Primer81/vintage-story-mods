using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class AiTaskComeToOwner : AiTaskStayCloseToEntity
{
	private long lastExecutedMs;

	public AiTaskComeToOwner(EntityAgent entity)
		: base(entity)
	{
	}

	public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
	{
		base.LoadConfig(taskConfig, aiConfig);
		minSeekSeconds = 10f;
	}

	public override bool ShouldExecute()
	{
		if (entity.WatchedAttributes.GetTreeAttribute("ownedby") == null)
		{
			lastExecutedMs = -99999L;
			return false;
		}
		if ((float)(entity.World.ElapsedMilliseconds - lastExecutedMs) / 1000f < 20f)
		{
			return base.ShouldExecute();
		}
		return false;
	}

	public override void StartExecute()
	{
		lastExecutedMs = entity.World.ElapsedMilliseconds;
		ITreeAttribute tree = entity.WatchedAttributes.GetTreeAttribute("ownedby");
		if (tree != null)
		{
			string uid = tree.GetString("uid");
			targetEntity = entity.World.PlayerByUid(uid)?.Entity;
			if (targetEntity != null)
			{
				float size = targetEntity.SelectionBox.XSize;
				pathTraverser.NavigateTo_Async(targetEntity.ServerPos.XYZ, moveSpeed, size + 0.2f, base.OnGoalReached, base.OnStuck, null, 1000, 1);
				targetOffset.Set(entity.World.Rand.NextDouble() * 2.0 - 1.0, 0.0, entity.World.Rand.NextDouble() * 2.0 - 1.0);
				stuck = false;
			}
			base.StartExecute();
		}
	}

	public override bool CanContinueExecute()
	{
		return pathTraverser.Ready;
	}

	public override bool ContinueExecute(float dt)
	{
		if (targetEntity != null)
		{
			return base.ContinueExecute(dt);
		}
		return false;
	}
}
