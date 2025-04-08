using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockBehaviorFiniteSpreadingLiquid : BlockBehavior
{
	private const int MAXLEVEL = 7;

	private const float MAXLEVEL_float = 7f;

	public static Vec2i[] downPaths = ShapeUtil.GetSquarePointsSortedByMDist(3);

	public static SimpleParticleProperties steamParticles;

	public static int ReplacableThreshold = 5000;

	private AssetLocation collisionReplaceSound;

	private int spreadDelay = 150;

	private string collidesWith;

	private AssetLocation liquidSourceCollisionReplacement;

	private AssetLocation liquidFlowingCollisionReplacement;

	public BlockBehaviorFiniteSpreadingLiquid(Block block)
		: base(block)
	{
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);
		spreadDelay = properties["spreadDelay"].AsInt();
		collisionReplaceSound = CreateAssetLocation(properties, "sounds/", "liquidCollisionSound");
		liquidSourceCollisionReplacement = CreateAssetLocation(properties, "sourceReplacementCode");
		liquidFlowingCollisionReplacement = CreateAssetLocation(properties, "flowingReplacementCode");
		collidesWith = properties["collidesWith"]?.AsString();
	}

	public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack, ref EnumHandling handling)
	{
		if (world is IServerWorldAccessor)
		{
			world.RegisterCallbackUnique(OnDelayedWaterUpdateCheck, blockSel.Position, spreadDelay);
		}
		return base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack, ref handling);
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		if (world is IServerWorldAccessor)
		{
			world.RegisterCallbackUnique(OnDelayedWaterUpdateCheck, pos, spreadDelay);
		}
	}

	private void OnDelayedWaterUpdateCheck(IWorldAccessor world, BlockPos pos, float dt)
	{
		SpreadAndUpdateLiquidLevels(world, pos);
		world.BulkBlockAccessor.Commit();
		Block block = world.BlockAccessor.GetBlock(pos, 2);
		if (block.HasBehavior<BlockBehaviorFiniteSpreadingLiquid>())
		{
			updateOwnFlowDir(block, world, pos);
		}
		BlockPos npos = pos.Copy();
		Cardinal[] aLL = Cardinal.ALL;
		foreach (Cardinal val in aLL)
		{
			npos.Set(pos.X + val.Normali.X, pos.Y, pos.Z + val.Normali.Z);
			Block neib = world.BlockAccessor.GetBlock(npos, 2);
			if (neib.HasBehavior<BlockBehaviorFiniteSpreadingLiquid>())
			{
				updateOwnFlowDir(neib, world, npos);
			}
		}
	}

	private void SpreadAndUpdateLiquidLevels(IWorldAccessor world, BlockPos pos)
	{
		Block ourBlock = world.BlockAccessor.GetBlock(pos, 2);
		int liquidLevel = ourBlock.LiquidLevel;
		if (liquidLevel <= 0 || TryLoweringLiquidLevel(ourBlock, world, pos))
		{
			return;
		}
		Block mostSolidBlock = world.BlockAccessor.GetMostSolidBlock(pos.DownCopy());
		Block ourSolid = world.BlockAccessor.GetBlock(pos, 1);
		if (((double)mostSolidBlock.GetLiquidBarrierHeightOnSide(BlockFacing.UP, pos.DownCopy()) != 1.0 && (double)ourSolid.GetLiquidBarrierHeightOnSide(BlockFacing.DOWN, pos) != 1.0 && TrySpreadDownwards(world, ourSolid, ourBlock, pos)) || liquidLevel <= 1)
		{
			return;
		}
		List<PosAndDist> downwardPaths = FindDownwardPaths(world, pos, ourBlock);
		if (downwardPaths.Count > 0)
		{
			FlowTowardDownwardPaths(downwardPaths, ourBlock, ourSolid, pos, world);
			return;
		}
		TrySpreadHorizontal(ourBlock, ourSolid, world, pos);
		if (IsLiquidSourceBlock(ourBlock))
		{
			return;
		}
		int nearbySourceBlockCount = CountNearbySourceBlocks(world.BlockAccessor, pos, ourBlock);
		if (nearbySourceBlockCount < 3 && (nearbySourceBlockCount != 2 || CountNearbyDiagonalSources(world.BlockAccessor, pos, ourBlock) < 3))
		{
			return;
		}
		world.BlockAccessor.SetBlock(GetMoreLiquidBlockId(world, pos, ourBlock), pos, 2);
		BlockPos npos = pos.Copy();
		for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++)
		{
			BlockFacing.HORIZONTALS[i].IterateThruFacingOffsets(npos);
			Block nblock = world.BlockAccessor.GetBlock(npos, 2);
			if (nblock.HasBehavior<BlockBehaviorFiniteSpreadingLiquid>())
			{
				updateOwnFlowDir(nblock, world, npos);
			}
		}
	}

	private int CountNearbySourceBlocks(IBlockAccessor blockAccessor, BlockPos pos, Block ourBlock)
	{
		BlockPos qpos = pos.Copy();
		int nearbySourceBlockCount = 0;
		for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++)
		{
			BlockFacing.HORIZONTALS[i].IterateThruFacingOffsets(qpos);
			Block nblock = blockAccessor.GetBlock(qpos, 2);
			if (IsSameLiquid(ourBlock, nblock) && IsLiquidSourceBlock(nblock))
			{
				nearbySourceBlockCount++;
			}
		}
		return nearbySourceBlockCount;
	}

	private int CountNearbyDiagonalSources(IBlockAccessor blockAccessor, BlockPos pos, Block ourBlock)
	{
		BlockPos npos = pos.Copy();
		int nearbySourceBlockCount = 0;
		Cardinal[] aLL = Cardinal.ALL;
		foreach (Cardinal val in aLL)
		{
			if (val.IsDiagnoal)
			{
				npos.Set(pos.X + val.Normali.X, pos.Y, pos.Z + val.Normali.Z);
				Block nblock = blockAccessor.GetBlock(npos, 2);
				if (IsSameLiquid(ourBlock, nblock) && IsLiquidSourceBlock(nblock))
				{
					nearbySourceBlockCount++;
				}
			}
		}
		return nearbySourceBlockCount;
	}

	private void FlowTowardDownwardPaths(List<PosAndDist> downwardPaths, Block liquidBlock, Block solidBlock, BlockPos pos, IWorldAccessor world)
	{
		foreach (PosAndDist pod in downwardPaths)
		{
			if (CanSpreadIntoBlock(liquidBlock, solidBlock, pos, pod.pos, pod.pos.FacingFrom(pos), world))
			{
				Block neighborLiquid = world.BlockAccessor.GetBlock(pod.pos, 2);
				if (IsDifferentCollidableLiquid(liquidBlock, neighborLiquid))
				{
					ReplaceLiquidBlock(neighborLiquid, pod.pos, world);
				}
				else
				{
					SpreadLiquid(GetLessLiquidBlockId(world, pod.pos, liquidBlock), pod.pos, world);
				}
			}
		}
	}

	private bool TrySpreadDownwards(IWorldAccessor world, Block ourSolid, Block ourBlock, BlockPos pos)
	{
		BlockPos npos = pos.DownCopy();
		Block belowLiquid = world.BlockAccessor.GetBlock(npos, 2);
		if (CanSpreadIntoBlock(ourBlock, ourSolid, pos, npos, BlockFacing.DOWN, world))
		{
			if (IsDifferentCollidableLiquid(ourBlock, belowLiquid))
			{
				ReplaceLiquidBlock(belowLiquid, npos, world);
				TryFindSourceAndSpread(npos, world);
			}
			else
			{
				bool fillWithSource = false;
				if (IsLiquidSourceBlock(ourBlock))
				{
					if (CountNearbySourceBlocks(world.BlockAccessor, npos, ourBlock) > 1)
					{
						fillWithSource = true;
					}
					else
					{
						npos.Down();
						if ((double)world.BlockAccessor.GetBlock(npos, 4).GetLiquidBarrierHeightOnSide(BlockFacing.UP, npos) == 1.0 || (double)ourSolid.GetLiquidBarrierHeightOnSide(BlockFacing.DOWN, pos) == 1.0 || IsLiquidSourceBlock(world.BlockAccessor.GetBlock(npos, 2)))
						{
							fillWithSource = CountNearbySourceBlocks(world.BlockAccessor, pos, ourBlock) >= 2;
						}
						npos.Up();
					}
				}
				SpreadLiquid(fillWithSource ? ourBlock.BlockId : GetFallingLiquidBlockId(ourBlock, world), npos, world);
			}
			return true;
		}
		if (IsLiquidSourceBlock(ourBlock))
		{
			return !IsLiquidSourceBlock(belowLiquid);
		}
		return true;
	}

	private void TrySpreadHorizontal(Block ourblock, Block ourSolid, IWorldAccessor world, BlockPos pos)
	{
		BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
		foreach (BlockFacing facing in hORIZONTALS)
		{
			TrySpreadIntoBlock(ourblock, ourSolid, pos, pos.AddCopy(facing), facing, world);
		}
	}

	private void ReplaceLiquidBlock(Block liquidBlock, BlockPos pos, IWorldAccessor world)
	{
		Block replacementBlock = GetReplacementBlock(liquidBlock, world);
		if (replacementBlock != null)
		{
			world.BulkBlockAccessor.SetBlock(replacementBlock.BlockId, pos);
			BlockBehaviorBreakIfFloating bh = replacementBlock.GetBehavior<BlockBehaviorBreakIfFloating>();
			if (bh != null && bh.IsSurroundedByNonSolid(world, pos))
			{
				world.BulkBlockAccessor.SetBlock(replacementBlock.BlockId, pos.DownCopy());
			}
			UpdateNeighbouringLiquids(pos, world);
			GenerateSteamParticles(pos, world);
			world.PlaySoundAt(collisionReplaceSound, pos, 0.0, null, randomizePitch: true, 16f);
		}
	}

	private void SpreadLiquid(int blockId, BlockPos pos, IWorldAccessor world)
	{
		world.BulkBlockAccessor.SetBlock(blockId, pos, 2);
		world.RegisterCallbackUnique(OnDelayedWaterUpdateCheck, pos, spreadDelay);
		Block ourBlock = world.GetBlock(blockId);
		TryReplaceNearbyLiquidBlocks(ourBlock, pos, world);
	}

	private void updateOwnFlowDir(Block block, IWorldAccessor world, BlockPos pos)
	{
		int blockId = GetLiquidBlockId(world, pos, block, block.LiquidLevel);
		if (block.BlockId != blockId)
		{
			world.BlockAccessor.SetBlock(blockId, pos, 2);
		}
	}

	private void TryReplaceNearbyLiquidBlocks(Block ourBlock, BlockPos pos, IWorldAccessor world)
	{
		BlockPos npos = pos.Copy();
		BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
		for (int i = 0; i < hORIZONTALS.Length; i++)
		{
			hORIZONTALS[i].IterateThruFacingOffsets(npos);
			Block neib = world.BlockAccessor.GetBlock(npos, 2);
			if (IsDifferentCollidableLiquid(ourBlock, neib))
			{
				ReplaceLiquidBlock(ourBlock, npos, world);
			}
		}
	}

	private bool TryFindSourceAndSpread(BlockPos startingPos, IWorldAccessor world)
	{
		BlockPos sourceBlockPos = startingPos.UpCopy();
		Block sourceBlock = world.BlockAccessor.GetBlock(sourceBlockPos, 2);
		while (sourceBlock.IsLiquid())
		{
			if (IsLiquidSourceBlock(sourceBlock))
			{
				Block ourSolid = world.BlockAccessor.GetBlock(sourceBlockPos, 1);
				TrySpreadHorizontal(sourceBlock, ourSolid, world, sourceBlockPos);
				return true;
			}
			sourceBlockPos.Add(0, 1, 0);
			sourceBlock = world.BlockAccessor.GetBlock(sourceBlockPos, 2);
		}
		return false;
	}

	private void GenerateSteamParticles(BlockPos pos, IWorldAccessor world)
	{
		float maxQuantity = 100f;
		int color = ColorUtil.ToRgba(100, 225, 225, 225);
		Vec3d minPos = new Vec3d();
		Vec3d addPos = new Vec3d();
		Vec3f minVelocity = new Vec3f(-0.25f, 0.1f, -0.25f);
		Vec3f maxVelocity = new Vec3f(0.25f, 0.1f, 0.25f);
		float lifeLength = 2f;
		float gravityEffect = -0.015f;
		float minSize = 0.1f;
		float maxSize = 0.1f;
		SimpleParticleProperties steamParticles = new SimpleParticleProperties(50f, maxQuantity, color, minPos, addPos, minVelocity, maxVelocity, lifeLength, gravityEffect, minSize, maxSize, EnumParticleModel.Quad);
		steamParticles.Async = true;
		steamParticles.MinPos.Set(pos.ToVec3d().AddCopy(0.5, 1.1, 0.5));
		steamParticles.AddPos.Set(new Vec3d(0.5, 1.0, 0.5));
		steamParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARINCREASE, 1f);
		world.SpawnParticles(steamParticles);
	}

	private void UpdateNeighbouringLiquids(BlockPos pos, IWorldAccessor world)
	{
		BlockPos npos = pos.DownCopy();
		if (world.BlockAccessor.GetBlock(npos, 2).HasBehavior<BlockBehaviorFiniteSpreadingLiquid>())
		{
			world.RegisterCallbackUnique(OnDelayedWaterUpdateCheck, npos.Copy(), spreadDelay);
		}
		npos.Up(2);
		if (world.BlockAccessor.GetBlock(npos, 2).HasBehavior<BlockBehaviorFiniteSpreadingLiquid>())
		{
			world.RegisterCallbackUnique(OnDelayedWaterUpdateCheck, npos.Copy(), spreadDelay);
		}
		npos.Down();
		Cardinal[] aLL = Cardinal.ALL;
		foreach (Cardinal val in aLL)
		{
			npos.Set(pos.X + val.Normali.X, pos.Y, pos.Z + val.Normali.Z);
			if (world.BlockAccessor.GetBlock(npos, 2).HasBehavior<BlockBehaviorFiniteSpreadingLiquid>())
			{
				world.RegisterCallbackUnique(OnDelayedWaterUpdateCheck, npos.Copy(), spreadDelay);
			}
		}
	}

	private Block GetReplacementBlock(Block neighborBlock, IWorldAccessor world)
	{
		AssetLocation replacementLocation = liquidFlowingCollisionReplacement;
		if (IsLiquidSourceBlock(neighborBlock))
		{
			replacementLocation = liquidSourceCollisionReplacement;
		}
		if (!(replacementLocation == null))
		{
			return world.GetBlock(replacementLocation);
		}
		return null;
	}

	private bool IsDifferentCollidableLiquid(Block block, Block other)
	{
		if (other.IsLiquid() && block.IsLiquid())
		{
			return other.LiquidCode == collidesWith;
		}
		return false;
	}

	private bool IsSameLiquid(Block block, Block other)
	{
		return block.LiquidCode == other.LiquidCode;
	}

	private bool IsLiquidSourceBlock(Block block)
	{
		return block.LiquidLevel == 7;
	}

	private bool TryLoweringLiquidLevel(Block ourBlock, IWorldAccessor world, BlockPos pos)
	{
		if (!IsLiquidSourceBlock(ourBlock) && GetMaxNeighbourLiquidLevel(ourBlock, world, pos) <= ourBlock.LiquidLevel)
		{
			LowerLiquidLevelAndNotifyNeighbors(ourBlock, pos, world);
			return true;
		}
		return false;
	}

	private void LowerLiquidLevelAndNotifyNeighbors(Block block, BlockPos pos, IWorldAccessor world)
	{
		SpreadLiquid(GetLessLiquidBlockId(world, pos, block), pos, world);
		BlockPos npos = pos.Copy();
		for (int i = 0; i < 6; i++)
		{
			BlockFacing.ALLFACES[i].IterateThruFacingOffsets(npos);
			Block liquidBlock = world.BlockAccessor.GetBlock(npos, 2);
			if (liquidBlock.BlockId != 0)
			{
				liquidBlock.OnNeighbourBlockChange(world, npos, pos);
			}
		}
	}

	private void TrySpreadIntoBlock(Block ourblock, Block ourSolid, BlockPos pos, BlockPos npos, BlockFacing facing, IWorldAccessor world)
	{
		if (CanSpreadIntoBlock(ourblock, ourSolid, pos, npos, facing, world))
		{
			Block neighborLiquid = world.BlockAccessor.GetBlock(npos, 2);
			if (IsDifferentCollidableLiquid(ourblock, neighborLiquid))
			{
				ReplaceLiquidBlock(neighborLiquid, npos, world);
			}
			else
			{
				SpreadLiquid(GetLessLiquidBlockId(world, npos, ourblock), npos, world);
			}
		}
	}

	public int GetLessLiquidBlockId(IWorldAccessor world, BlockPos pos, Block block)
	{
		return GetLiquidBlockId(world, pos, block, block.LiquidLevel - 1);
	}

	public int GetMoreLiquidBlockId(IWorldAccessor world, BlockPos pos, Block block)
	{
		return GetLiquidBlockId(world, pos, block, Math.Min(7, block.LiquidLevel + 1));
	}

	public int GetLiquidBlockId(IWorldAccessor world, BlockPos pos, Block block, int liquidLevel)
	{
		if (liquidLevel < 1)
		{
			return 0;
		}
		Vec3i dir = new Vec3i();
		bool anySideFree = false;
		BlockPos npos = pos.Copy();
		IBlockAccessor blockAccessor = world.BlockAccessor;
		Cardinal[] aLL = Cardinal.ALL;
		foreach (Cardinal val in aLL)
		{
			npos.Set(pos.X + val.Normali.X, pos.Y, pos.Z + val.Normali.Z);
			Block nblock = blockAccessor.GetBlock(npos, 2);
			if (nblock.LiquidLevel != liquidLevel && nblock.Replaceable >= 6000 && nblock.IsLiquid())
			{
				Vec3i normal = ((nblock.LiquidLevel < liquidLevel) ? val.Normali : val.Opposite.Normali);
				if (!val.IsDiagnoal)
				{
					nblock = blockAccessor.GetBlock(npos, 1);
					anySideFree |= (double)nblock.GetLiquidBarrierHeightOnSide(BlockFacing.ALLFACES[val.Opposite.Index / 2], npos) != 1.0;
					nblock = blockAccessor.GetBlock(pos, 1);
					anySideFree |= (double)nblock.GetLiquidBarrierHeightOnSide(BlockFacing.ALLFACES[val.Index / 2], pos) != 1.0;
				}
				dir.X += normal.X;
				dir.Z += normal.Z;
			}
		}
		if (Math.Abs(dir.X) > Math.Abs(dir.Z))
		{
			dir.Z = 0;
		}
		else if (Math.Abs(dir.Z) > Math.Abs(dir.X))
		{
			dir.X = 0;
		}
		dir.X = Math.Sign(dir.X);
		dir.Z = Math.Sign(dir.Z);
		Cardinal flowDir = Cardinal.FromNormali(dir);
		if (flowDir == null)
		{
			Block downBlock = blockAccessor.GetBlock(pos.DownCopy(), 2);
			Block upBlock = blockAccessor.GetBlock(pos.UpCopy(), 2);
			bool num = IsSameLiquid(downBlock, block);
			bool upLiquid = IsSameLiquid(upBlock, block);
			if ((num && downBlock.Variant["flow"] == "d") || (upLiquid && upBlock.Variant["flow"] == "d"))
			{
				return world.GetBlock(block.CodeWithParts("d", liquidLevel.ToString() ?? "")).BlockId;
			}
			if (anySideFree)
			{
				return world.GetBlock(block.CodeWithParts("d", liquidLevel.ToString() ?? "")).BlockId;
			}
			return world.GetBlock(block.CodeWithParts("still", liquidLevel.ToString() ?? "")).BlockId;
		}
		return world.GetBlock(block.CodeWithParts(flowDir.Initial, liquidLevel.ToString() ?? "")).BlockId;
	}

	private int GetFallingLiquidBlockId(Block ourBlock, IWorldAccessor world)
	{
		return world.GetBlock(ourBlock.CodeWithParts("d", "6")).BlockId;
	}

	public int GetMaxNeighbourLiquidLevel(Block ourblock, IWorldAccessor world, BlockPos pos)
	{
		Block ourSolid = world.BlockAccessor.GetBlock(pos, 1);
		BlockPos npos = pos.UpCopy();
		Block ublock = world.BlockAccessor.GetBlock(npos, 2);
		Block uSolid = world.BlockAccessor.GetBlock(npos, 1);
		if (IsSameLiquid(ourblock, ublock) && (double)ourSolid.GetLiquidBarrierHeightOnSide(BlockFacing.UP, pos) == 0.0 && (double)uSolid.GetLiquidBarrierHeightOnSide(BlockFacing.DOWN, npos) == 0.0)
		{
			return 7;
		}
		int level = 0;
		npos.Down();
		for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++)
		{
			BlockFacing.HORIZONTALS[i].IterateThruFacingOffsets(npos);
			Block nblock = world.BlockAccessor.GetBlock(npos, 2);
			if (IsSameLiquid(ourblock, nblock))
			{
				int nLevel = nblock.LiquidLevel;
				if (!(ourSolid.GetLiquidBarrierHeightOnSide(BlockFacing.HORIZONTALS[i], pos) >= (float)nLevel / 7f) && !(world.BlockAccessor.GetBlock(npos, 1).GetLiquidBarrierHeightOnSide(BlockFacing.HORIZONTALS[i].Opposite, npos) >= (float)nLevel / 7f))
				{
					level = Math.Max(level, nLevel);
				}
			}
		}
		return level;
	}

	[Obsolete("Instead Use CanSpreadIntoBlock(Block, BlockPos, IWorldAccessor) to read from the liquid layer correctly, as well as the block layer")]
	public bool CanSpreadIntoBlock(Block ourblock, Block neighborBlock, IWorldAccessor world)
	{
		if (!IsSameLiquid(ourblock, neighborBlock) || neighborBlock.LiquidLevel >= ourblock.LiquidLevel)
		{
			if (!IsSameLiquid(ourblock, neighborBlock))
			{
				return neighborBlock.Replaceable >= ReplacableThreshold;
			}
			return false;
		}
		return true;
	}

	public bool CanSpreadIntoBlock(Block ourblock, Block ourSolid, BlockPos pos, BlockPos npos, BlockFacing facing, IWorldAccessor world)
	{
		if (ourSolid.GetLiquidBarrierHeightOnSide(facing, pos) >= (float)ourblock.LiquidLevel / 7f)
		{
			return false;
		}
		if (world.BlockAccessor.GetBlock(npos, 1).GetLiquidBarrierHeightOnSide(facing.Opposite, npos) >= (float)ourblock.LiquidLevel / 7f)
		{
			return false;
		}
		Block neighborLiquid = world.BlockAccessor.GetBlock(npos, 2);
		if (IsSameLiquid(ourblock, neighborLiquid))
		{
			return neighborLiquid.LiquidLevel < ourblock.LiquidLevel;
		}
		if (neighborLiquid.LiquidLevel == 7 && !IsDifferentCollidableLiquid(ourblock, neighborLiquid))
		{
			return false;
		}
		if (neighborLiquid.BlockId != 0)
		{
			return neighborLiquid.Replaceable >= ourblock.Replaceable;
		}
		if (ourblock.LiquidLevel <= 1)
		{
			return facing == BlockFacing.DOWN;
		}
		return true;
	}

	public override bool IsReplacableBy(Block byBlock, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		if (block.IsLiquid() || block.Replaceable >= ReplacableThreshold)
		{
			return byBlock.Replaceable <= block.Replaceable;
		}
		return false;
	}

	public List<PosAndDist> FindDownwardPaths(IWorldAccessor world, BlockPos pos, Block ourBlock)
	{
		List<PosAndDist> paths = new List<PosAndDist>();
		Queue<BlockPos> uncheckedPositions = new Queue<BlockPos>();
		int shortestPath = 99;
		BlockPos npos = new BlockPos(pos.dimension);
		for (int j = 0; j < downPaths.Length; j++)
		{
			Vec2i offset = downPaths[j];
			npos.Set(pos.X + offset.X, pos.Y - 1, pos.Z + offset.Y);
			Block block = world.BlockAccessor.GetBlock(npos);
			npos.Y++;
			Block obj = world.BlockAccessor.GetBlock(npos, 2);
			Block aboveblock = world.BlockAccessor.GetBlock(npos, 1);
			if (obj.LiquidLevel >= ourBlock.LiquidLevel || block.Replaceable < ReplacableThreshold || aboveblock.Replaceable < ReplacableThreshold)
			{
				continue;
			}
			uncheckedPositions.Enqueue(new BlockPos(pos.X + offset.X, pos.Y, pos.Z + offset.Y, pos.dimension));
			BlockPos foundPos = BfsSearchPath(world, uncheckedPositions, pos, ourBlock);
			if (foundPos != null)
			{
				PosAndDist pad = new PosAndDist
				{
					pos = foundPos,
					dist = pos.ManhattenDistance(pos.X + offset.X, pos.Y, pos.Z + offset.Y)
				};
				if (pad.dist == 1 && ourBlock.LiquidLevel < 7)
				{
					paths.Clear();
					paths.Add(pad);
					return paths;
				}
				paths.Add(pad);
				shortestPath = Math.Min(shortestPath, pad.dist);
			}
		}
		for (int i = 0; i < paths.Count; i++)
		{
			if (paths[i].dist > shortestPath)
			{
				paths.RemoveAt(i);
				i--;
			}
		}
		return paths;
	}

	private BlockPos BfsSearchPath(IWorldAccessor world, Queue<BlockPos> uncheckedPositions, BlockPos target, Block ourBlock)
	{
		BlockPos npos = new BlockPos(target.dimension);
		BlockPos origin = null;
		while (uncheckedPositions.Count > 0)
		{
			BlockPos pos = uncheckedPositions.Dequeue();
			if (origin == null)
			{
				origin = pos;
			}
			int curDist = pos.ManhattenDistance(target);
			npos.Set(pos);
			for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++)
			{
				BlockFacing.HORIZONTALS[i].IterateThruFacingOffsets(npos);
				if (npos.ManhattenDistance(target) <= curDist)
				{
					if (npos.Equals(target))
					{
						return pos;
					}
					if (!(world.BlockAccessor.GetMostSolidBlock(npos).GetLiquidBarrierHeightOnSide(BlockFacing.HORIZONTALS[i].Opposite, npos) >= (float)(ourBlock.LiquidLevel - pos.ManhattenDistance(origin)) / 7f))
					{
						uncheckedPositions.Enqueue(npos.Copy());
					}
				}
			}
		}
		return null;
	}

	public override bool ShouldReceiveClientParticleTicks(IWorldAccessor world, IPlayer byPlayer, BlockPos pos, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		if (block.ParticleProperties == null || block.ParticleProperties.Length == 0)
		{
			return false;
		}
		if (block.LiquidCode == "lava")
		{
			return world.BlockAccessor.GetBlockAbove(pos).Replaceable > ReplacableThreshold;
		}
		handled = EnumHandling.PassThrough;
		return false;
	}

	private static AssetLocation CreateAssetLocation(JsonObject properties, string propertyName)
	{
		return CreateAssetLocation(properties, null, propertyName);
	}

	private static AssetLocation CreateAssetLocation(JsonObject properties, string prefix, string propertyName)
	{
		string value = properties[propertyName]?.AsString();
		if (value == null)
		{
			return null;
		}
		if (prefix != null)
		{
			return new AssetLocation(prefix + value);
		}
		return new AssetLocation(value);
	}
}
