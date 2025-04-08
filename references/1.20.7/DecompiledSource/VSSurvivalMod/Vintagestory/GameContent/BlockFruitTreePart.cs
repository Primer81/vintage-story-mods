using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockFruitTreePart : Block
{
	private SimpleParticleProperties foliageParticles;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		foliageParticles = new SimpleParticleProperties
		{
			MinQuantity = 1f,
			AddQuantity = 0f,
			MinPos = new Vec3d(),
			AddPos = new Vec3d(1.0, 1.0, 1.0),
			LifeLength = 2f,
			GravityEffect = 0.005f,
			MinSize = 0.1f,
			MaxSize = 0.2f,
			ParticleModel = EnumParticleModel.Quad,
			WindAffectednes = 2f,
			ShouldSwimOnLiquid = true
		};
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityFruitTreeBranch bebranch && (bebranch.RootOff?.Equals(Vec3i.Zero)).GetValueOrDefault() && byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative)
		{
			if (world.Side == EnumAppSide.Server)
			{
				bebranch?.InteractDebug();
			}
			return true;
		}
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityFruitTreePart be)
		{
			return be.OnBlockInteractStart(byPlayer, blockSel);
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityFruitTreePart be)
		{
			return be.OnBlockInteractStep(secondsUsed, byPlayer, blockSel);
		}
		return base.OnBlockInteractStep(secondsUsed, world, byPlayer, blockSel);
	}

	public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityFruitTreePart be)
		{
			be.OnBlockInteractStop(secondsUsed, byPlayer, blockSel);
		}
		else
		{
			base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel);
		}
	}

	public override void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
	{
		if (api.World.Rand.NextDouble() < 0.98 - (double)(GlobalConstants.CurrentWindSpeedClient.X / 10f))
		{
			return;
		}
		BlockEntityFruitTreePart be = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityFruitTreePart;
		if (be?.LeafParticlesColor != null)
		{
			if (be.FruitTreeState == EnumFruitTreeState.EnterDormancy)
			{
				foliageParticles.Color = be.LeafParticlesColor[api.World.Rand.Next(25)];
				foliageParticles.Color = (api as ICoreClientAPI).World.ApplyColorMapOnRgba("climatePlantTint", SeasonColorMap, foliageParticles.Color, pos.X, pos.Y, pos.Z);
				foliageParticles.GravityEffect = 0.02f + 0.005f * GlobalConstants.CurrentWindSpeedClient.X;
				foliageParticles.MinSize = 0.4f;
			}
			else
			{
				foliageParticles.Color = be.BlossomParticlesColor[api.World.Rand.Next(25)];
				foliageParticles.MinSize = 0.1f;
				foliageParticles.GravityEffect = 0.005f + 0.005f * GlobalConstants.CurrentWindSpeedClient.X;
			}
			foliageParticles.LifeLength = 7f - GlobalConstants.CurrentWindSpeedClient.X * 3f;
			foliageParticles.WindAffectednes = 1f;
			foliageParticles.MinPos.Set(pos);
			manager.Spawn(foliageParticles);
		}
	}

	public override bool ShouldReceiveClientParticleTicks(IWorldAccessor world, IPlayer player, BlockPos pos, out bool isWindAffected)
	{
		BlockEntityFruitTreePart be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityFruitTreePart;
		isWindAffected = true;
		Dictionary<string, FruitTreeTypeProperties> typeProps = be?.blockBranch?.TypeProps;
		if (be != null && be.TreeType != null && typeProps != null && typeProps.ContainsKey(be.TreeType))
		{
			if (be.fruitingSide <= 0 || be.LeafParticlesColor == null || be.FruitTreeState != EnumFruitTreeState.EnterDormancy)
			{
				if (be.FruitTreeState == EnumFruitTreeState.Flowering && be.Progress > 0.5)
				{
					return typeProps[be.TreeType].BlossomParticles;
				}
				return false;
			}
			return true;
		}
		return false;
	}
}
