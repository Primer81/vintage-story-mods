using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityTransient : BlockEntity
{
	private double lastCheckAtTotalDays;

	private double transitionHoursLeft = -1.0;

	private TransientProperties props;

	private long listenerId;

	private double? transitionAtTotalDaysOld;

	public string ConvertToOverride;

	public virtual int CheckIntervalMs { get; set; } = 2000;


	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		JsonObject attributes = base.Block.Attributes;
		if (attributes == null || !attributes["transientProps"].Exists)
		{
			return;
		}
		if (api.Side == EnumAppSide.Server)
		{
			Block block = Api.World.BlockAccessor.GetBlock(Pos, 1);
			if (block.Id != base.Block.Id)
			{
				if (!(block.EntityClass == base.Block.EntityClass))
				{
					Api.World.Logger.Warning("BETransient @{0} for Block {1}, but there is {2} at this position? Will delete BE", Pos, base.Block.Code.ToShortString(), block.Code.ToShortString());
					api.Event.EnqueueMainThreadTask(delegate
					{
						api.World.BlockAccessor.RemoveBlockEntity(Pos);
					}, "delete betransient");
					return;
				}
				if (!(block.Code.FirstCodePart() == base.Block.Code.FirstCodePart()))
				{
					Api.World.Logger.Warning("BETransient @{0} for Block {1}, but there is {2} at this position? Will delete BE and attempt to recreate it", Pos, base.Block.Code.ToShortString(), block.Code.ToShortString());
					api.Event.EnqueueMainThreadTask(delegate
					{
						api.World.BlockAccessor.RemoveBlockEntity(Pos);
						Block block2 = api.World.BlockAccessor.GetBlock(Pos, 1);
						api.World.BlockAccessor.SetBlock(block2.Id, Pos, 1);
					}, "delete betransient");
					return;
				}
				base.Block = block;
			}
		}
		props = base.Block.Attributes["transientProps"].AsObject<TransientProperties>();
		if (props == null)
		{
			return;
		}
		if (transitionHoursLeft <= 0.0)
		{
			transitionHoursLeft = props.InGameHours;
		}
		if (api.Side == EnumAppSide.Server)
		{
			if (listenerId != 0L)
			{
				throw new InvalidOperationException("Initializing BETransient twice would create a memory and performance leak");
			}
			listenerId = RegisterGameTickListener(CheckTransition, CheckIntervalMs);
			if (transitionAtTotalDaysOld.HasValue)
			{
				lastCheckAtTotalDays = Api.World.Calendar.TotalDays;
				transitionHoursLeft = (transitionAtTotalDaysOld.Value - lastCheckAtTotalDays) * (double)Api.World.Calendar.HoursPerDay;
			}
		}
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		lastCheckAtTotalDays = Api.World.Calendar.TotalDays;
	}

	public virtual void CheckTransition(float dt)
	{
		if (Api.World.BlockAccessor.GetBlock(Pos).Attributes == null)
		{
			Api.World.Logger.Error("BETransient @{0}: cannot find block attributes for {1}. Will stop transient timer", Pos, base.Block.Code.ToShortString());
			UnregisterGameTickListener(listenerId);
			return;
		}
		lastCheckAtTotalDays = Math.Min(lastCheckAtTotalDays, Api.World.Calendar.TotalDays);
		ClimateCondition baseClimate = Api.World.BlockAccessor.GetClimateAt(Pos, EnumGetClimateMode.WorldGenValues);
		if (baseClimate == null)
		{
			return;
		}
		float baseTemperature = baseClimate.Temperature;
		float oneHour = 1f / Api.World.Calendar.HoursPerDay;
		double timeNow = Api.World.Calendar.TotalDays;
		while (timeNow - lastCheckAtTotalDays > (double)oneHour)
		{
			lastCheckAtTotalDays += oneHour;
			transitionHoursLeft -= 1.0;
			baseClimate.Temperature = baseTemperature;
			ClimateCondition conds = Api.World.BlockAccessor.GetClimateAt(Pos, baseClimate, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, lastCheckAtTotalDays);
			if (props.Condition == EnumTransientCondition.Temperature)
			{
				if (conds.Temperature < props.WhenBelowTemperature || conds.Temperature > props.WhenAboveTemperature)
				{
					tryTransition(props.ConvertTo);
				}
				continue;
			}
			bool reset = conds.Temperature < props.ResetBelowTemperature;
			if (conds.Temperature < props.StopBelowTemperature || reset)
			{
				transitionHoursLeft += 1.0;
				if (reset)
				{
					transitionHoursLeft = props.InGameHours;
				}
			}
			else if (transitionHoursLeft <= 0.0)
			{
				tryTransition(ConvertToOverride ?? props.ConvertTo);
				break;
			}
		}
	}

	public void tryTransition(string toCode)
	{
		Block block = Api.World.BlockAccessor.GetBlock(Pos);
		if (block.Attributes == null)
		{
			return;
		}
		string fromCode = props.ConvertFrom;
		if (fromCode != null && toCode != null)
		{
			if (fromCode.IndexOf(':') == -1)
			{
				fromCode = block.Code.Domain + ":" + fromCode;
			}
			if (toCode.IndexOf(':') == -1)
			{
				toCode = block.Code.Domain + ":" + toCode;
			}
			AssetLocation blockCode = ((fromCode != null && toCode.Contains('*')) ? block.Code.WildCardReplace(new AssetLocation(fromCode), new AssetLocation(toCode)) : new AssetLocation(toCode));
			Block tblock = Api.World.GetBlock(blockCode);
			if (tblock != null)
			{
				Api.World.BlockAccessor.SetBlock(tblock.BlockId, Pos, 1);
			}
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		transitionHoursLeft = tree.GetDouble("transitionHoursLeft");
		if (tree.HasAttribute("transitionAtTotalDays"))
		{
			transitionAtTotalDaysOld = tree.GetDouble("transitionAtTotalDays");
		}
		lastCheckAtTotalDays = tree.GetDouble("lastCheckAtTotalDays");
		ConvertToOverride = tree.GetString("convertToOverride");
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetDouble("transitionHoursLeft", transitionHoursLeft);
		tree.SetDouble("lastCheckAtTotalDays", lastCheckAtTotalDays);
		if (ConvertToOverride != null)
		{
			tree.SetString("convertToOverride", ConvertToOverride);
		}
	}

	public void SetPlaceTime(double totalHours)
	{
		float hours = props.InGameHours;
		transitionHoursLeft = (double)hours + totalHours - Api.World.Calendar.TotalHours;
	}

	public bool IsDueTransition()
	{
		return transitionHoursLeft <= 0.0;
	}
}
