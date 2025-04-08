using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BEBehaviorControlPointAnimatable : BEBehaviorAnimatable
{
	protected ModSystemControlPoints modSys;

	protected float moveSpeedMul;

	protected ILoadedSound activeSound;

	protected bool active;

	protected ControlPoint animControlPoint;

	protected virtual Shape AnimationShape => null;

	public BEBehaviorControlPointAnimatable(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		AssetLocation controlpointcode = AssetLocation.Create(properties["controlpointcode"].ToString(), base.Block.Code.Domain);
		moveSpeedMul = properties["animSpeedMul"].AsFloat(1f);
		string soundloc = properties["activeSound"].AsString();
		if (soundloc != null && api is ICoreClientAPI capi)
		{
			AssetLocation loc = AssetLocation.Create(soundloc, base.Block.Code.Domain).WithPathPrefixOnce("sounds/");
			activeSound = capi.World.LoadSound(new SoundParams
			{
				Location = loc,
				DisposeOnFinish = false,
				ShouldLoop = true,
				SoundType = EnumSoundType.Ambient,
				Volume = 0.25f,
				Range = 16f,
				RelativePosition = false,
				Position = base.Pos.ToVec3f().Add(0.5f, 0.5f, 0.5f)
			});
		}
		modSys = api.ModLoader.GetModSystem<ModSystemControlPoints>();
		animControlPoint = modSys[controlpointcode];
		animControlPoint.Activate += BEBehaviorControlPointAnimatable_Activate;
		if (api.Side == EnumAppSide.Client)
		{
			animUtil.InitializeAnimator(base.Block.Code.ToShortString(), AnimationShape, null, new Vec3f(0f, base.Block.Shape.rotateY, 0f));
			BEBehaviorControlPointAnimatable_Activate(animControlPoint);
		}
	}

	protected virtual void BEBehaviorControlPointAnimatable_Activate(ControlPoint cpoint)
	{
		updateAnimationstate();
	}

	protected void updateAnimationstate()
	{
		if (animControlPoint == null)
		{
			return;
		}
		active = false;
		AnimationMetaData animData = animControlPoint.ControlData as AnimationMetaData;
		if (animData == null)
		{
			return;
		}
		if (animData.AnimationSpeed == 0f)
		{
			activeSound?.FadeOutAndStop(2f);
			animUtil.StopAnimation(animData.Animation);
			animUtil.StopAnimation(animData.Animation + "-inverse");
			return;
		}
		active = true;
		if (moveSpeedMul != 1f)
		{
			animData = animData.Clone();
			if (moveSpeedMul < 0f)
			{
				animData.Animation += "-inverse";
				animData.Code += "-inverse";
			}
			animData.AnimationSpeed *= Math.Abs(moveSpeedMul);
		}
		if (!animUtil.StartAnimation(animData))
		{
			animUtil.activeAnimationsByAnimCode[animData.Animation].AnimationSpeed = animData.AnimationSpeed;
		}
		else
		{
			activeSound?.Start();
			activeSound?.FadeIn(2f, null);
		}
		Blockentity.MarkDirty(redrawOnClient: true);
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		activeSound?.Stop();
		activeSound?.Dispose();
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		base.GetBlockInfo(forPlayer, dsc);
	}
}
