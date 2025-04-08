using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

/// <summary>
/// Syncs every frame with entity.ActiveAnimationsByAnimCode, starts, progresses and stops animations when necessary 
/// </summary>
public abstract class AnimatorBase : IAnimator
{
	public static readonly float[] identMat = Mat4f.Create();

	private WalkSpeedSupplierDelegate WalkSpeedSupplier;

	private Action<string> onAnimationStoppedListener;

	protected int activeAnimCount;

	public ShapeElement[] RootElements;

	public List<ElementPose> RootPoses;

	public RunningAnimation[] anims;

	/// <summary>
	/// We skip the last row - https://stackoverflow.com/questions/32565827/whats-the-purpose-of-magic-4-of-last-row-in-matrix-4x4-for-3d-graphics 
	/// </summary>
	public float[] TransformationMatrices = new float[16 * GlobalConstants.MaxAnimatedElements];

	/// <summary>
	/// The entities default pose. Meaning for most elements this is the identity matrix, with exception of individually controlled elements such as the head.
	/// </summary>
	public float[] TransformationMatricesDefaultPose = new float[16 * GlobalConstants.MaxAnimatedElements];

	public Dictionary<string, AttachmentPointAndPose> AttachmentPointByCode = new Dictionary<string, AttachmentPointAndPose>();

	public RunningAnimation[] CurAnims = new RunningAnimation[20];

	private float accum = 0.25f;

	private double walkSpeed;

	public bool CalculateMatrices { get; set; } = true;


	public float[] Matrices
	{
		get
		{
			if (activeAnimCount <= 0)
			{
				return TransformationMatricesDefaultPose;
			}
			return TransformationMatrices;
		}
	}

	public int ActiveAnimationCount => activeAnimCount;

	[Obsolete("Use Animations instead")]
	public RunningAnimation[] RunningAnimations => Animations;

	public RunningAnimation[] Animations => anims;

	public abstract int MaxJointId { get; }

	public RunningAnimation GetAnimationState(string code)
	{
		for (int i = 0; i < anims.Length; i++)
		{
			RunningAnimation anim = anims[i];
			if (anim.Animation.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
			{
				return anim;
			}
		}
		return null;
	}

	public AnimatorBase(WalkSpeedSupplierDelegate WalkSpeedSupplier, Animation[] Animations, Action<string> onAnimationStoppedListener = null)
	{
		this.WalkSpeedSupplier = WalkSpeedSupplier;
		this.onAnimationStoppedListener = onAnimationStoppedListener;
		anims = new RunningAnimation[(Animations != null) ? Animations.Length : 0];
		for (int j = 0; j < anims.Length; j++)
		{
			Animations[j].Code = Animations[j].Code.ToLower();
			anims[j] = new RunningAnimation
			{
				Active = false,
				Running = false,
				Animation = Animations[j],
				CurrentFrame = 0f
			};
		}
		for (int i = 0; i < TransformationMatricesDefaultPose.Length; i++)
		{
			TransformationMatricesDefaultPose[i] = identMat[i % 16];
		}
	}

	public virtual void OnFrame(Dictionary<string, AnimationMetaData> activeAnimationsByAnimCode, float dt)
	{
		activeAnimCount = 0;
		accum += dt;
		if (accum > 0.25f)
		{
			walkSpeed = ((WalkSpeedSupplier == null) ? 1.0 : WalkSpeedSupplier());
			accum = 0f;
		}
		for (int i = 0; i < anims.Length; i++)
		{
			RunningAnimation anim = anims[i];
			activeAnimationsByAnimCode.TryGetValue(anim.Animation.Code, out var animData);
			bool active = anim.Active;
			anim.Active = animData != null;
			if (!active && anim.Active)
			{
				AnimNowActive(anim, animData);
			}
			if (active && !anim.Active)
			{
				if (anim.Animation.OnActivityStopped == EnumEntityActivityStoppedHandling.Rewind)
				{
					anim.ShouldRewind = true;
				}
				if (anim.Animation.OnActivityStopped == EnumEntityActivityStoppedHandling.Stop)
				{
					anim.Stop();
					activeAnimationsByAnimCode.Remove(anim.Animation.Code);
					onAnimationStoppedListener?.Invoke(anim.Animation.Code);
				}
				if (anim.Animation.OnActivityStopped == EnumEntityActivityStoppedHandling.PlayTillEnd)
				{
					anim.ShouldPlayTillEnd = true;
				}
			}
			if (!anim.Running)
			{
				continue;
			}
			if ((anim.Iterations > 0 && anim.Animation.OnAnimationEnd == EnumEntityAnimationEndHandling.Stop) || (anim.Iterations > 0 && !anim.Active && (anim.Animation.OnActivityStopped == EnumEntityActivityStoppedHandling.PlayTillEnd || anim.Animation.OnActivityStopped == EnumEntityActivityStoppedHandling.EaseOut) && anim.EasingFactor < 0.002f) || (anim.Iterations > 0 && anim.Animation.OnAnimationEnd == EnumEntityAnimationEndHandling.EaseOut && anim.EasingFactor < 0.002f) || (anim.Iterations < 0 && !anim.Active && anim.Animation.OnActivityStopped == EnumEntityActivityStoppedHandling.Rewind && anim.EasingFactor < 0.002f))
			{
				anim.Stop();
				if (anim.Animation.OnAnimationEnd == EnumEntityAnimationEndHandling.Stop || anim.Animation.OnAnimationEnd == EnumEntityAnimationEndHandling.EaseOut)
				{
					activeAnimationsByAnimCode.Remove(anim.Animation.Code);
					onAnimationStoppedListener?.Invoke(anim.Animation.Code);
				}
				continue;
			}
			CurAnims[activeAnimCount] = anim;
			if ((anim.Animation.OnAnimationEnd == EnumEntityAnimationEndHandling.Hold && anim.Iterations != 0 && !anim.Active) || (anim.Animation.OnAnimationEnd == EnumEntityAnimationEndHandling.EaseOut && anim.Iterations != 0))
			{
				anim.EaseOut(dt);
			}
			anim.Progress(dt, (float)walkSpeed);
			activeAnimCount++;
		}
		calculateMatrices(dt);
	}

	public virtual string DumpCurrentState()
	{
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < anims.Length; i++)
		{
			RunningAnimation anim = anims[i];
			if (anim.Active && anim.Running)
			{
				sb.Append("Active&Running: " + anim.Animation.Code);
			}
			else if (anim.Active)
			{
				sb.Append("Active: " + anim.Animation.Code);
			}
			else
			{
				if (!anim.Running)
				{
					continue;
				}
				sb.Append("Running: " + anim.Animation.Code);
			}
			sb.Append(", easing: " + anim.EasingFactor);
			sb.Append(", currentframe: " + anim.CurrentFrame);
			sb.Append(", iterations: " + anim.Iterations);
			sb.Append(", blendedweight: " + anim.BlendedWeight);
			sb.Append(", animmetacode: " + anim.meta.Code);
			sb.AppendLine();
		}
		return sb.ToString();
	}

	protected virtual void AnimNowActive(RunningAnimation anim, AnimationMetaData animData)
	{
		anim.Running = true;
		anim.Active = true;
		anim.meta = animData;
		anim.ShouldRewind = false;
		anim.ShouldPlayTillEnd = false;
		anim.CurrentFrame = animData.StartFrameOnce;
		animData.StartFrameOnce = 0f;
	}

	protected abstract void calculateMatrices(float dt);

	public AttachmentPointAndPose GetAttachmentPointPose(string code)
	{
		AttachmentPointByCode.TryGetValue(code, out var apap);
		return apap;
	}

	public virtual ElementPose GetPosebyName(string name, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
	{
		throw new NotImplementedException();
	}

	public virtual void ReloadAttachmentPoints()
	{
		throw new NotImplementedException();
	}
}
