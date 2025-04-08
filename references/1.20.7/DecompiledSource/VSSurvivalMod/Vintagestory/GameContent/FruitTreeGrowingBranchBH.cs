using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class FruitTreeGrowingBranchBH : BlockEntityBehavior
{
	private int callbackTimeMs = 20000;

	public float VDrive;

	public float HDrive;

	private Block stemBlock;

	private BlockFruitTreeBranch branchBlock;

	private BlockFruitTreeFoliage leavesBlock;

	private long listenerId;

	private BlockEntityFruitTreeBranch ownBe => Blockentity as BlockEntityFruitTreeBranch;

	public FruitTreeGrowingBranchBH(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		if (Api.Side == EnumAppSide.Server)
		{
			listenerId = Blockentity.RegisterGameTickListener(OnTick, callbackTimeMs + Api.World.Rand.Next(callbackTimeMs));
		}
		stemBlock = Api.World.GetBlock(ownBe.Block.CodeWithVariant("type", "stem"));
		branchBlock = Api.World.GetBlock(ownBe.Block.CodeWithVariant("type", "branch")) as BlockFruitTreeBranch;
		leavesBlock = Api.World.GetBlock(AssetLocation.Create(ownBe.Block.Attributes["foliageBlock"].AsString(), ownBe.Block.Code.Domain)) as BlockFruitTreeFoliage;
		if (ownBe.Block == leavesBlock)
		{
			ownBe.PartType = EnumTreePartType.Leaves;
		}
		if (ownBe.Block == branchBlock)
		{
			ownBe.PartType = EnumTreePartType.Branch;
		}
		if (ownBe.lastGrowthAttemptTotalDays == 0.0)
		{
			ownBe.lastGrowthAttemptTotalDays = api.World.Calendar.TotalDays;
		}
	}

	protected void OnTick(float dt)
	{
		if (ownBe.RootOff == null)
		{
			return;
		}
		if (!(Api.World.BlockAccessor.GetBlockEntity(ownBe.Pos.AddCopy(ownBe.RootOff)) is BlockEntityFruitTreeBranch rootBe))
		{
			if (Api.World.Rand.NextDouble() < 0.25)
			{
				Api.World.BlockAccessor.BreakBlock(ownBe.Pos, null);
			}
			return;
		}
		double totalDays = Api.World.Calendar.TotalDays;
		if (ownBe.GrowTries > 60 || ownBe.FoliageState == EnumFoliageState.Dead)
		{
			ownBe.lastGrowthAttemptTotalDays = totalDays;
			return;
		}
		ownBe.lastGrowthAttemptTotalDays = Math.Max(ownBe.lastGrowthAttemptTotalDays, totalDays - (double)(Api.World.Calendar.DaysPerYear * 4));
		if (totalDays - ownBe.lastGrowthAttemptTotalDays < 0.5)
		{
			return;
		}
		double hoursPerDay = Api.World.Calendar.HoursPerDay;
		FruitTreeProperties props = null;
		if (ownBe.TreeType == null)
		{
			Api.World.BlockAccessor.SetBlock(0, ownBe.Pos);
			return;
		}
		FruitTreeRootBH behavior = rootBe.GetBehavior<FruitTreeRootBH>();
		if (behavior == null || !behavior.propsByType.TryGetValue(ownBe.TreeType, out props))
		{
			return;
		}
		if (ownBe.FoliageState == EnumFoliageState.Dead)
		{
			ownBe.UnregisterGameTickListener(listenerId);
			listenerId = 0L;
			return;
		}
		double growthStepDays = props.GrowthStepDays;
		ClimateCondition baseClimate = Api.World.BlockAccessor.GetClimateAt(ownBe.Pos, EnumGetClimateMode.WorldGenValues);
		if (baseClimate == null)
		{
			return;
		}
		while (totalDays - ownBe.lastGrowthAttemptTotalDays > growthStepDays)
		{
			if (Api.World.BlockAccessor.GetClimateAt(ownBe.Pos, baseClimate, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, (double)(int)ownBe.lastGrowthAttemptTotalDays + hoursPerDay / 2.0).Temperature < 12f)
			{
				ownBe.lastGrowthAttemptTotalDays += growthStepDays;
				continue;
			}
			TryGrow();
			ownBe.lastGrowthAttemptTotalDays += growthStepDays;
			ownBe.GrowTries++;
		}
	}

	public void OnNeighbourBranchRemoved(BlockFacing facing)
	{
		if ((ownBe.SideGrowth & (1 << facing.Index)) > 0)
		{
			ownBe.GrowTries = Math.Min(55, ownBe.GrowTries - 5);
			HDrive += 1f;
		}
	}

	private void TryGrow()
	{
		Random rnd = Api.World.Rand;
		if (ownBe.TreeType == "" || ownBe.TreeType == null)
		{
			return;
		}
		switch (ownBe.PartType)
		{
		case EnumTreePartType.Stem:
		{
			Block upBlock2 = Api.World.BlockAccessor.GetBlock(ownBe.Pos.UpCopy());
			if (upBlock2.Id == 0)
			{
				TryGrowTo(EnumTreePartType.Leaves, BlockFacing.UP);
				ownBe.GrowTries /= 2;
				break;
			}
			if (upBlock2 == leavesBlock)
			{
				TryGrowTo(EnumTreePartType.Branch, BlockFacing.UP);
				break;
			}
			BlockFacing[] hORIZONTALS = ((BlockFacing[])BlockFacing.ALLFACES.Clone()).Shuffle(Api.World.Rand);
			foreach (BlockFacing facing in hORIZONTALS)
			{
				if ((ownBe.SideGrowth & (1 << facing.Index)) > 0)
				{
					Block nblock = Api.World.BlockAccessor.GetBlock(ownBe.Pos.AddCopy(facing));
					if (nblock == leavesBlock)
					{
						TryGrowTo(EnumTreePartType.Branch, facing, 1, 1f);
					}
					else if (nblock.Id == 0)
					{
						TryGrowTo(EnumTreePartType.Leaves, facing);
					}
				}
			}
			break;
		}
		case EnumTreePartType.Cutting:
		{
			if (ownBe.FoliageState == EnumFoliageState.Dead || ownBe.GrowTries < 1)
			{
				break;
			}
			FruitTreeRootBH rootBh = (Api.World.BlockAccessor.GetBlockEntity(ownBe.Pos.AddCopy(ownBe.RootOff)) as BlockEntityFruitTreeBranch).GetBehavior<FruitTreeRootBH>();
			if (rootBh != null)
			{
				double rndval = Api.World.Rand.NextDouble();
				if ((ownBe.GrowthDir.IsVertical && (double)branchBlock.TypeProps[ownBe.TreeType].CuttingRootingChance >= rndval) || (ownBe.GrowthDir.IsHorizontal && (double)branchBlock.TypeProps[ownBe.TreeType].CuttingGraftChance >= rndval))
				{
					Api.World.BlockAccessor.ExchangeBlock(branchBlock.Id, ownBe.Pos);
					ownBe.GrowTries += 4;
					ownBe.PartType = EnumTreePartType.Branch;
					rootBh.propsByType[ownBe.TreeType].State = EnumFruitTreeState.Young;
					TryGrowTo(EnumTreePartType.Leaves, ownBe.GrowthDir);
					ownBe.MarkDirty(redrawOnClient: true);
				}
				else
				{
					rootBh.propsByType[ownBe.TreeType].State = EnumFruitTreeState.Dead;
					ownBe.FoliageState = EnumFoliageState.Dead;
					ownBe.MarkDirty(redrawOnClient: true);
				}
			}
			break;
		}
		case EnumTreePartType.Branch:
		{
			Block upBlock = Api.World.BlockAccessor.GetBlock(ownBe.Pos.UpCopy());
			if (ownBe.GrowthDir == BlockFacing.UP)
			{
				if (ownBe.GrowTries > 5 && upBlock == leavesBlock && VDrive > 0f)
				{
					TryGrowTo(EnumTreePartType.Branch, BlockFacing.UP);
					TryGrowTo(EnumTreePartType.Leaves, BlockFacing.UP, 2);
					break;
				}
				bool growStem = ownBe.GrowTries > 20 && upBlock == branchBlock && ownBe.Height < 3;
				bool growThinBranch = ownBe.GrowTries > 20 && upBlock == branchBlock && ownBe.Height >= 3 && rnd.NextDouble() < 0.05;
				if (growStem || growThinBranch)
				{
					if (growStem)
					{
						Api.World.BlockAccessor.ExchangeBlock(stemBlock.Id, ownBe.Pos);
						ownBe.PartType = EnumTreePartType.Stem;
						ownBe.MarkDirty(redrawOnClient: true);
					}
					for (int i = 0; i < 4; i++)
					{
						BlockFacing face = BlockFacing.HORIZONTALS[i];
						BlockPos npos = ownBe.Pos.AddCopy(face);
						if (Api.World.BlockAccessor.GetBlock(npos) != leavesBlock)
						{
							continue;
						}
						if (ownBe.Height >= 2 && rnd.NextDouble() < 0.6 && HDrive > 0f)
						{
							if (TryGrowTo(EnumTreePartType.Branch, face))
							{
								ownBe.SideGrowth |= 1 << i;
								ownBe.MarkDirty(redrawOnClient: true);
								TryGrowTo(EnumTreePartType.Leaves, face, 2);
							}
							continue;
						}
						bool hasBranch = false;
						BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
						foreach (BlockFacing nface in hORIZONTALS)
						{
							hasBranch |= Api.World.BlockAccessor.GetBlock(npos.AddCopy(nface)) == branchBlock;
						}
						if (!hasBranch)
						{
							Api.World.BlockAccessor.SetBlock(0, npos);
						}
					}
				}
				else if (upBlock.IsReplacableBy(leavesBlock))
				{
					TryGrowTo(EnumTreePartType.Leaves, BlockFacing.UP);
				}
				else if (ownBe.Height > 0)
				{
					BlockFacing facing2 = BlockFacing.HORIZONTALS[rnd.Next(4)];
					TryGrowTo(EnumTreePartType.Leaves, facing2);
				}
				break;
			}
			if (rnd.NextDouble() > 0.5)
			{
				BlockFacing dir = ownBe.GrowthDir;
				Block nblock2 = Api.World.BlockAccessor.GetBlock(ownBe.Pos.AddCopy(dir));
				TryGrowTo((nblock2 == leavesBlock && HDrive > 0f) ? EnumTreePartType.Branch : EnumTreePartType.Leaves, dir);
				break;
			}
			int k = 0;
			for (int j = 0; j < 5; j++)
			{
				BlockFacing facing3 = BlockFacing.ALLFACES[j];
				if (rnd.NextDouble() < 0.4 && k < 2)
				{
					if (TryGrowTo(EnumTreePartType.Leaves, facing3))
					{
						ownBe.MarkDirty(redrawOnClient: true);
					}
					k++;
				}
			}
			break;
		}
		}
	}

	private bool TryGrowTo(EnumTreePartType partType, BlockFacing facing, int len = 1, float? hdrive = null)
	{
		BlockPos pos = ownBe.Pos.AddCopy(facing, len);
		Block block = stemBlock;
		if (partType == EnumTreePartType.Branch)
		{
			block = branchBlock;
		}
		if (partType == EnumTreePartType.Leaves)
		{
			block = leavesBlock;
		}
		Block nblock = Api.World.BlockAccessor.GetBlock(pos);
		if ((partType != EnumTreePartType.Leaves || !nblock.IsReplacableBy(leavesBlock)) && (partType != EnumTreePartType.Branch || nblock != leavesBlock) && (partType != 0 || nblock != branchBlock))
		{
			return false;
		}
		BlockPos rootPos = ownBe.Pos.AddCopy(ownBe.RootOff);
		if (!(Api.World.BlockAccessor.GetBlockEntity(rootPos) is BlockEntityFruitTreeBranch rootBe))
		{
			return false;
		}
		FruitTreeRootBH bh = rootBe.GetBehavior<FruitTreeRootBH>();
		if (bh != null)
		{
			bh.BlocksGrown++;
		}
		Api.World.BlockAccessor.SetBlock(block.Id, pos);
		BlockEntityFruitTreeBranch beb = Api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityFruitTreeBranch;
		FruitTreeGrowingBranchBH beh = beb?.GetBehavior<FruitTreeGrowingBranchBH>();
		if (beh != null)
		{
			beh.VDrive = VDrive - (float)(facing.IsVertical ? 1 : 0);
			float hd = ((!hdrive.HasValue) ? (HDrive - (float)(facing.IsHorizontal ? 1 : 0)) : hdrive.Value);
			beh.HDrive = hd;
			beb.ParentOff = facing.Normali.Clone();
			beb.lastGrowthAttemptTotalDays = ownBe.lastGrowthAttemptTotalDays;
		}
		if (Api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityFruitTreePart be)
		{
			if (partType != 0)
			{
				be.FoliageState = EnumFoliageState.Plain;
			}
			be.GrowthDir = facing;
			be.TreeType = ownBe.TreeType;
			be.PartType = partType;
			be.RootOff = (rootPos - pos).ToVec3i();
			be.Height = ownBe.Height + facing.Normali.Y;
			be.OnGrown();
		}
		return true;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		base.GetBlockInfo(forPlayer, dsc);
		if (ownBe.PartType == EnumTreePartType.Cutting)
		{
			dsc.AppendLine((ownBe.FoliageState == EnumFoliageState.Dead) ? ("<font color=\"#ff8080\">" + Lang.Get("Dead tree cutting") + "</font>") : Lang.Get("Establishing tree cutting"));
			if (ownBe.FoliageState != EnumFoliageState.Dead && branchBlock.TypeProps.TryGetValue(ownBe.TreeType, out var typeprops))
			{
				dsc.AppendLine(Lang.Get("{0}% survival chance", 100f * (ownBe.GrowthDir.IsVertical ? typeprops.CuttingRootingChance : typeprops.CuttingGraftChance)));
			}
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		VDrive = tree.GetFloat("vdrive");
		HDrive = tree.GetFloat("hdrive");
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetFloat("vdrive", VDrive);
		tree.SetFloat("hdrive", HDrive);
	}
}
