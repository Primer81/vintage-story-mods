using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockLava : BlockForFluidsLayer, IBlockFlowing
{
	private class FireLocation
	{
		public readonly BlockPos firePos;

		public readonly BlockFacing facing;

		public FireLocation(BlockPos firePos, BlockFacing facing)
		{
			this.firePos = firePos;
			this.facing = facing;
		}
	}

	private readonly int temperature = 1200;

	private readonly int tempLossPerMeter = 100;

	private Block blockFire;

	private AdvancedParticleProperties[] fireParticles;

	public string Flow { get; set; }

	public Vec3i FlowNormali { get; set; }

	public bool IsLava => true;

	public int Height { get; set; }

	public BlockLava()
	{
		if (Attributes != null)
		{
			temperature = Attributes["temperature"].AsInt(1200);
			tempLossPerMeter = Attributes["tempLossPerMeter"].AsInt(100);
		}
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		string f = Variant["flow"];
		Flow = ((f != null) ? string.Intern(f) : null);
		FlowNormali = ((Flow == null) ? null : Cardinal.FromInitial(Flow)?.Normali);
		Height = Variant["height"]?.ToInt() ?? 7;
		if (blockFire == null)
		{
			blockFire = api.World.GetBlock(new AssetLocation("fire"));
			fireParticles = new AdvancedParticleProperties[blockFire.ParticleProperties.Length];
			for (int i = 0; i < fireParticles.Length; i++)
			{
				fireParticles[i] = blockFire.ParticleProperties[i].Clone();
			}
			fireParticles[2].HsvaColor[2].avg += 60f;
			fireParticles[2].LifeLength.avg += 3f;
		}
	}

	public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
	{
		base.OnServerGameTick(world, pos, extra);
		FireLocation fireLocation = (FireLocation)extra;
		world.BlockAccessor.SetBlock(blockFire.BlockId, fireLocation.firePos);
		world.BlockAccessor.GetBlockEntity(fireLocation.firePos)?.GetBehavior<BEBehaviorBurning>()?.OnFirePlaced(fireLocation.facing, null);
	}

	private FireLocation FindFireLocation(IWorldAccessor world, BlockPos lavaPos)
	{
		Random rnd = world.Rand;
		int tries = 20;
		if (world.BlockAccessor.GetBlockAbove(lavaPos).Id == 0)
		{
			tries = 40;
		}
		BlockPos pos = new BlockPos(lavaPos.dimension);
		for (int i = 0; i < tries; i++)
		{
			pos.Set(lavaPos);
			pos.Add(rnd.Next(7) - 3, rnd.Next(4), rnd.Next(7) - 3);
			if (world.BlockAccessor.GetBlock(pos).Id == 0)
			{
				BlockFacing facing = IsNextToCombustibleBlock(world, lavaPos, pos);
				if (facing != null)
				{
					return new FireLocation(pos, facing);
				}
			}
		}
		return null;
	}

	private BlockFacing IsNextToCombustibleBlock(IWorldAccessor world, BlockPos lavaPos, BlockPos airBlockPos)
	{
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing facing in aLLFACES)
		{
			BlockPos npos = airBlockPos.AddCopy(facing);
			Block block = world.BlockAccessor.GetBlock(npos);
			if (block.CombustibleProps != null && block.CombustibleProps.BurnTemperature <= GetTemperatureAtLocation(lavaPos, airBlockPos))
			{
				return facing;
			}
		}
		return null;
	}

	private int GetTemperatureAtLocation(BlockPos lavaPos, BlockPos airBlockPos)
	{
		int distance = lavaPos.ManhattenDistance(airBlockPos);
		return temperature - distance * tempLossPerMeter;
	}

	public override bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object extra)
	{
		if (LiquidLevel == 7)
		{
			FireLocation fireLocation = FindFireLocation(world, pos);
			if (fireLocation != null)
			{
				extra = fireLocation;
				return true;
			}
		}
		extra = null;
		return false;
	}

	public override float GetTraversalCost(BlockPos pos, EnumAICreatureType creatureType)
	{
		return 99999f;
	}

	public override bool ShouldReceiveClientParticleTicks(IWorldAccessor world, IPlayer player, BlockPos pos, out bool isWindAffected)
	{
		isWindAffected = false;
		pos.Up();
		Block blockAbove = world.BlockAccessor.GetBlock(pos);
		pos.Down();
		if (!blockAbove.IsLiquid())
		{
			if (blockAbove.CollisionBoxes != null)
			{
				return blockAbove.CollisionBoxes.Length == 0;
			}
			return true;
		}
		return false;
	}

	public override void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
	{
		if (GameMath.MurmurHash3Mod(pos.X, pos.Y, pos.Z, 100) < 2)
		{
			for (int i = 0; i < fireParticles.Length; i++)
			{
				AdvancedParticleProperties bps = fireParticles[i];
				bps.Quantity.avg = (float)i * 0.3f;
				bps.WindAffectednesAtPos = windAffectednessAtPos;
				bps.basePos.X = (float)pos.X + TopMiddlePos.X;
				bps.basePos.Y = (float)pos.Y + TopMiddlePos.Y;
				bps.basePos.Z = (float)pos.Z + TopMiddlePos.Z;
				manager.Spawn(bps);
			}
		}
		base.OnAsyncClientParticleTick(manager, pos, windAffectednessAtPos, secondsTicking);
	}
}
