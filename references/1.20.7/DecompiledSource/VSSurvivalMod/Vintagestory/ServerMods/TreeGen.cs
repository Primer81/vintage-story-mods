using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.ServerMods.NoObf;

namespace Vintagestory.ServerMods;

public class TreeGen : ITreeGenerator
{
	private IBlockAccessor blockAccessor;

	private TreeGenParams treeGenParams;

	private float size;

	private List<TreeGenBranch> branchesByDepth = new List<TreeGenBranch>();

	private TreeGenConfig config;

	private readonly ForestFloorSystem forestFloor;

	public TreeGen(TreeGenConfig config, int seed, ForestFloorSystem ffs)
	{
		this.config = config;
		forestFloor = ffs;
	}

	public void GrowTree(IBlockAccessor ba, BlockPos pos, TreeGenParams treeGenParams, IRandom random)
	{
		int treeSubType = random.NextInt(8);
		blockAccessor = ba;
		this.treeGenParams = treeGenParams;
		size = treeGenParams.size * config.sizeMultiplier + config.sizeVar.nextFloat(1f, random);
		pos.Up(config.yOffset);
		TreeGenTrunk[] trunks = config.trunks;
		branchesByDepth.Clear();
		branchesByDepth.Add(null);
		branchesByDepth.AddRange(config.branches);
		forestFloor.ClearOutline();
		TreeGenTrunk trunk = config.trunks[0];
		float trunkHeight = Math.Max(0f, trunk.dieAt.nextFloat(1f, random));
		float trunkWidthLoss = trunk.WidthLoss(random);
		for (int i = 0; i < trunks.Length; i++)
		{
			trunk = config.trunks[i];
			if (random.NextDouble() <= (double)trunk.probability)
			{
				branchesByDepth[0] = trunk;
				growBranch(random, 0, pos, treeSubType, trunk.dx, 0f, trunk.dz, trunk.angleVert.nextFloat(1f, random), trunk.angleHori.nextFloat(1f, random), size * trunk.widthMultiplier, trunkHeight, trunkWidthLoss, trunks.Length > 1);
			}
		}
		if (!treeGenParams.skipForestFloor)
		{
			forestFloor.CreateForestFloor(ba, config, pos, random, treeGenParams.treesInChunkGenerated);
		}
	}

	private void growBranch(IRandom rand, int depth, BlockPos basePos, int treeSubType, float dx, float dy, float dz, float angleVerStart, float angleHorStart, float curWidth, float dieAt, float trunkWidthLoss, bool wideTrunk)
	{
		if (depth > 30)
		{
			Console.WriteLine("TreeGen.growBranch() aborted, too many branches!");
			return;
		}
		TreeGenBranch branch = branchesByDepth[Math.Min(depth, branchesByDepth.Count - 1)];
		float widthloss = ((depth == 0) ? trunkWidthLoss : branch.WidthLoss(rand));
		float widthlossCurve = branch.widthlossCurve;
		float branchspacing = branch.branchSpacing.nextFloat(1f, rand);
		float branchstart = branch.branchStart.nextFloat(1f, rand);
		float branchQuantityStart = branch.branchQuantity.nextFloat(1f, rand);
		float branchWidthMulitplierStart = branch.branchWidthMultiplier.nextFloat(1f, rand);
		float lastreldistance = 0f;
		float totaldistance = curWidth / widthloss;
		int iteration = 0;
		float sequencesPerIteration = 1f / (curWidth / widthloss);
		BlockPos currentPos = new BlockPos(basePos.dimension);
		while (curWidth > 0f && iteration++ < 5000)
		{
			curWidth -= widthloss;
			if (widthlossCurve + curWidth / 20f < 1f)
			{
				widthloss *= widthlossCurve + curWidth / 20f;
			}
			float currentSequence = sequencesPerIteration * (float)(iteration - 1);
			if (curWidth < dieAt)
			{
				break;
			}
			float angleVer = branch.angleVertEvolve.nextFloat(angleVerStart, currentSequence);
			float angleHor = branch.angleHoriEvolve.nextFloat(angleHorStart, currentSequence);
			float sinAngleVer = GameMath.FastSin(angleVer);
			float cosAnglerHor = GameMath.FastCos(angleHor);
			float sinAngleHor = GameMath.FastSin(angleHor);
			float trunkOffsetX = Math.Max(-0.5f, Math.Min(0.5f, 0.7f * sinAngleVer * cosAnglerHor));
			float trunkOffsetZ = Math.Max(-0.5f, Math.Min(0.5f, 0.7f * sinAngleVer * sinAngleHor));
			float ddrag = branch.gravityDrag * (float)Math.Sqrt(dx * dx + dz * dz);
			dx += sinAngleVer * cosAnglerHor / Math.Max(1f, Math.Abs(ddrag));
			dy += Math.Min(1f, Math.Max(-1f, GameMath.FastCos(angleVer) - ddrag));
			dz += sinAngleVer * sinAngleHor / Math.Max(1f, Math.Abs(ddrag));
			int blockId = branch.getBlockId(rand, curWidth, config.treeBlocks, this, treeSubType);
			if (blockId == 0)
			{
				break;
			}
			currentPos.Set((float)basePos.X + dx, (float)basePos.Y + dy, (float)basePos.Z + dz);
			switch (getPlaceResumeState(currentPos, blockId, wideTrunk))
			{
			case PlaceResumeState.CanPlace:
				PlaceBlockEtc(blockId, currentPos, rand, dx, dz);
				break;
			case PlaceResumeState.Stop:
				return;
			}
			float reldistance = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz) / totaldistance;
			if (!(reldistance < branchstart) && reldistance > lastreldistance + branchspacing * (1f - reldistance))
			{
				branchspacing = branch.branchSpacing.nextFloat(1f, rand);
				lastreldistance = reldistance;
				float branchQuantity = ((branch.branchQuantityEvolve != null) ? branch.branchQuantityEvolve.nextFloat(branchQuantityStart, currentSequence) : branch.branchQuantity.nextFloat(1f, rand));
				if (rand.NextDouble() < (double)branchQuantity % 1.0)
				{
					branchQuantity += 1f;
				}
				curWidth = GrowBranchesHere((int)branchQuantity, branch, depth + 1, rand, curWidth, branchWidthMulitplierStart, currentSequence, angleHor, dx + trunkOffsetX, dy, dz + trunkOffsetZ, basePos, treeSubType, trunkWidthLoss);
			}
		}
	}

	private float GrowBranchesHere(int branchQuantity, TreeGenBranch branch, int newDepth, IRandom rand, float curWidth, float branchWidthMulitplierStart, float currentSequence, float angleHor, float dx, float dy, float dz, BlockPos basePos, int treeSubType, float trunkWidthLoss)
	{
		float prevHorAngle = 0f;
		float minHorangleDist = Math.Min((float)Math.PI / 5f, branch.branchHorizontalAngle.var / 5f);
		bool first = true;
		while (branchQuantity-- > 0)
		{
			curWidth *= branch.branchWidthLossMul;
			float horAngle = angleHor + branch.branchHorizontalAngle.nextFloat(1f, rand);
			int tries = 10;
			while (!first && Math.Abs(horAngle - prevHorAngle) < minHorangleDist && tries-- > 0)
			{
				float newAngle = angleHor + branch.branchHorizontalAngle.nextFloat(1f, rand);
				if (Math.Abs(horAngle - prevHorAngle) < Math.Abs(newAngle - prevHorAngle))
				{
					horAngle = newAngle;
				}
			}
			growBranch(curWidth: (branch.branchWidthMultiplierEvolve == null) ? branch.branchWidthMultiplier.nextFloat(curWidth, rand) : (curWidth * branch.branchWidthMultiplierEvolve.nextFloat(branchWidthMulitplierStart, currentSequence)), rand: rand, depth: newDepth, basePos: basePos, treeSubType: treeSubType, dx: dx, dy: dy, dz: dz, angleVerStart: branch.branchVerticalAngle.nextFloat(1f, rand), angleHorStart: horAngle, dieAt: Math.Max(0f, branch.dieAt.nextFloat(1f, rand)), trunkWidthLoss: trunkWidthLoss, wideTrunk: false);
			first = false;
			prevHorAngle = angleHor + horAngle;
		}
		return curWidth;
	}

	private void PlaceBlockEtc(int blockId, BlockPos currentPos, IRandom rand, float dx, float dz)
	{
		blockAccessor.SetBlock(blockId, currentPos);
		if (blockAccessor.GetBlock(blockId).BlockMaterial == EnumBlockMaterial.Wood && treeGenParams.mossGrowthChance > 0f && config.treeBlocks.mossDecorBlock != null)
		{
			double rnd = rand.NextDouble();
			int faceIndex = ((treeGenParams.hemisphere != 0) ? 2 : 0);
			int i = 2;
			while (i >= 0 && !(rnd > (double)(treeGenParams.mossGrowthChance * (float)i)))
			{
				BlockFacing face = BlockFacing.HORIZONTALS[faceIndex % 4];
				if (!blockAccessor.GetBlockOnSide(currentPos, face).SideSolid[face.Opposite.Index])
				{
					blockAccessor.SetDecor(config.treeBlocks.mossDecorBlock, currentPos, face);
				}
				faceIndex += rand.NextInt(4);
				i--;
			}
		}
		int idz = (int)(dz + 16f);
		int idx = (int)(dx + 16f);
		if (idz > 1 && idz < 31 && idx > 1 && idx < 31)
		{
			short[] outline = forestFloor.GetOutline();
			int canopyIndex = idz * 33 + idx;
			outline[canopyIndex - 66 - 2]++;
			outline[canopyIndex - 66 - 1]++;
			outline[canopyIndex - 66]++;
			outline[canopyIndex - 66 + 1]++;
			outline[canopyIndex - 66 + 2]++;
			outline[canopyIndex - 33 - 2]++;
			outline[canopyIndex - 33 - 1] += 2;
			outline[canopyIndex - 33] += 2;
			outline[canopyIndex - 33 + 1] += 2;
			outline[canopyIndex - 33 + 2]++;
			outline[canopyIndex - 2]++;
			outline[canopyIndex - 1] += 2;
			outline[canopyIndex] += 3;
			outline[canopyIndex + 1] += 2;
			outline[canopyIndex + 2]++;
			outline[canopyIndex + 33]++;
		}
		if (!(treeGenParams.vinesGrowthChance > 0f) || !(rand.NextDouble() < (double)treeGenParams.vinesGrowthChance) || config.treeBlocks.vinesBlock == null)
		{
			return;
		}
		BlockFacing facing = BlockFacing.HORIZONTALS[rand.NextInt(4)];
		BlockPos vinePos = currentPos.AddCopy(facing);
		float cnt = 1f + (float)rand.NextInt(11) * (treeGenParams.vinesGrowthChance + 0.2f);
		while (blockAccessor.GetBlockId(vinePos) == 0 && cnt-- > 0f)
		{
			Block block = config.treeBlocks.vinesBlock;
			if (cnt <= 0f && config.treeBlocks.vinesEndBlock != null)
			{
				block = config.treeBlocks.vinesEndBlock;
			}
			block.TryPlaceBlockForWorldGen(blockAccessor, vinePos, facing, rand);
			vinePos.Down();
		}
	}

	internal bool TriggerRandomOtherBlock(IRandom lcgRandom)
	{
		return lcgRandom.NextDouble() < (double)treeGenParams.otherBlockChance * config.treeBlocks.otherLogChance;
	}

	private PlaceResumeState getPlaceResumeState(BlockPos targetPos, int desiredblockId, bool wideTrunk)
	{
		if (targetPos.X < 0 || targetPos.Y < 0 || targetPos.Z < 0 || targetPos.X >= blockAccessor.MapSizeX || targetPos.Y >= blockAccessor.MapSizeY || targetPos.Z >= blockAccessor.MapSizeZ)
		{
			return PlaceResumeState.Stop;
		}
		int currentblockId = blockAccessor.GetBlockId(targetPos);
		switch (currentblockId)
		{
		case -1:
			return PlaceResumeState.CannotPlace;
		case 0:
			return PlaceResumeState.CanPlace;
		default:
		{
			Block currentBlock = blockAccessor.GetBlock(currentblockId);
			Block desiredBock = blockAccessor.GetBlock(desiredblockId);
			if ((currentBlock.Fertility == 0 || desiredBock.BlockMaterial != EnumBlockMaterial.Wood) && currentBlock.BlockMaterial != EnumBlockMaterial.Leaves && currentBlock.Replaceable < 6000 && !wideTrunk && !config.treeBlocks.blockIds.Contains(currentBlock.BlockId))
			{
				return PlaceResumeState.Stop;
			}
			if (desiredBock.Replaceable <= currentBlock.Replaceable)
			{
				return PlaceResumeState.CanPlace;
			}
			return PlaceResumeState.CannotPlace;
		}
		}
	}
}
