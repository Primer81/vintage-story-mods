using System;
using System.Collections.Generic;

namespace Vintagestory.API.Common;

/// <summary>
/// A NoAnimator built off of <see cref="T:Vintagestory.API.Common.IAnimator" />
/// </summary>
public class NoAnimator : IAnimator
{
	/// <summary>
	/// The matrices for this No-Animator
	/// </summary>
	public float[] Matrices => null;

	/// <summary>
	/// The active animation count for this no animator.
	/// </summary>
	public int ActiveAnimationCount => 0;

	public bool CalculateMatrices { get; set; }

	public RunningAnimation[] Animations => new RunningAnimation[0];

	public int MaxJointId
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public string DumpCurrentState()
	{
		throw new NotImplementedException();
	}

	public RunningAnimation GetAnimationState(string code)
	{
		return null;
	}

	/// <summary>
	/// Gets the attachment point for this pose.
	/// </summary>
	/// <param name="code"></param>
	/// <returns></returns>
	public AttachmentPointAndPose GetAttachmentPointPose(string code)
	{
		return null;
	}

	public ElementPose GetPosebyName(string name, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
	{
		return null;
	}

	/// <summary>
	/// The event fired when a specified frame has been hit.
	/// </summary>
	/// <param name="activeAnimationsByAnimCode"></param>
	/// <param name="dt"></param>
	public void OnFrame(Dictionary<string, AnimationMetaData> activeAnimationsByAnimCode, float dt)
	{
	}

	public void ReloadAttachmentPoints()
	{
	}
}
