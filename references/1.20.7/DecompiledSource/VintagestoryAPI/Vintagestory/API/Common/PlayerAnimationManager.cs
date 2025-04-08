using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public class PlayerAnimationManager : AnimationManager
{
	public bool UseFpAnmations = true;

	private EntityPlayer plrEntity;

	protected string lastActiveHeldReadyAnimation;

	protected string lastActiveRightHeldIdleAnimation;

	protected string lastActiveLeftHeldIdleAnimation;

	protected string lastActiveHeldHitAnimation;

	protected string lastActiveHeldUseAnimation;

	public string lastRunningHeldHitAnimation;

	public string lastRunningHeldUseAnimation;

	private bool useFpAnimSet
	{
		get
		{
			if (UseFpAnmations && api.Side == EnumAppSide.Client && capi.World.Player.Entity.EntityId == entity.EntityId)
			{
				return capi.World.Player.CameraMode == EnumCameraMode.FirstPerson;
			}
			return false;
		}
	}

	private string fpEnding
	{
		get
		{
			if (UseFpAnmations)
			{
				ICoreClientAPI coreClientAPI = capi;
				if (coreClientAPI != null && coreClientAPI.World.Player.CameraMode == EnumCameraMode.FirstPerson)
				{
					ICoreClientAPI obj = api as ICoreClientAPI;
					if (obj == null || !obj.Settings.Bool["immersiveFpMode"])
					{
						return "-fp";
					}
					return "-ifp";
				}
			}
			return "";
		}
	}

	public override void Init(ICoreAPI api, Entity entity)
	{
		base.Init(api, entity);
		plrEntity = entity as EntityPlayer;
	}

	public override void OnClientFrame(float dt)
	{
		base.OnClientFrame(dt);
		if (useFpAnimSet)
		{
			plrEntity.TpAnimManager.OnClientFrame(dt);
		}
	}

	public override void ResetAnimation(string animCode)
	{
		base.ResetAnimation(animCode);
		base.ResetAnimation(animCode + "-ifp");
		base.ResetAnimation(animCode + "-fp");
	}

	public override bool StartAnimation(string configCode)
	{
		if (configCode == null)
		{
			return false;
		}
		AnimationMetaData animdata2;
		if (useFpAnimSet)
		{
			plrEntity.TpAnimManager.StartAnimationBase(configCode);
			if (entity.Properties.Client.AnimationsByMetaCode.TryGetValue(configCode + fpEnding, out var animdata))
			{
				StartAnimation(animdata);
				return true;
			}
		}
		else if (entity.Properties.Client.AnimationsByMetaCode.TryGetValue(configCode + fpEnding, out animdata2))
		{
			plrEntity.SelfFpAnimManager.StartAnimationBase(animdata2);
		}
		return base.StartAnimation(configCode);
	}

	public override bool StartAnimation(AnimationMetaData animdata)
	{
		if (useFpAnimSet && !animdata.Code.EndsWithOrdinal(fpEnding))
		{
			plrEntity.TpAnimManager.StartAnimation(animdata);
			if (animdata.WithFpVariant)
			{
				if (ActiveAnimationsByAnimCode.TryGetValue(animdata.FpVariant.Animation, out var activeAnimdata) && activeAnimdata == animdata.FpVariant)
				{
					return false;
				}
				return base.StartAnimation(animdata.FpVariant);
			}
			if (entity.Properties.Client.AnimationsByMetaCode.TryGetValue(animdata.Code + fpEnding, out var animdatafp))
			{
				if (ActiveAnimationsByAnimCode.TryGetValue(animdatafp.Animation, out var activeAnimdata2) && activeAnimdata2 == animdatafp)
				{
					return false;
				}
				return base.StartAnimation(animdatafp);
			}
		}
		return base.StartAnimation(animdata);
	}

	public bool StartAnimationBase(string configCode)
	{
		if (entity.Properties.Client.AnimationsByMetaCode.TryGetValue(configCode + fpEnding, out var animdata))
		{
			StartAnimation(animdata);
			return true;
		}
		return base.StartAnimation(configCode);
	}

	public bool StartAnimationBase(AnimationMetaData animdata)
	{
		return base.StartAnimation(animdata);
	}

	public override void RegisterFrameCallback(AnimFrameCallback trigger)
	{
		if (useFpAnimSet && !trigger.Animation.EndsWithOrdinal(fpEnding) && entity.Properties.Client.AnimationsByMetaCode.ContainsKey(trigger.Animation + fpEnding))
		{
			trigger.Animation += fpEnding;
		}
		base.RegisterFrameCallback(trigger);
	}

	public override void StopAnimation(string code)
	{
		if (code != null)
		{
			if (api.Side == EnumAppSide.Client)
			{
				(plrEntity.OtherAnimManager as PlayerAnimationManager).StopSelfAnimation(code);
			}
			StopSelfAnimation(code);
		}
	}

	public void StopSelfAnimation(string code)
	{
		string[] array = new string[3]
		{
			code,
			code + "-ifp",
			code + "-fp"
		};
		foreach (string anim in array)
		{
			base.StopAnimation(anim);
		}
	}

	public override bool IsAnimationActive(params string[] anims)
	{
		if (useFpAnimSet)
		{
			foreach (string val in anims)
			{
				if (ActiveAnimationsByAnimCode.ContainsKey(val + fpEnding))
				{
					return true;
				}
			}
		}
		return base.IsAnimationActive(anims);
	}

	public override RunningAnimation GetAnimationState(string anim)
	{
		if (useFpAnimSet && !anim.EndsWithOrdinal(fpEnding) && entity.Properties.Client.AnimationsByMetaCode.ContainsKey(anim + fpEnding))
		{
			return base.GetAnimationState(anim + fpEnding);
		}
		return base.GetAnimationState(anim);
	}

	public bool IsAnimationActiveOrRunning(string anim, float untilProgress = 0.95f)
	{
		if (anim == null || base.Animator == null)
		{
			return false;
		}
		if (!IsAnimationMostlyRunning(anim, untilProgress))
		{
			return IsAnimationMostlyRunning(anim + fpEnding, untilProgress);
		}
		return true;
	}

	protected bool IsAnimationMostlyRunning(string anim, float untilProgress = 0.95f)
	{
		RunningAnimation ranim = base.Animator.GetAnimationState(anim);
		if (ranim != null && ranim.Running && ranim.AnimProgress < untilProgress)
		{
			return ranim.Active;
		}
		return false;
	}

	protected override void onReceivedServerAnimation(AnimationMetaData animmetadata)
	{
		StartAnimation(animmetadata);
	}

	public override void OnReceivedServerAnimations(int[] activeAnimations, int activeAnimationsCount, float[] activeAnimationSpeeds)
	{
		base.OnReceivedServerAnimations(activeAnimations, activeAnimationsCount, activeAnimationSpeeds);
	}

	public void OnActiveSlotChanged(ItemSlot slot)
	{
		string beginholdAnim = slot.Itemstack?.Collectible?.GetHeldReadyAnimation(slot, entity, EnumHand.Right);
		if (beginholdAnim != lastActiveHeldReadyAnimation)
		{
			StopHeldReadyAnim();
		}
		if (beginholdAnim != null)
		{
			StartHeldReadyAnim(beginholdAnim);
		}
		lastActiveHeldHitAnimation = null;
	}

	public void StartHeldReadyAnim(string heldReadyAnim, bool force = false)
	{
		if (force || (!IsHeldHitActive() && !IsHeldUseActive()))
		{
			if (lastActiveHeldReadyAnimation != null)
			{
				StopAnimation(lastActiveHeldReadyAnimation);
			}
			ResetAnimation(heldReadyAnim);
			StartAnimation(heldReadyAnim);
			lastActiveHeldReadyAnimation = heldReadyAnim;
		}
	}

	public void StartHeldUseAnim(string animCode)
	{
		StopHeldReadyAnim();
		StopAnimation(lastActiveRightHeldIdleAnimation);
		StopAnimation(lastActiveHeldHitAnimation);
		StartAnimation(animCode);
		lastActiveHeldUseAnimation = animCode;
		lastRunningHeldUseAnimation = animCode;
	}

	public void StartHeldHitAnim(string animCode)
	{
		StopHeldReadyAnim();
		StopAnimation(lastActiveRightHeldIdleAnimation);
		StopAnimation(lastActiveHeldUseAnimation);
		StartAnimation(animCode);
		lastActiveHeldHitAnimation = animCode;
		lastRunningHeldHitAnimation = animCode;
	}

	public void StartRightHeldIdleAnim(string animCode)
	{
		StopAnimation(lastActiveRightHeldIdleAnimation);
		StopAnimation(lastActiveHeldUseAnimation);
		StartAnimation(animCode);
		lastActiveRightHeldIdleAnimation = animCode;
	}

	public void StartLeftHeldIdleAnim(string animCode)
	{
		StopAnimation(lastActiveLeftHeldIdleAnimation);
		StartAnimation(animCode);
		lastActiveLeftHeldIdleAnimation = animCode;
	}

	public void StopHeldReadyAnim()
	{
		if (!plrEntity.RightHandItemSlot.Empty)
		{
			JsonObject itemAttributes = plrEntity.RightHandItemSlot.Itemstack.ItemAttributes;
			if (itemAttributes != null && itemAttributes.IsTrue("alwaysPlayHeldReady"))
			{
				return;
			}
		}
		StopAnimation(lastActiveHeldReadyAnimation);
		lastActiveHeldReadyAnimation = null;
	}

	public void StopHeldUseAnim()
	{
		StopAnimation(lastActiveHeldUseAnimation);
		lastActiveHeldUseAnimation = null;
	}

	public void StopHeldAttackAnim()
	{
		if (lastActiveHeldHitAnimation != null && entity.Properties.Client.AnimationsByMetaCode.TryGetValue(lastActiveHeldHitAnimation, out var animData))
		{
			JsonObject attributes = animData.Attributes;
			if (attributes != null && attributes.IsTrue("authorative") && IsHeldHitActive())
			{
				return;
			}
		}
		StopAnimation(lastActiveHeldHitAnimation);
		lastActiveHeldHitAnimation = null;
	}

	public void StopRightHeldIdleAnim()
	{
		StopAnimation(lastActiveRightHeldIdleAnimation);
		lastActiveRightHeldIdleAnimation = null;
	}

	public void StopLeftHeldIdleAnim()
	{
		StopAnimation(lastActiveLeftHeldIdleAnimation);
		lastActiveLeftHeldIdleAnimation = null;
	}

	public bool IsHeldHitAuthoritative()
	{
		return IsAuthoritative(lastActiveHeldHitAnimation);
	}

	public bool IsAuthoritative(string anim)
	{
		if (anim == null)
		{
			return false;
		}
		if (entity.Properties.Client.AnimationsByMetaCode.TryGetValue(anim, out var animData))
		{
			return animData.Attributes?.IsTrue("authorative") ?? false;
		}
		return false;
	}

	public bool IsHeldUseActive()
	{
		if (lastActiveHeldUseAnimation != null)
		{
			return IsAnimationActiveOrRunning(lastActiveHeldUseAnimation);
		}
		return false;
	}

	public bool IsHeldHitActive(float untilProgress = 0.95f)
	{
		if (lastActiveHeldHitAnimation != null)
		{
			return IsAnimationActiveOrRunning(lastActiveHeldHitAnimation, untilProgress);
		}
		return false;
	}

	public bool IsLeftHeldActive()
	{
		if (lastActiveLeftHeldIdleAnimation != null)
		{
			return IsAnimationActiveOrRunning(lastActiveLeftHeldIdleAnimation);
		}
		return false;
	}

	public bool IsRightHeldActive()
	{
		if (lastActiveRightHeldIdleAnimation != null)
		{
			return IsAnimationActiveOrRunning(lastActiveRightHeldIdleAnimation);
		}
		return false;
	}

	public bool IsRightHeldReadyActive()
	{
		if (lastActiveHeldReadyAnimation != null)
		{
			return IsAnimationActiveOrRunning(lastActiveHeldReadyAnimation);
		}
		return false;
	}

	public bool HeldRightReadyAnimChanged(string nowHeldRightReadyAnim)
	{
		if (lastActiveHeldReadyAnimation != null)
		{
			return nowHeldRightReadyAnim != lastActiveHeldReadyAnimation;
		}
		return false;
	}

	public bool HeldUseAnimChanged(string nowHeldRightUseAnim)
	{
		if (lastActiveHeldUseAnimation != null)
		{
			return nowHeldRightUseAnim != lastActiveHeldUseAnimation;
		}
		return false;
	}

	public bool HeldHitAnimChanged(string nowHeldRightHitAnim)
	{
		if (lastActiveHeldHitAnimation != null)
		{
			return nowHeldRightHitAnim != lastActiveHeldHitAnimation;
		}
		return false;
	}

	public bool RightHeldIdleChanged(string nowHeldRightIdleAnim)
	{
		if (lastActiveRightHeldIdleAnimation != null)
		{
			return nowHeldRightIdleAnim != lastActiveRightHeldIdleAnimation;
		}
		return false;
	}

	public bool LeftHeldIdleChanged(string nowHeldLeftIdleAnim)
	{
		if (lastActiveLeftHeldIdleAnimation != null)
		{
			return nowHeldLeftIdleAnim != lastActiveLeftHeldIdleAnimation;
		}
		return false;
	}

	public override void FromAttributes(ITreeAttribute tree, string version)
	{
		if (entity == null || capi?.World.Player.Entity.EntityId != entity.EntityId)
		{
			base.FromAttributes(tree, version);
		}
		lastActiveHeldUseAnimation = tree.GetString("lrHeldUseAnim");
		lastActiveHeldHitAnimation = tree.GetString("lrHeldHitAnim");
	}

	public override void ToAttributes(ITreeAttribute tree, bool forClient)
	{
		base.ToAttributes(tree, forClient);
		if (lastActiveHeldUseAnimation != null)
		{
			tree.SetString("lrHeldUseAnim", lastActiveHeldUseAnimation);
		}
		if (lastActiveHeldHitAnimation != null)
		{
			tree.SetString("lrHeldHitAnim", lastActiveHeldHitAnimation);
		}
		if (lastActiveRightHeldIdleAnimation != null)
		{
			tree.SetString("lrRightHeldIdleAnim", lastActiveRightHeldIdleAnimation);
		}
	}

	public void OnIfpModeChanged(bool prev, bool now)
	{
		if (prev == now)
		{
			return;
		}
		string[] array = ActiveAnimationsByAnimCode.Keys.ToArray();
		string stopVariant = (now ? "-fp" : "-ifp");
		string[] array2 = array;
		foreach (string animcode in array2)
		{
			if (animcode.EndsWith(stopVariant))
			{
				StopAnimation(animcode);
			}
		}
	}
}
