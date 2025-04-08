using System;
using System.Collections.Generic;

namespace Vintagestory.API.Common;

public interface IAnimator
{
	int MaxJointId { get; }

	/// <summary>
	/// The 30 pose transformation matrices that go to the shader
	/// </summary>
	float[] Matrices { get; }

	/// <summary>
	/// Amount of currently active animations
	/// </summary>
	int ActiveAnimationCount { get; }

	/// <summary>
	/// Holds data over all animations. This list always contains all animations of the creature. You have to check yourself which of them are active
	/// </summary>
	RunningAnimation[] Animations { get; }

	/// <summary>
	/// Whether or not to calculate the animation matrices, required for GetAttachmentPointPose() to deliver correct values. Default on on the client, server side only on when the creature is dead
	/// </summary>
	bool CalculateMatrices { get; set; }

	RunningAnimation GetAnimationState(string code);

	/// <summary>
	/// Gets the attachment point pose.
	/// </summary>
	/// <param name="code"></param>
	/// <returns></returns>
	AttachmentPointAndPose GetAttachmentPointPose(string code);

	ElementPose GetPosebyName(string name, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase);

	/// <summary>
	/// The event fired on each frame.
	/// </summary>
	/// <param name="activeAnimationsByAnimCode"></param>
	/// <param name="dt"></param>
	void OnFrame(Dictionary<string, AnimationMetaData> activeAnimationsByAnimCode, float dt);

	string DumpCurrentState();
}
