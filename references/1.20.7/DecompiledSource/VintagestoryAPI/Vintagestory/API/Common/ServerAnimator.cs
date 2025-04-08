using System;
using System.Collections.Generic;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.API.Common;

public class ServerAnimator : ClientAnimator
{
	public static ServerAnimator CreateForEntity(Entity entity, List<ElementPose> rootPoses, Animation[] animations, ShapeElement[] rootElements, Dictionary<int, AnimationJoint> jointsById, bool requirePosesOnServer)
	{
		if (entity is EntityAgent)
		{
			EntityAgent entityag = entity as EntityAgent;
			return new ServerAnimator(() => (double)entityag.Controls.MovespeedMultiplier * entityag.GetWalkSpeedMultiplier(), rootPoses, animations, rootElements, jointsById, entity.AnimManager.TriggerAnimationStopped, requirePosesOnServer);
		}
		return new ServerAnimator(() => 1.0, rootPoses, animations, rootElements, jointsById, entity.AnimManager.TriggerAnimationStopped, requirePosesOnServer);
	}

	public static ServerAnimator CreateForEntity(Entity entity, Animation[] animations, ShapeElement[] rootElements, Dictionary<int, AnimationJoint> jointsById, bool requirePosesOnServer)
	{
		if (entity is EntityAgent)
		{
			EntityAgent entityag = entity as EntityAgent;
			return new ServerAnimator(() => (double)entityag.Controls.MovespeedMultiplier * entityag.GetWalkSpeedMultiplier(), animations, rootElements, jointsById, entity.AnimManager.TriggerAnimationStopped, requirePosesOnServer);
		}
		return new ServerAnimator(() => 1.0, animations, rootElements, jointsById, entity.AnimManager.TriggerAnimationStopped, requirePosesOnServer);
	}

	public ServerAnimator(WalkSpeedSupplierDelegate walkSpeedSupplier, Animation[] animations, ShapeElement[] rootElements, Dictionary<int, AnimationJoint> jointsById, Action<string> onAnimationStoppedListener = null, bool loadFully = false)
		: base(walkSpeedSupplier, animations, onAnimationStoppedListener)
	{
		RootElements = rootElements;
		base.jointsById = jointsById;
		RootPoses = new List<ElementPose>();
		LoadPosesAndAttachmentPoints(rootElements, RootPoses);
		initFields();
	}

	public ServerAnimator(WalkSpeedSupplierDelegate walkSpeedSupplier, List<ElementPose> rootPoses, Animation[] animations, ShapeElement[] rootElements, Dictionary<int, AnimationJoint> jointsById, Action<string> onAnimationStoppedListener = null, bool loadFully = false)
		: base(walkSpeedSupplier, animations, onAnimationStoppedListener)
	{
		RootElements = rootElements;
		base.jointsById = jointsById;
		RootPoses = rootPoses;
		LoadAttachmentPoints(RootPoses);
		initFields();
	}

	protected override void LoadPosesAndAttachmentPoints(ShapeElement[] elements, List<ElementPose> intoPoses)
	{
		base.LoadPosesAndAttachmentPoints(elements, intoPoses);
	}
}
