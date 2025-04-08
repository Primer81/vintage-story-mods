using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

/// <summary>
/// Syncs every frame with entity.ActiveAnimationsByAnimCode, starts and stops animations when necessary 
/// and does recursive interpolation on the rotation, position and scale value for each frame, for each element and for each active element
/// this produces always correctly blended animations but is significantly more costly for the cpu when compared to the technique used by the <see cref="T:Vintagestory.API.Common.AnimatorBase" />.
/// </summary>
public class ClientAnimator : AnimatorBase
{
	protected HashSet<int> jointsDone = new HashSet<int>();

	public Dictionary<int, AnimationJoint> jointsById;

	public static int MaxConcurrentAnimations = 16;

	private int maxDepth;

	private List<ElementPose>[][] frameByDepthByAnimation;

	private List<ElementPose>[][] nextFrameTransformsByAnimation;

	private ShapeElementWeights[][][] weightsByAnimationAndElement;

	private float[] localTransformMatrix = Mat4f.Create();

	private float[] tmpMatrix = Mat4f.Create();

	private Action<AnimationSound> onShouldPlaySoundListener;

	private int[] prevFrame = new int[MaxConcurrentAnimations];

	private int[] nextFrame = new int[MaxConcurrentAnimations];

	private static bool EleWeightDebug = false;

	private Dictionary<string, string> eleWeights = new Dictionary<string, string>();

	public override int MaxJointId => jointsById.Count + 1;

	public static ClientAnimator CreateForEntity(Entity entity, List<ElementPose> rootPoses, Animation[] animations, ShapeElement[] rootElements, Dictionary<int, AnimationJoint> jointsById)
	{
		if (entity is EntityAgent)
		{
			EntityAgent entityag = entity as EntityAgent;
			return new ClientAnimator(() => (double)entityag.Controls.MovespeedMultiplier * entityag.GetWalkSpeedMultiplier(), rootPoses, animations, rootElements, jointsById, entity.AnimManager.TriggerAnimationStopped, entity.AnimManager.ShouldPlaySound);
		}
		return new ClientAnimator(() => 1.0, rootPoses, animations, rootElements, jointsById, entity.AnimManager.TriggerAnimationStopped, entity.AnimManager.ShouldPlaySound);
	}

	public static ClientAnimator CreateForEntity(Entity entity, Animation[] animations, ShapeElement[] rootElements, Dictionary<int, AnimationJoint> jointsById)
	{
		if (entity is EntityAgent)
		{
			EntityAgent entityag = entity as EntityAgent;
			return new ClientAnimator(() => (double)entityag.Controls.MovespeedMultiplier * entityag.GetWalkSpeedMultiplier(), animations, rootElements, jointsById, entity.AnimManager.TriggerAnimationStopped, entity.AnimManager.ShouldPlaySound);
		}
		return new ClientAnimator(() => 1.0, animations, rootElements, jointsById, entity.AnimManager.TriggerAnimationStopped, entity.AnimManager.ShouldPlaySound);
	}

	public ClientAnimator(WalkSpeedSupplierDelegate walkSpeedSupplier, Animation[] animations, Action<string> onAnimationStoppedListener = null, Action<AnimationSound> onShouldPlaySoundListener = null)
		: base(walkSpeedSupplier, animations, onAnimationStoppedListener)
	{
		this.onShouldPlaySoundListener = onShouldPlaySoundListener;
		initFields();
	}

	public ClientAnimator(WalkSpeedSupplierDelegate walkSpeedSupplier, List<ElementPose> rootPoses, Animation[] animations, ShapeElement[] rootElements, Dictionary<int, AnimationJoint> jointsById, Action<string> onAnimationStoppedListener = null, Action<AnimationSound> onShouldPlaySoundListener = null)
		: base(walkSpeedSupplier, animations, onAnimationStoppedListener)
	{
		RootElements = rootElements;
		this.jointsById = jointsById;
		RootPoses = rootPoses;
		this.onShouldPlaySoundListener = onShouldPlaySoundListener;
		LoadAttachmentPoints(RootPoses);
		initFields();
	}

	public ClientAnimator(WalkSpeedSupplierDelegate walkSpeedSupplier, Animation[] animations, ShapeElement[] rootElements, Dictionary<int, AnimationJoint> jointsById, Action<string> onAnimationStoppedListener = null, Action<AnimationSound> onShouldPlaySoundListener = null)
		: base(walkSpeedSupplier, animations, onAnimationStoppedListener)
	{
		RootElements = rootElements;
		this.jointsById = jointsById;
		RootPoses = new List<ElementPose>();
		LoadPosesAndAttachmentPoints(rootElements, RootPoses);
		this.onShouldPlaySoundListener = onShouldPlaySoundListener;
		initFields();
	}

	protected virtual void initFields()
	{
		maxDepth = 2 + ((RootPoses != null) ? getMaxDepth(RootPoses, 1) : 0);
		frameByDepthByAnimation = new List<ElementPose>[maxDepth][];
		nextFrameTransformsByAnimation = new List<ElementPose>[maxDepth][];
		weightsByAnimationAndElement = new ShapeElementWeights[maxDepth][][];
		for (int i = 0; i < maxDepth; i++)
		{
			frameByDepthByAnimation[i] = new List<ElementPose>[MaxConcurrentAnimations];
			nextFrameTransformsByAnimation[i] = new List<ElementPose>[MaxConcurrentAnimations];
			weightsByAnimationAndElement[i] = new ShapeElementWeights[MaxConcurrentAnimations][];
		}
	}

	public override void ReloadAttachmentPoints()
	{
		LoadAttachmentPoints(RootPoses);
	}

	protected virtual void LoadAttachmentPoints(List<ElementPose> cachedPoses)
	{
		for (int i = 0; i < cachedPoses.Count; i++)
		{
			ElementPose elem = cachedPoses[i];
			if (elem.ForElement.AttachmentPoints != null)
			{
				for (int j = 0; j < elem.ForElement.AttachmentPoints.Length; j++)
				{
					AttachmentPoint apoint = elem.ForElement.AttachmentPoints[j];
					AttachmentPointByCode[apoint.Code] = new AttachmentPointAndPose
					{
						AttachPoint = apoint,
						CachedPose = elem
					};
				}
			}
			if (elem.ChildElementPoses != null)
			{
				LoadAttachmentPoints(elem.ChildElementPoses);
			}
		}
	}

	protected virtual void LoadPosesAndAttachmentPoints(ShapeElement[] elements, List<ElementPose> intoPoses)
	{
		foreach (ShapeElement elem in elements)
		{
			ElementPose pose;
			intoPoses.Add(pose = new ElementPose());
			pose.AnimModelMatrix = Mat4f.Create();
			pose.ForElement = elem;
			if (elem.AttachmentPoints != null)
			{
				for (int j = 0; j < elem.AttachmentPoints.Length; j++)
				{
					AttachmentPoint apoint = elem.AttachmentPoints[j];
					AttachmentPointByCode[apoint.Code] = new AttachmentPointAndPose
					{
						AttachPoint = apoint,
						CachedPose = pose
					};
				}
			}
			if (elem.Children != null)
			{
				pose.ChildElementPoses = new List<ElementPose>(elem.Children.Length);
				LoadPosesAndAttachmentPoints(elem.Children, pose.ChildElementPoses);
			}
		}
	}

	private int getMaxDepth(List<ElementPose> poses, int depth)
	{
		for (int i = 0; i < poses.Count; i++)
		{
			ElementPose pose = poses[i];
			if (pose.ChildElementPoses != null)
			{
				depth = getMaxDepth(pose.ChildElementPoses, depth);
			}
		}
		return depth + 1;
	}

	public override ElementPose GetPosebyName(string name, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
	{
		return getPosebyName(RootPoses, name);
	}

	private ElementPose getPosebyName(List<ElementPose> poses, string name, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
	{
		for (int i = 0; i < poses.Count; i++)
		{
			ElementPose pose = poses[i];
			if (pose.ForElement.Name.Equals(name, stringComparison))
			{
				return pose;
			}
			if (pose.ChildElementPoses != null)
			{
				ElementPose foundPose = getPosebyName(pose.ChildElementPoses, name);
				if (foundPose != null)
				{
					return foundPose;
				}
			}
		}
		return null;
	}

	protected override void AnimNowActive(RunningAnimation anim, AnimationMetaData animData)
	{
		base.AnimNowActive(anim, animData);
		if (anim.Animation.PrevNextKeyFrameByFrame == null)
		{
			anim.Animation.GenerateAllFrames(RootElements, jointsById);
		}
		anim.LoadWeights(RootElements);
	}

	public override void OnFrame(Dictionary<string, AnimationMetaData> activeAnimationsByAnimCode, float dt)
	{
		for (int i = 0; i < activeAnimCount; i++)
		{
			RunningAnimation anim = CurAnims[i];
			if (anim.Animation.PrevNextKeyFrameByFrame == null && anim.Animation.KeyFrames.Length != 0)
			{
				anim.Animation.GenerateAllFrames(RootElements, jointsById);
			}
			if (anim.meta.AnimationSound != null && onShouldPlaySoundListener != null && anim.CurrentFrame >= (float)anim.meta.AnimationSound.Frame && anim.SoundPlayedAtIteration != anim.Iterations && anim.Active)
			{
				onShouldPlaySoundListener(anim.meta.AnimationSound);
				anim.SoundPlayedAtIteration = anim.Iterations;
			}
		}
		base.OnFrame(activeAnimationsByAnimCode, dt);
	}

	protected override void calculateMatrices(float dt)
	{
		if (!base.CalculateMatrices)
		{
			return;
		}
		jointsDone.Clear();
		int animVersion = 0;
		for (int k = 0; k < activeAnimCount; k++)
		{
			RunningAnimation anim = CurAnims[k];
			weightsByAnimationAndElement[0][k] = anim.ElementWeights;
			animVersion = Math.Max(animVersion, anim.Animation.Version);
			AnimationFrame[] prevNextFrame = anim.Animation.PrevNextKeyFrameByFrame[(int)anim.CurrentFrame % anim.Animation.QuantityFrames];
			frameByDepthByAnimation[0][k] = prevNextFrame[0].RootElementTransforms;
			prevFrame[k] = prevNextFrame[0].FrameNumber;
			if (anim.Animation.OnAnimationEnd == EnumEntityAnimationEndHandling.Hold && (int)anim.CurrentFrame + 1 == anim.Animation.QuantityFrames)
			{
				nextFrameTransformsByAnimation[0][k] = prevNextFrame[0].RootElementTransforms;
				nextFrame[k] = prevNextFrame[0].FrameNumber;
			}
			else
			{
				nextFrameTransformsByAnimation[0][k] = prevNextFrame[1].RootElementTransforms;
				nextFrame[k] = prevNextFrame[1].FrameNumber;
			}
		}
		calculateMatrices(animVersion, dt, RootPoses, weightsByAnimationAndElement[0], Mat4f.Create(), frameByDepthByAnimation[0], nextFrameTransformsByAnimation[0], 0);
		for (int jointid = 0; jointid < GlobalConstants.MaxAnimatedElements; jointid++)
		{
			if (!jointsById.ContainsKey(jointid))
			{
				for (int j = 0; j < 16; j++)
				{
					TransformationMatrices[jointid * 16 + j] = AnimatorBase.identMat[j];
				}
			}
		}
		foreach (KeyValuePair<string, AttachmentPointAndPose> val in AttachmentPointByCode)
		{
			for (int i = 0; i < 16; i++)
			{
				val.Value.AnimModelMatrix[i] = val.Value.CachedPose.AnimModelMatrix[i];
			}
		}
	}

	private void calculateMatrices(int animVersion, float dt, List<ElementPose> outFrame, ShapeElementWeights[][] weightsByAnimationAndElement, float[] modelMatrix, List<ElementPose>[] nowKeyFrameByAnimation, List<ElementPose>[] nextInKeyFrameByAnimation, int depth)
	{
		depth++;
		List<ElementPose>[] nowChildKeyFrameByAnimation = frameByDepthByAnimation[depth];
		List<ElementPose>[] nextChildKeyFrameByAnimation = nextFrameTransformsByAnimation[depth];
		ShapeElementWeights[][] childWeightsByAnimationAndElement = this.weightsByAnimationAndElement[depth];
		for (int childPoseIndex = 0; childPoseIndex < outFrame.Count; childPoseIndex++)
		{
			ElementPose outFramePose = outFrame[childPoseIndex];
			ShapeElement elem = outFramePose.ForElement;
			outFramePose.SetMat(modelMatrix);
			Mat4f.Identity(localTransformMatrix);
			outFramePose.Clear();
			float weightSum = 0f;
			for (int animIndex2 = 0; animIndex2 < activeAnimCount; animIndex2++)
			{
				RunningAnimation anim2 = CurAnims[animIndex2];
				ShapeElementWeights sew2 = weightsByAnimationAndElement[animIndex2][childPoseIndex];
				if (sew2.BlendMode != 0)
				{
					weightSum += sew2.Weight * anim2.EasingFactor;
				}
			}
			for (int animIndex = 0; animIndex < activeAnimCount; animIndex++)
			{
				RunningAnimation anim = CurAnims[animIndex];
				ShapeElementWeights sew = weightsByAnimationAndElement[animIndex][childPoseIndex];
				anim.CalcBlendedWeight(weightSum / sew.Weight, sew.BlendMode);
				ElementPose nowFramePose = nowKeyFrameByAnimation[animIndex][childPoseIndex];
				ElementPose nextFramePose = nextInKeyFrameByAnimation[animIndex][childPoseIndex];
				int prevFrame = this.prevFrame[animIndex];
				int nextFrame = this.nextFrame[animIndex];
				float keyFrameDist = ((nextFrame > prevFrame) ? (nextFrame - prevFrame) : (anim.Animation.QuantityFrames - prevFrame + nextFrame));
				float lerp = ((anim.CurrentFrame >= (float)prevFrame) ? (anim.CurrentFrame - (float)prevFrame) : ((float)(anim.Animation.QuantityFrames - prevFrame) + anim.CurrentFrame)) / keyFrameDist;
				outFramePose.Add(nowFramePose, nextFramePose, lerp, anim.BlendedWeight);
				nowChildKeyFrameByAnimation[animIndex] = nowFramePose.ChildElementPoses;
				childWeightsByAnimationAndElement[animIndex] = sew.ChildElements;
				nextChildKeyFrameByAnimation[animIndex] = nextFramePose.ChildElementPoses;
			}
			elem.GetLocalTransformMatrix(animVersion, localTransformMatrix, outFramePose);
			Mat4f.Mul(outFramePose.AnimModelMatrix, outFramePose.AnimModelMatrix, localTransformMatrix);
			if (elem.JointId > 0 && !jointsDone.Contains(elem.JointId))
			{
				Mat4f.Mul(tmpMatrix, outFramePose.AnimModelMatrix, elem.inverseModelTransform);
				int index = 16 * elem.JointId;
				for (int i = 0; i < 16; i++)
				{
					TransformationMatrices[index + i] = tmpMatrix[i];
				}
				jointsDone.Add(elem.JointId);
			}
			if (outFramePose.ChildElementPoses != null)
			{
				calculateMatrices(animVersion, dt, outFramePose.ChildElementPoses, childWeightsByAnimationAndElement, outFramePose.AnimModelMatrix, nowChildKeyFrameByAnimation, nextChildKeyFrameByAnimation, depth);
			}
		}
	}

	public override string DumpCurrentState()
	{
		EleWeightDebug = true;
		eleWeights.Clear();
		calculateMatrices(1f / 60f);
		EleWeightDebug = false;
		return base.DumpCurrentState() + "\nElement weights:\n" + string.Join("\n", eleWeights.Select((KeyValuePair<string, string> x) => x.Key + ": " + x.Value));
	}
}
