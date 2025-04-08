using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class NpcPlayAnimationCommand : INpcCommand
{
	public string AnimCode;

	public float AnimSpeed;

	private EntityAnimalBot entity;

	public string Type => "anim";

	public NpcPlayAnimationCommand(EntityAnimalBot entity, string animCode, float animSpeed)
	{
		this.entity = entity;
		AnimCode = animCode;
		AnimSpeed = animSpeed;
	}

	public void Start()
	{
		if (AnimSpeed != 1f || !entity.AnimManager.StartAnimation(AnimCode))
		{
			entity.AnimManager.StartAnimation(new AnimationMetaData
			{
				Animation = AnimCode,
				Code = AnimCode,
				AnimationSpeed = AnimSpeed
			}.Init());
		}
	}

	public void Stop()
	{
		entity.Properties.Client.AnimationsByMetaCode.TryGetValue(AnimCode, out var animData);
		if (animData != null && animData.Code != null)
		{
			entity.AnimManager.StopAnimation(animData.Code);
		}
		else
		{
			entity.AnimManager.StopAnimation(AnimCode);
		}
	}

	public bool IsFinished()
	{
		entity.Properties.Client.AnimationsByMetaCode.TryGetValue(AnimCode, out var animData);
		if (!entity.AnimManager.ActiveAnimationsByAnimCode.ContainsKey(AnimCode))
		{
			if (animData != null && animData.Animation != null)
			{
				return !entity.AnimManager.ActiveAnimationsByAnimCode.ContainsKey(animData?.Animation);
			}
			return true;
		}
		return false;
	}

	public override string ToString()
	{
		return "Play animation " + AnimCode;
	}

	public void ToAttribute(ITreeAttribute tree)
	{
		tree.SetString("animCode", AnimCode);
		tree.SetFloat("animSpeed", AnimSpeed);
	}

	public void FromAttribute(ITreeAttribute tree)
	{
		AnimCode = tree.GetString("animCode");
		AnimSpeed = tree.GetFloat("animSpeed", 1f);
	}
}
