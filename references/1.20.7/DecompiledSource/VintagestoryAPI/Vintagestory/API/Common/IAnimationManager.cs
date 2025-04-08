using System;
using System.Collections.Generic;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common;

/// <summary>
/// Everything needed for allowing animations the <see cref="T:Vintagestory.API.Common.Entities.Entity" /> class holds a reference to an IAnimator. 
/// Currently implemented by <see cref="T:Vintagestory.API.Common.ServerAnimator" />
/// </summary>
public interface IAnimationManager : IDisposable
{
	/// <summary>
	/// The animator for this animation manager
	/// </summary>
	IAnimator Animator { get; set; }

	/// <summary>
	/// The head controller for this manager.
	/// </summary>
	EntityHeadController HeadController { get; set; }

	/// <summary>
	/// Whether or not the animation is dirty.
	/// </summary>
	bool AnimationsDirty { get; set; }

	/// <summary>
	/// Gets the AnimationMetaData for the target action.
	/// </summary>
	Dictionary<string, AnimationMetaData> ActiveAnimationsByAnimCode { get; }

	event StartAnimationDelegate OnStartAnimation;

	event StartAnimationDelegate OnAnimationReceived;

	event Action<string> OnAnimationStopped;

	/// <summary>
	/// Initialization call for the animation manager.
	/// </summary>
	/// <param name="api">The core API</param>
	/// <param name="entity">The entity being animated.</param>
	void Init(ICoreAPI api, Entity entity);

	bool IsAnimationActive(params string[] anims);

	RunningAnimation GetAnimationState(string anim);

	/// <summary>
	/// Starts an animation based on the AnimationMetaData, if it exists; returns false if it does not exist (or if unable to start it, e.g. because it is already playing)
	/// </summary>
	/// <param name="animdata"></param>
	/// <returns></returns>
	bool TryStartAnimation(AnimationMetaData animdata);

	/// <summary>
	/// Starts an animation based on the AnimationMetaData
	/// </summary>
	/// <param name="animdata"></param>
	/// <returns></returns>
	bool StartAnimation(AnimationMetaData animdata);

	/// <summary>
	/// Starts an animation based on JSON code.
	/// </summary>
	/// <param name="configCode">The json code.</param>
	/// <returns></returns>
	bool StartAnimation(string configCode);

	/// <summary>
	/// Stops the animation.
	/// </summary>
	/// <param name="code">The code to stop the animation on</param>
	void StopAnimation(string code);

	/// <summary>
	/// Additional attributes applied to the animation
	/// </summary>
	/// <param name="tree"></param>
	/// <param name="version"></param>
	void FromAttributes(ITreeAttribute tree, string version);

	/// <summary>
	/// Additional attributes applied from the animation
	/// </summary>
	/// <param name="tree"></param>
	/// <param name="forClient"></param>
	void ToAttributes(ITreeAttribute tree, bool forClient);

	/// <summary>
	/// The event fired when the client recieves the server animations
	/// </summary>
	/// <param name="activeAnimations">all of active animations</param>
	/// <param name="activeAnimationsCount">the number of the animations</param>
	/// <param name="activeAnimationSpeeds">The speed of those animations.</param>
	void OnReceivedServerAnimations(int[] activeAnimations, int activeAnimationsCount, float[] activeAnimationSpeeds);

	/// <summary>
	/// The event fired when the animation is stopped.
	/// </summary>
	/// <param name="code">The code that the animation stopped with.</param>
	void TriggerAnimationStopped(string code);

	void ShouldPlaySound(AnimationSound sound);

	void OnServerTick(float dt);

	void OnClientFrame(float dt);

	/// <summary>
	/// If given animation is running, will set its progress to the first animation frame
	/// </summary>
	/// <param name="beginholdAnim"></param>
	void ResetAnimation(string beginholdAnim);

	void RegisterFrameCallback(AnimFrameCallback trigger);

	IAnimator LoadAnimator(ICoreAPI api, Entity entity, Shape entityShape, RunningAnimation[] copyOverAnims, bool requirePosesOnServer, params string[] requireJointsForElements);

	void CopyOverAnimStates(RunningAnimation[] copyOverAnims, IAnimator animator);
}
