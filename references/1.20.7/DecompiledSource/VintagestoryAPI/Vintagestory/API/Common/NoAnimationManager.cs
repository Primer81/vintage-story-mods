using System;
using System.Collections.Generic;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common;

/// <summary>
/// A No-Animation Manager built off of <see cref="T:Vintagestory.API.Common.IAnimationManager" />.
/// </summary>
public class NoAnimationManager : IAnimationManager, IDisposable
{
	public IAnimator Animator { get; set; }

	public bool AnimationsDirty { get; set; }

	public Dictionary<string, AnimationMetaData> ActiveAnimationsByAnimCode => new Dictionary<string, AnimationMetaData>();

	public EntityHeadController HeadController { get; set; }

	public event StartAnimationDelegate OnStartAnimation;

	public event Action<string> OnAnimationStopped;

	public event StartAnimationDelegate OnAnimationReceived;

	public NoAnimationManager()
	{
		Animator = new NoAnimator();
	}

	public void CopyOverAnimStates(RunningAnimation[] copyOverAnims, IAnimator animator)
	{
		throw new NotImplementedException();
	}

	public void Dispose()
	{
	}

	public void FromAttributes(ITreeAttribute tree, string version)
	{
	}

	public RunningAnimation GetAnimationState(string anim)
	{
		throw new NotImplementedException();
	}

	public void Init(ICoreAPI api, Entity entity)
	{
	}

	public bool IsAnimationActive(params string[] anims)
	{
		return false;
	}

	public IAnimator LoadAnimator(ICoreAPI api, Entity entity, Shape entityShape, RunningAnimation[] copyOverAnims, bool requirePosesOnServer, params string[] requireJointsForElements)
	{
		throw new NotImplementedException();
	}

	public void TriggerAnimationStopped(string code)
	{
	}

	public void OnClientFrame(float dt)
	{
	}

	public void OnReceivedServerAnimations(int[] activeAnimations, int activeAnimationsCount, float[] activeAnimationSpeeds)
	{
	}

	public void OnServerTick(float dt)
	{
	}

	public void RegisterFrameCallback(AnimFrameCallback trigger)
	{
		throw new NotImplementedException();
	}

	public void ResetAnimation(string beginholdAnim)
	{
	}

	public bool TryStartAnimation(AnimationMetaData animdata)
	{
		return false;
	}

	public bool StartAnimation(AnimationMetaData animdata)
	{
		return false;
	}

	public bool StartAnimation(string configCode)
	{
		return false;
	}

	public void StopAnimation(string code)
	{
	}

	public void ToAttributes(ITreeAttribute tree, bool forClient)
	{
	}

	public void ShouldPlaySound(AnimationSound sound)
	{
	}
}
