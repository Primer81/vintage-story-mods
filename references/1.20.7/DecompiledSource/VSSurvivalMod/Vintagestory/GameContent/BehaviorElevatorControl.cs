using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BehaviorElevatorControl : BlockBehavior
{
	private ICoreAPI api;

	private SimpleParticleProperties particleProperties;

	public BehaviorElevatorControl(Block block)
		: base(block)
	{
	}

	public override void OnLoaded(ICoreAPI api)
	{
		if (api.Side == EnumAppSide.Client)
		{
			this.api = api;
			particleProperties = new SimpleParticleProperties
			{
				MinQuantity = 3f,
				OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, -75f),
				ParticleModel = EnumParticleModel.Quad,
				GravityEffect = 0f,
				LifeLength = 6f,
				MinSize = 0.125f,
				MaxSize = 0.125f,
				MinVelocity = new Vec3f(-0.0625f, 1f / 32f, -0.0625f),
				AddVelocity = new Vec3f(0.125f, 0.0625f, 0.125f),
				Color = ColorUtil.ColorFromRgba(200, 250, 250, 75)
			};
		}
	}

	public override bool ShouldReceiveClientParticleTicks(IWorldAccessor world, IPlayer byPlayer, BlockPos pos, ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		return true;
	}

	public override void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
	{
		if (api.World.Rand.NextDouble() < 0.05)
		{
			particleProperties.MinPos = pos.ToVec3d().AddCopy(0.5, 0.10000000149011612, 0.5);
			manager.Spawn(particleProperties);
		}
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
	{
		handling = EnumHandling.PreventSubsequent;
		if (byPlayer.Entity.Api.Side == EnumAppSide.Server)
		{
			BEBehaviorElevatorControl beBehaviorElevatorControl = block.GetBEBehavior<BEBehaviorElevatorControl>(blockSel.Position);
			if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative && byPlayer.Entity.Controls.Sneak)
			{
				beBehaviorElevatorControl.OnInteract(blockSel.Position, deactivate: true);
			}
			else
			{
				beBehaviorElevatorControl.OnInteract(blockSel.Position);
			}
		}
		return true;
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, ref EnumHandling handling)
	{
		BEBehaviorElevatorControl bEBehavior = block.GetBEBehavior<BEBehaviorElevatorControl>(selection.Position);
		if (bEBehavior == null || !bEBehavior.EnableElevator)
		{
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "elevator-call",
					MouseButton = EnumMouseButton.Right
				}
			};
		}
		bool num = forPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative;
		List<WorldInteraction> interactions = new List<WorldInteraction>();
		if (num)
		{
			interactions.Add(new WorldInteraction
			{
				ActionLangCode = "elevator-disable",
				MouseButton = EnumMouseButton.Right,
				HotKeyCode = "sneak"
			});
		}
		interactions.Add(new WorldInteraction
		{
			ActionLangCode = "elevator-enable",
			MouseButton = EnumMouseButton.Right
		});
		return interactions.ToArray();
	}
}
