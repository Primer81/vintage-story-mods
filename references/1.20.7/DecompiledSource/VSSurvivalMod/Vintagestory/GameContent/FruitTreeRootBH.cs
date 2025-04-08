using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class FruitTreeRootBH : BlockEntityBehavior
{
	public int BlocksGrown;

	public int BlocksRemoved;

	public double TreePlantedTotalDays;

	public double LastRootTickTotalDays;

	public Dictionary<string, FruitTreeProperties> propsByType = new Dictionary<string, FruitTreeProperties>();

	private RoomRegistry roomreg;

	private ItemStack parentPlantStack;

	private BlockFruitTreeBranch blockBranch;

	private double stateUpdateIntervalDays = 1.0 / 3.0;

	public double nonFloweringYoungDays = 30.0;

	private float greenhouseTempBonus;

	private BlockEntity be => Blockentity;

	private BlockEntityFruitTreeBranch bebr => be as BlockEntityFruitTreeBranch;

	public bool IsYoung => Api?.World.Calendar.TotalDays - TreePlantedTotalDays < nonFloweringYoungDays;

	public FruitTreeRootBH(BlockEntity blockentity, ItemStack parentPlantStack)
		: base(blockentity)
	{
		this.parentPlantStack = parentPlantStack;
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		if (api.Side == EnumAppSide.Server && Api.World.Config.GetBool("processCrops", defaultValue: true))
		{
			Blockentity.RegisterGameTickListener(onRootTick, 5000, api.World.Rand.Next(5000));
		}
		roomreg = api.ModLoader.GetModSystem<RoomRegistry>();
		blockBranch = be.Block as BlockFruitTreeBranch;
		RegisterTreeType(bebr.TreeType);
		double totalDays = api.World.Calendar.TotalDays;
		if (TreePlantedTotalDays == 0.0)
		{
			TreePlantedTotalDays = totalDays;
			LastRootTickTotalDays = totalDays;
		}
		else
		{
			TreePlantedTotalDays = Math.Min(TreePlantedTotalDays, totalDays);
			LastRootTickTotalDays = Math.Min(LastRootTickTotalDays, totalDays);
		}
	}

	public void RegisterTreeType(string treeType)
	{
		if (treeType != null && !propsByType.ContainsKey(treeType))
		{
			if (!blockBranch.TypeProps.TryGetValue(bebr.TreeType, out var typeProps))
			{
				Api.Logger.Error("Missing fruitTreeProperties for dynamic tree of type '" + bebr.TreeType + "', will use default values.");
				typeProps = new FruitTreeTypeProperties();
			}
			Random rnd = Api.World.Rand;
			FruitTreeProperties fruitTreeProperties2 = (propsByType[treeType] = new FruitTreeProperties
			{
				EnterDormancyTemp = typeProps.EnterDormancyTemp.nextFloat(1f, rnd),
				LeaveDormancyTemp = typeProps.LeaveDormancyTemp.nextFloat(1f, rnd),
				FloweringDays = typeProps.FloweringDays.nextFloat(1f, rnd),
				FruitingDays = typeProps.FruitingDays.nextFloat(1f, rnd),
				RipeDays = typeProps.RipeDays.nextFloat(1f, rnd),
				GrowthStepDays = typeProps.GrowthStepDays.nextFloat(1f, rnd),
				DieBelowTemp = typeProps.DieBelowTemp.nextFloat(1f, rnd),
				FruitStacks = typeProps.FruitStacks,
				CycleType = typeProps.CycleType,
				VernalizationHours = (typeProps.VernalizationHours?.nextFloat(1f, rnd) ?? 0f),
				VernalizationTemp = (typeProps.VernalizationTemp?.nextFloat(1f, rnd) ?? 0f),
				BlossomAtYearRel = (typeProps.BlossomAtYearRel?.nextFloat(1f, rnd) ?? 0f),
				LooseLeavesBelowTemp = (typeProps.LooseLeavesBelowTemp?.nextFloat(1f, rnd) ?? 0f),
				RootSizeMul = (typeProps.RootSizeMul?.nextFloat(1f, rnd) ?? 0f)
			});
			FruitTreeProperties props = fruitTreeProperties2;
			if (parentPlantStack != null)
			{
				props.EnterDormancyTemp = typeProps.EnterDormancyTemp.ClampToRange((props.EnterDormancyTemp + parentPlantStack.Attributes?.GetFloat("enterDormancyTempDiff")).GetValueOrDefault());
				props.LeaveDormancyTemp = typeProps.LeaveDormancyTemp.ClampToRange((props.LeaveDormancyTemp + parentPlantStack.Attributes?.GetFloat("leaveDormancyTempDiff")).GetValueOrDefault());
				props.FloweringDays = typeProps.FloweringDays.ClampToRange((props.FloweringDays + parentPlantStack.Attributes?.GetFloat("floweringDaysDiff")).GetValueOrDefault());
				props.FruitingDays = typeProps.FruitingDays.ClampToRange((props.FruitingDays + parentPlantStack.Attributes?.GetFloat("fruitingDaysDiff")).GetValueOrDefault());
				props.RipeDays = typeProps.RipeDays.ClampToRange((props.RipeDays + parentPlantStack.Attributes?.GetFloat("ripeDaysDiff")).GetValueOrDefault());
				props.GrowthStepDays = typeProps.GrowthStepDays.ClampToRange((props.GrowthStepDays + parentPlantStack.Attributes?.GetFloat("growthStepDaysDiff")).GetValueOrDefault());
				props.DieBelowTemp = typeProps.DieBelowTemp.ClampToRange((props.DieBelowTemp + parentPlantStack.Attributes?.GetFloat("dieBelowTempDiff")).GetValueOrDefault());
				props.VernalizationHours = typeProps.VernalizationHours.ClampToRange((props.VernalizationHours + parentPlantStack.Attributes?.GetFloat("vernalizationHoursDiff")).GetValueOrDefault());
				props.VernalizationTemp = typeProps.VernalizationTemp.ClampToRange((props.VernalizationTemp + parentPlantStack.Attributes?.GetFloat("vernalizationTempDiff")).GetValueOrDefault());
				props.BlossomAtYearRel = typeProps.BlossomAtYearRel.ClampToRange((props.BlossomAtYearRel + parentPlantStack.Attributes?.GetFloat("blossomAtYearRelDiff")).GetValueOrDefault());
				props.LooseLeavesBelowTemp = typeProps.LooseLeavesBelowTemp.ClampToRange((props.LooseLeavesBelowTemp + parentPlantStack.Attributes?.GetFloat("looseLeavesBelowTempDiff")).GetValueOrDefault());
				props.RootSizeMul = typeProps.RootSizeMul.ClampToRange((props.RootSizeMul + parentPlantStack.Attributes?.GetFloat("rootSizeMulDiff")).GetValueOrDefault());
			}
		}
	}

	private void onRootTick(float dt)
	{
		double totalDays = Api.World.Calendar.TotalDays;
		if (totalDays - LastRootTickTotalDays < stateUpdateIntervalDays)
		{
			return;
		}
		int prevIntDays = -99;
		float temp = 0f;
		bool markDirty = false;
		ClimateCondition baseClimate = Api.World.BlockAccessor.GetClimateAt(be.Pos, EnumGetClimateMode.WorldGenValues);
		if (baseClimate == null)
		{
			return;
		}
		greenhouseTempBonus = getGreenhouseTempBonus();
		foreach (FruitTreeProperties value in propsByType.Values)
		{
			value.workingState = value.State;
		}
		while (totalDays - LastRootTickTotalDays >= stateUpdateIntervalDays)
		{
			int intDays = (int)LastRootTickTotalDays;
			foreach (KeyValuePair<string, FruitTreeProperties> item in propsByType)
			{
				FruitTreeProperties props = item.Value;
				if (props.workingState == EnumFruitTreeState.Dead)
				{
					continue;
				}
				if (prevIntDays != intDays)
				{
					double midday = (double)intDays + 0.5;
					temp = Api.World.BlockAccessor.GetClimateAt(be.Pos, baseClimate, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, midday).Temperature;
					temp = applyGreenhouseTempBonus(temp);
					prevIntDays = intDays;
				}
				if (props.DieBelowTemp > temp + (float)((props.workingState == EnumFruitTreeState.Dormant) ? 3 : 0))
				{
					props.workingState = EnumFruitTreeState.Dead;
					props.lastStateChangeTotalDays = Api.World.Calendar.TotalDays;
					markDirty = true;
					break;
				}
				switch (props.workingState)
				{
				case EnumFruitTreeState.Young:
					if (props.CycleType == EnumTreeCycleType.Evergreen)
					{
						if (LastRootTickTotalDays - TreePlantedTotalDays < nonFloweringYoungDays)
						{
							continue;
						}
						if (Math.Abs(LastRootTickTotalDays / (double)Api.World.Calendar.DaysPerYear % 1.0 - (double)props.BlossomAtYearRel) < 0.125)
						{
							props.workingState = EnumFruitTreeState.Flowering;
							props.lastStateChangeTotalDays = LastRootTickTotalDays;
							markDirty = true;
						}
					}
					else if (props.CycleType == EnumTreeCycleType.Deciduous && temp < props.EnterDormancyTemp)
					{
						props.workingState = EnumFruitTreeState.EnterDormancy;
						props.lastStateChangeTotalDays = LastRootTickTotalDays;
						markDirty = true;
					}
					break;
				case EnumFruitTreeState.Flowering:
					if (props.lastStateChangeTotalDays + (double)props.FloweringDays < LastRootTickTotalDays)
					{
						props.workingState = ((temp < props.EnterDormancyTemp) ? EnumFruitTreeState.Empty : EnumFruitTreeState.Fruiting);
						props.lastStateChangeTotalDays = LastRootTickTotalDays;
						markDirty = true;
					}
					break;
				case EnumFruitTreeState.Fruiting:
					if (props.lastStateChangeTotalDays + (double)props.FruitingDays < LastRootTickTotalDays)
					{
						props.workingState = EnumFruitTreeState.Ripe;
						props.lastStateChangeTotalDays = LastRootTickTotalDays;
						markDirty = true;
					}
					break;
				case EnumFruitTreeState.Ripe:
					if (props.lastStateChangeTotalDays + (double)props.RipeDays < LastRootTickTotalDays)
					{
						props.workingState = EnumFruitTreeState.Empty;
						props.lastStateChangeTotalDays = LastRootTickTotalDays;
						markDirty = true;
					}
					break;
				case EnumFruitTreeState.Empty:
					if (props.CycleType == EnumTreeCycleType.Evergreen)
					{
						if (Math.Abs(LastRootTickTotalDays / (double)Api.World.Calendar.DaysPerYear % 1.0 - (double)props.BlossomAtYearRel) < 0.125)
						{
							props.workingState = EnumFruitTreeState.Flowering;
							props.lastStateChangeTotalDays = LastRootTickTotalDays;
							markDirty = true;
						}
					}
					else if (props.CycleType == EnumTreeCycleType.Deciduous && temp < props.EnterDormancyTemp)
					{
						props.workingState = EnumFruitTreeState.EnterDormancy;
						props.lastStateChangeTotalDays = LastRootTickTotalDays;
						markDirty = true;
					}
					break;
				case EnumFruitTreeState.EnterDormancy:
					if (props.CycleType == EnumTreeCycleType.Deciduous && props.lastStateChangeTotalDays + 3.0 < LastRootTickTotalDays)
					{
						props.workingState = EnumFruitTreeState.Dormant;
						props.lastStateChangeTotalDays = LastRootTickTotalDays;
						markDirty = true;
					}
					break;
				case EnumFruitTreeState.Dormant:
					if (props.CycleType == EnumTreeCycleType.Deciduous)
					{
						updateVernalizedHours(props, temp);
						if (temp >= 20f || (temp > 15f && LastRootTickTotalDays - props.lastCheckAtTotalDays > 3.0))
						{
							props.workingState = EnumFruitTreeState.Empty;
							props.lastStateChangeTotalDays = LastRootTickTotalDays;
							markDirty = true;
						}
						else if (props.vernalizedHours > (double)props.VernalizationHours)
						{
							props.workingState = EnumFruitTreeState.DormantVernalized;
							props.lastStateChangeTotalDays = LastRootTickTotalDays;
							markDirty = true;
						}
					}
					break;
				case EnumFruitTreeState.DormantVernalized:
					if (temp >= 15f || (temp > 10f && LastRootTickTotalDays - props.lastCheckAtTotalDays > 3.0))
					{
						props.workingState = EnumFruitTreeState.Flowering;
						props.lastStateChangeTotalDays = LastRootTickTotalDays;
						markDirty = true;
					}
					break;
				}
				props.lastCheckAtTotalDays = LastRootTickTotalDays;
			}
			LastRootTickTotalDays += stateUpdateIntervalDays;
		}
		if (!markDirty)
		{
			return;
		}
		foreach (FruitTreeProperties value2 in propsByType.Values)
		{
			value2.State = value2.workingState;
		}
		Blockentity.MarkDirty(redrawOnClient: true);
	}

	public double GetCurrentStateProgress(string treeType)
	{
		if (Api == null)
		{
			return 0.0;
		}
		if (propsByType.TryGetValue(treeType, out var val))
		{
			switch (val.State)
			{
			case EnumFruitTreeState.Dormant:
				return 0.0;
			case EnumFruitTreeState.Flowering:
				return (Api.World.Calendar.TotalDays - val.lastStateChangeTotalDays) / (double)val.FloweringDays;
			case EnumFruitTreeState.Fruiting:
				return (Api.World.Calendar.TotalDays - val.lastStateChangeTotalDays) / (double)val.FruitingDays;
			case EnumFruitTreeState.Ripe:
				return (Api.World.Calendar.TotalDays - val.lastStateChangeTotalDays) / (double)val.RipeDays;
			case EnumFruitTreeState.Empty:
				return 0.0;
			}
		}
		return 0.0;
	}

	private void updateVernalizedHours(FruitTreeProperties props, float temp)
	{
		if (temp <= props.VernalizationTemp)
		{
			props.vernalizedHours += stateUpdateIntervalDays * (double)Api.World.Calendar.HoursPerDay;
		}
	}

	protected float getGreenhouseTempBonus()
	{
		if (Api.World.BlockAccessor.GetRainMapHeightAt(be.Pos) > be.Pos.Y)
		{
			Room room = roomreg?.GetRoomForPosition(be.Pos);
			if (((room != null && room.SkylightCount > room.NonSkylightCount && room.ExitCount == 0) ? 1 : 0) > 0)
			{
				return 5f;
			}
		}
		return 0f;
	}

	public float applyGreenhouseTempBonus(float temp)
	{
		return temp + greenhouseTempBonus;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		ITreeAttribute subtree = tree.GetTreeAttribute("dynproprs");
		if (subtree == null)
		{
			return;
		}
		foreach (KeyValuePair<string, IAttribute> val in subtree)
		{
			propsByType[val.Key] = new FruitTreeProperties();
			propsByType[val.Key].FromTreeAttributes(val.Value as ITreeAttribute);
		}
		LastRootTickTotalDays = tree.GetDouble("lastRootTickTotalDays");
		TreePlantedTotalDays = tree.GetDouble("treePlantedTotalDays");
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		TreeAttribute subtree = (TreeAttribute)(tree["dynproprs"] = new TreeAttribute());
		tree.SetDouble("lastRootTickTotalDays", LastRootTickTotalDays);
		tree.SetDouble("treePlantedTotalDays", TreePlantedTotalDays);
		foreach (KeyValuePair<string, FruitTreeProperties> val in propsByType)
		{
			TreeAttribute proptree = new TreeAttribute();
			val.Value.ToTreeAttributes(proptree);
			subtree[val.Key] = proptree;
		}
	}
}
