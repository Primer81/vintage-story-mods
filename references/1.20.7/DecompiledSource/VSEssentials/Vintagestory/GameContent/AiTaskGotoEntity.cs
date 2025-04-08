using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AiTaskGotoEntity : AiTaskBase
{
	public Entity targetEntity;

	public float moveSpeed = 0.02f;

	public float seekingRange = 25f;

	public float maxFollowTime = 60f;

	public float allowedExtraDistance;

	private bool stuck;

	private float currentFollowTime;

	public bool Finished => !pathTraverser.Ready;

	public AiTaskGotoEntity(EntityAgent entity, Entity target)
		: base(entity)
	{
		targetEntity = target;
		animMeta = new AnimationMetaData
		{
			Code = "walk",
			Animation = "walk",
			AnimationSpeed = 1f
		}.Init();
	}

	public override bool ShouldExecute()
	{
		return false;
	}

	public float MinDistanceToTarget()
	{
		return allowedExtraDistance + Math.Max(0.8f, targetEntity.SelectionBox.XSize / 2f + entity.SelectionBox.XSize / 2f);
	}

	public override void StartExecute()
	{
		base.StartExecute();
		stuck = false;
		pathTraverser.NavigateTo_Async(targetEntity.ServerPos.XYZ, moveSpeed, MinDistanceToTarget(), OnGoalReached, OnStuck, null, 999);
		currentFollowTime = 0f;
	}

	public override bool CanContinueExecute()
	{
		return pathTraverser.Ready;
	}

	public override bool ContinueExecute(float dt)
	{
		currentFollowTime += dt;
		pathTraverser.CurrentTarget.X = targetEntity.ServerPos.X;
		pathTraverser.CurrentTarget.Y = targetEntity.ServerPos.Y;
		pathTraverser.CurrentTarget.Z = targetEntity.ServerPos.Z;
		Cuboidd cuboidd = targetEntity.SelectionBox.ToDouble().Translate(targetEntity.ServerPos.X, targetEntity.ServerPos.Y, targetEntity.ServerPos.Z);
		Vec3d pos = entity.ServerPos.XYZ.Add(0.0, entity.SelectionBox.Y2 / 2f, 0.0).Ahead(entity.SelectionBox.XSize / 2f, 0f, entity.ServerPos.Yaw);
		double distance = cuboidd.ShortestDistanceFrom(pos);
		float minDist = MinDistanceToTarget();
		if (currentFollowTime < maxFollowTime && distance < (double)(seekingRange * seekingRange) && distance > (double)minDist)
		{
			return !stuck;
		}
		return false;
	}

	public bool TargetReached()
	{
		Cuboidd cuboidd = targetEntity.SelectionBox.ToDouble().Translate(targetEntity.ServerPos.X, targetEntity.ServerPos.Y, targetEntity.ServerPos.Z);
		Vec3d pos = entity.ServerPos.XYZ.Add(0.0, entity.SelectionBox.Y2 / 2f, 0.0).Ahead(entity.SelectionBox.XSize / 2f, 0f, entity.ServerPos.Yaw);
		double num = cuboidd.ShortestDistanceFrom(pos);
		float minDist = MinDistanceToTarget();
		return num < (double)minDist;
	}

	public override void FinishExecute(bool cancelled)
	{
		base.FinishExecute(cancelled);
		pathTraverser.Stop();
	}

	public override bool Notify(string key, object data)
	{
		return false;
	}

	private void OnStuck()
	{
		stuck = true;
	}

	private void OnGoalReached()
	{
		pathTraverser.Active = true;
	}
}
