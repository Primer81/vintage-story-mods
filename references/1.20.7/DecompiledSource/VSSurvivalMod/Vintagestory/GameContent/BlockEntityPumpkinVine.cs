using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityPumpkinVine : BlockEntity
{
	public static readonly float pumpkinHoursToGrow = 12f;

	public static readonly float vineHoursToGrow = 12f;

	public static readonly float vineHoursToGrowStage2 = 6f;

	public static readonly float bloomProbability = 0.5f;

	public static readonly float debloomProbability = 0.5f;

	public static readonly float vineSpawnProbability = 0.5f;

	public static readonly float preferredGrowthDirProbability = 0.75f;

	public static readonly int maxAllowedPumpkinGrowthTries = 3;

	public long growListenerId;

	public Block stage1VineBlock;

	public Block pumpkinBlock;

	public double totalHoursForNextStage;

	public bool canBloom;

	public int pumpkinGrowthTries;

	public Dictionary<BlockFacing, double> pumpkinTotalHoursForNextStage = new Dictionary<BlockFacing, double>();

	public BlockPos parentPlantPos;

	public BlockFacing preferredGrowthDir;

	public int internalStage;

	public BlockEntityPumpkinVine()
	{
		BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
		foreach (BlockFacing facing in hORIZONTALS)
		{
			pumpkinTotalHoursForNextStage.Add(facing, 0.0);
		}
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		stage1VineBlock = api.World.GetBlock(new AssetLocation("pumpkin-vine-1-normal"));
		pumpkinBlock = api.World.GetBlock(new AssetLocation("pumpkin-fruit-1"));
		if (api is ICoreServerAPI)
		{
			growListenerId = RegisterGameTickListener(TryGrow, 2000);
		}
	}

	public void CreatedFromParent(BlockPos parentPlantPos, BlockFacing preferredGrowthDir, double currentTotalHours)
	{
		totalHoursForNextStage = currentTotalHours + (double)vineHoursToGrow;
		this.parentPlantPos = parentPlantPos;
		this.preferredGrowthDir = preferredGrowthDir;
	}

	private void TryGrow(float dt)
	{
		if (!DieIfParentDead())
		{
			while (Api.World.Calendar.TotalHours > totalHoursForNextStage)
			{
				GrowVine();
				totalHoursForNextStage += vineHoursToGrow;
			}
			TryGrowPumpkins();
		}
	}

	private void TryGrowPumpkins()
	{
		BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
		foreach (BlockFacing facing in hORIZONTALS)
		{
			double pumpkinTotalHours = pumpkinTotalHoursForNextStage[facing];
			while (pumpkinTotalHours > 0.0 && Api.World.Calendar.TotalHours > pumpkinTotalHours)
			{
				BlockPos pumpkinPos = Pos.AddCopy(facing);
				Block pumpkin = Api.World.BlockAccessor.GetBlock(pumpkinPos);
				if (IsPumpkin(pumpkin))
				{
					int currentStage = CurrentPumpkinStage(pumpkin);
					if (currentStage == 4)
					{
						pumpkinTotalHours = 0.0;
					}
					else
					{
						SetPumpkinStage(pumpkin, pumpkinPos, currentStage + 1);
						pumpkinTotalHours += (double)pumpkinHoursToGrow;
					}
				}
				else
				{
					pumpkinTotalHours = 0.0;
				}
				pumpkinTotalHoursForNextStage[facing] = pumpkinTotalHours;
			}
		}
	}

	private void GrowVine()
	{
		internalStage++;
		Block block = Api.World.BlockAccessor.GetBlock(Pos);
		int currentStage = CurrentVineStage(block);
		if (internalStage > 6)
		{
			SetVineStage(block, currentStage + 1);
		}
		if (IsBlooming())
		{
			if (pumpkinGrowthTries >= maxAllowedPumpkinGrowthTries || Api.World.Rand.NextDouble() < (double)debloomProbability)
			{
				pumpkinGrowthTries = 0;
				SetVineStage(block, 3);
			}
			else
			{
				pumpkinGrowthTries++;
				TrySpawnPumpkin(totalHoursForNextStage - (double)vineHoursToGrow);
			}
		}
		if (currentStage == 3)
		{
			if (canBloom && Api.World.Rand.NextDouble() < (double)bloomProbability)
			{
				SetBloomingStage(block);
			}
			canBloom = false;
		}
		if (currentStage == 2)
		{
			if (Api.World.Rand.NextDouble() < (double)vineSpawnProbability)
			{
				TrySpawnNewVine();
			}
			totalHoursForNextStage += vineHoursToGrowStage2;
			canBloom = true;
			SetVineStage(block, currentStage + 1);
		}
		if (currentStage < 2)
		{
			SetVineStage(block, currentStage + 1);
		}
	}

	private void TrySpawnPumpkin(double curTotalHours)
	{
		BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
		foreach (BlockFacing facing in hORIZONTALS)
		{
			BlockPos candidatePos = Pos.AddCopy(facing);
			Block block = Api.World.BlockAccessor.GetBlock(candidatePos);
			if (CanReplace(block) && PumpkinCropBehavior.CanSupportPumpkin(Api, candidatePos.DownCopy()))
			{
				Api.World.BlockAccessor.SetBlock(pumpkinBlock.BlockId, candidatePos);
				pumpkinTotalHoursForNextStage[facing] = curTotalHours + (double)pumpkinHoursToGrow;
				break;
			}
		}
	}

	private bool IsPumpkin(Block block)
	{
		return block?.Code.GetName().StartsWithOrdinal("pumpkin-fruit") ?? false;
	}

	private bool DieIfParentDead()
	{
		if (parentPlantPos == null)
		{
			Die();
			return true;
		}
		Block parentBlock = Api.World.BlockAccessor.GetBlock(parentPlantPos);
		if (!IsValidParentBlock(parentBlock) && Api.World.BlockAccessor.GetChunkAtBlockPos(parentPlantPos) != null)
		{
			Die();
			return true;
		}
		return false;
	}

	private void Die()
	{
		UnregisterGameTickListener(growListenerId);
		growListenerId = 0L;
		Api.World.BlockAccessor.SetBlock(0, Pos);
	}

	private bool IsValidParentBlock(Block parentBlock)
	{
		if (parentBlock != null)
		{
			string blockCode = parentBlock.Code.GetName();
			if (blockCode.StartsWithOrdinal("crop-pumpkin") || blockCode.StartsWithOrdinal("pumpkin-vine"))
			{
				return true;
			}
		}
		return false;
	}

	public bool IsBlooming()
	{
		Block block = Api.World.BlockAccessor.GetBlock(Pos);
		block.LastCodePart();
		return block.LastCodePart() == "blooming";
	}

	private bool CanReplace(Block block)
	{
		if (block != null)
		{
			if (block.Replaceable >= 6000)
			{
				return !block.Code.GetName().Contains("pumpkin");
			}
			return false;
		}
		return true;
	}

	private void SetVineStage(Block block, int toStage)
	{
		try
		{
			ReplaceSelf(block.CodeWithParts(toStage.ToString() ?? "", (toStage == 4) ? "withered" : "normal"));
		}
		catch (Exception)
		{
			Api.World.BlockAccessor.SetBlock(0, Pos);
		}
	}

	private void SetPumpkinStage(Block pumpkinBlock, BlockPos pumpkinPos, int toStage)
	{
		Block nextBlock = Api.World.GetBlock(pumpkinBlock.CodeWithParts(toStage.ToString() ?? ""));
		if (nextBlock != null)
		{
			Api.World.BlockAccessor.ExchangeBlock(nextBlock.BlockId, pumpkinPos);
		}
	}

	private void SetBloomingStage(Block block)
	{
		ReplaceSelf(block.CodeWithParts("blooming"));
	}

	private void ReplaceSelf(AssetLocation blockCode)
	{
		Block nextBlock = Api.World.GetBlock(blockCode);
		if (nextBlock != null)
		{
			Api.World.BlockAccessor.ExchangeBlock(nextBlock.BlockId, Pos);
		}
	}

	private void TrySpawnNewVine()
	{
		BlockFacing spawnDir = GetVineSpawnDirection();
		BlockPos newVinePos = Pos.AddCopy(spawnDir);
		Block blockToReplace = Api.World.BlockAccessor.GetBlock(newVinePos);
		if (!IsReplaceable(blockToReplace))
		{
			return;
		}
		newVinePos.Y--;
		if (CanGrowOn(Api, newVinePos))
		{
			newVinePos.Y++;
			Api.World.BlockAccessor.SetBlock(stage1VineBlock.BlockId, newVinePos);
			if (Api.World.BlockAccessor.GetBlockEntity(newVinePos) is BlockEntityPumpkinVine be)
			{
				be.CreatedFromParent(Pos, spawnDir, totalHoursForNextStage);
			}
		}
	}

	private bool CanGrowOn(ICoreAPI api, BlockPos pos)
	{
		return api.World.BlockAccessor.GetMostSolidBlock(pos).CanAttachBlockAt(api.World.BlockAccessor, stage1VineBlock, pos, BlockFacing.UP);
	}

	private bool IsReplaceable(Block block)
	{
		if (block != null)
		{
			return block.Replaceable >= 6000;
		}
		return true;
	}

	private BlockFacing GetVineSpawnDirection()
	{
		if (Api.World.Rand.NextDouble() < (double)preferredGrowthDirProbability)
		{
			return preferredGrowthDir;
		}
		return DirectionAdjacentToPreferred();
	}

	private BlockFacing DirectionAdjacentToPreferred()
	{
		if (BlockFacing.NORTH == preferredGrowthDir || BlockFacing.SOUTH == preferredGrowthDir)
		{
			if (!(Api.World.Rand.NextDouble() < 0.5))
			{
				return BlockFacing.WEST;
			}
			return BlockFacing.EAST;
		}
		if (!(Api.World.Rand.NextDouble() < 0.5))
		{
			return BlockFacing.SOUTH;
		}
		return BlockFacing.NORTH;
	}

	private int CurrentVineStage(Block block)
	{
		int stage = 0;
		int.TryParse(block.LastCodePart(1), out stage);
		return stage;
	}

	private int CurrentPumpkinStage(Block block)
	{
		int stage = 0;
		int.TryParse(block.LastCodePart(), out stage);
		return stage;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		totalHoursForNextStage = tree.GetDouble("totalHoursForNextStage");
		canBloom = tree.GetInt("canBloom") > 0;
		BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
		foreach (BlockFacing facing in hORIZONTALS)
		{
			pumpkinTotalHoursForNextStage[facing] = tree.GetDouble(facing.Code);
		}
		pumpkinGrowthTries = tree.GetInt("pumpkinGrowthTries");
		parentPlantPos = new BlockPos(tree.GetInt("parentPlantPosX"), tree.GetInt("parentPlantPosY"), tree.GetInt("parentPlantPosZ"));
		preferredGrowthDir = BlockFacing.ALLFACES[tree.GetInt("preferredGrowthDir")];
		internalStage = tree.GetInt("internalStage");
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetDouble("totalHoursForNextStage", totalHoursForNextStage);
		tree.SetInt("canBloom", canBloom ? 1 : 0);
		BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
		foreach (BlockFacing facing in hORIZONTALS)
		{
			tree.SetDouble(facing.Code, pumpkinTotalHoursForNextStage[facing]);
		}
		tree.SetInt("pumpkinGrowthTries", pumpkinGrowthTries);
		if (parentPlantPos != null)
		{
			tree.SetInt("parentPlantPosX", parentPlantPos.X);
			tree.SetInt("parentPlantPosY", parentPlantPos.Y);
			tree.SetInt("parentPlantPosZ", parentPlantPos.Z);
		}
		if (preferredGrowthDir != null)
		{
			tree.SetInt("preferredGrowthDir", preferredGrowthDir.Index);
		}
		tree.SetInt("internalStage", internalStage);
	}
}
