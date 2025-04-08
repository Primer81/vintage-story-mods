using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityBerryBush : BlockEntity, IAnimalFoodSource, IPointOfInterest
{
	private static Random rand = new Random();

	private const float intervalHours = 2f;

	private double lastCheckAtTotalDays;

	private double transitionHoursLeft = -1.0;

	private double? totalDaysForNextStageOld;

	private RoomRegistry roomreg;

	public int roomness;

	public bool Pruned;

	public double LastPrunedTotalDays;

	private float resetBelowTemperature;

	private float resetAboveTemperature;

	private float stopBelowTemperature;

	private float stopAboveTemperature;

	private float revertBlockBelowTemperature;

	private float revertBlockAboveTemperature;

	private float growthRateMul = 1f;

	public string[] creatureDietFoodTags;

	public Vec3d Position => Pos.ToVec3d().Add(0.5, 0.5, 0.5);

	public string Type => "food";

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		growthRateMul = (float)Api.World.Config.GetDecimal("cropGrowthRateMul", growthRateMul);
		if (api is ICoreServerAPI)
		{
			creatureDietFoodTags = base.Block.Attributes["foodTags"].AsArray<string>();
			if (transitionHoursLeft <= 0.0)
			{
				transitionHoursLeft = GetHoursForNextStage();
				lastCheckAtTotalDays = api.World.Calendar.TotalDays;
			}
			if (Api.World.Config.GetBool("processCrops", defaultValue: true))
			{
				RegisterGameTickListener(CheckGrow, 8000);
			}
			api.ModLoader.GetModSystem<POIRegistry>().AddPOI(this);
			roomreg = Api.ModLoader.GetModSystem<RoomRegistry>();
			if (totalDaysForNextStageOld.HasValue)
			{
				transitionHoursLeft = (totalDaysForNextStageOld.Value - Api.World.Calendar.TotalDays) * (double)Api.World.Calendar.HoursPerDay;
			}
		}
	}

	public void Prune()
	{
		Pruned = true;
		LastPrunedTotalDays = Api.World.Calendar.TotalDays;
		MarkDirty(redrawOnClient: true);
	}

	private void CheckGrow(float dt)
	{
		if (!(Api as ICoreServerAPI).World.IsFullyLoadedChunk(Pos))
		{
			return;
		}
		if (base.Block.Attributes == null)
		{
			UnregisterAllTickListeners();
			return;
		}
		lastCheckAtTotalDays = Math.Min(lastCheckAtTotalDays, Api.World.Calendar.TotalDays);
		LastPrunedTotalDays = Math.Min(LastPrunedTotalDays, Api.World.Calendar.TotalDays);
		double daysToCheck = GameMath.Mod(Api.World.Calendar.TotalDays - lastCheckAtTotalDays, Api.World.Calendar.DaysPerYear);
		float intervalDays = 2f / Api.World.Calendar.HoursPerDay;
		if (daysToCheck <= (double)intervalDays)
		{
			return;
		}
		if (Api.World.BlockAccessor.GetRainMapHeightAt(Pos) > Pos.Y)
		{
			Room room = roomreg?.GetRoomForPosition(Pos);
			roomness = ((room != null && room.SkylightCount > room.NonSkylightCount && room.ExitCount == 0) ? 1 : 0);
		}
		else
		{
			roomness = 0;
		}
		ClimateCondition conds = null;
		float baseTemperature = 0f;
		while (daysToCheck > (double)intervalDays)
		{
			daysToCheck -= (double)intervalDays;
			lastCheckAtTotalDays += intervalDays;
			transitionHoursLeft -= 2.0;
			if (conds == null)
			{
				conds = Api.World.BlockAccessor.GetClimateAt(Pos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, lastCheckAtTotalDays);
				if (conds == null)
				{
					return;
				}
				baseTemperature = conds.WorldGenTemperature;
			}
			else
			{
				conds.Temperature = baseTemperature;
				Api.World.BlockAccessor.GetClimateAt(Pos, conds, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, lastCheckAtTotalDays);
			}
			float temperature = conds.Temperature;
			if (roomness > 0)
			{
				temperature += 5f;
			}
			bool reset = temperature < resetBelowTemperature || temperature > resetAboveTemperature;
			if (temperature < stopBelowTemperature || temperature > stopAboveTemperature || reset)
			{
				if (!IsRipe())
				{
					transitionHoursLeft += 2.0;
				}
				if (reset)
				{
					bool num = temperature < revertBlockBelowTemperature || temperature > revertBlockAboveTemperature;
					if (!IsRipe())
					{
						transitionHoursLeft = GetHoursForNextStage();
					}
					if (num && base.Block.Variant["state"] != "empty")
					{
						Block nextBlock = Api.World.GetBlock(base.Block.CodeWithVariant("state", "empty"));
						Api.World.BlockAccessor.ExchangeBlock(nextBlock.BlockId, Pos);
					}
				}
			}
			else
			{
				if (Pruned && Api.World.Calendar.TotalDays - LastPrunedTotalDays > (double)Api.World.Calendar.DaysPerYear)
				{
					Pruned = false;
				}
				if (transitionHoursLeft <= 0.0 && !DoGrow())
				{
					return;
				}
			}
		}
		MarkDirty();
	}

	public override void OnExchanged(Block block)
	{
		base.OnExchanged(block);
		transitionHoursLeft = GetHoursForNextStage();
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Server)
		{
			UpdateTransitionsFromBlock();
		}
	}

	public override void CreateBehaviors(Block block, IWorldAccessor worldForResolve)
	{
		base.CreateBehaviors(block, worldForResolve);
		if (worldForResolve.Side == EnumAppSide.Server)
		{
			UpdateTransitionsFromBlock();
		}
	}

	protected void UpdateTransitionsFromBlock()
	{
		if (base.Block?.Attributes == null)
		{
			resetBelowTemperature = (stopBelowTemperature = (revertBlockBelowTemperature = -999f));
			resetAboveTemperature = (stopAboveTemperature = (revertBlockAboveTemperature = 999f));
			return;
		}
		resetBelowTemperature = base.Block.Attributes["resetBelowTemperature"].AsFloat(-999f);
		resetAboveTemperature = base.Block.Attributes["resetAboveTemperature"].AsFloat(999f);
		stopBelowTemperature = base.Block.Attributes["stopBelowTemperature"].AsFloat(-999f);
		stopAboveTemperature = base.Block.Attributes["stopAboveTemperature"].AsFloat(999f);
		revertBlockBelowTemperature = base.Block.Attributes["revertBlockBelowTemperature"].AsFloat(-999f);
		revertBlockAboveTemperature = base.Block.Attributes["revertBlockAboveTemperature"].AsFloat(999f);
	}

	public double GetHoursForNextStage()
	{
		if (IsRipe())
		{
			return 4.0 * (5.0 + rand.NextDouble()) * 1.6 * (double)Api.World.Calendar.HoursPerDay;
		}
		return (5.0 + rand.NextDouble()) * 1.6 * (double)Api.World.Calendar.HoursPerDay / (double)growthRateMul;
	}

	public bool IsRipe()
	{
		return base.Block.Variant["state"] == "ripe";
	}

	private bool DoGrow()
	{
		string nowCodePart = base.Block.Variant["state"];
		string nextCodePart = ((nowCodePart == "empty") ? "flowering" : ((nowCodePart == "flowering") ? "ripe" : "empty"));
		AssetLocation loc = base.Block.CodeWithVariant("state", nextCodePart);
		if (!loc.Valid)
		{
			Api.World.BlockAccessor.RemoveBlockEntity(Pos);
			return false;
		}
		Block nextBlock = Api.World.GetBlock(loc);
		if (nextBlock?.Code == null)
		{
			return false;
		}
		Api.World.BlockAccessor.ExchangeBlock(nextBlock.BlockId, Pos);
		MarkDirty(redrawOnClient: true);
		return true;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		transitionHoursLeft = tree.GetDouble("transitionHoursLeft");
		if (tree.HasAttribute("totalDaysForNextStage"))
		{
			totalDaysForNextStageOld = tree.GetDouble("totalDaysForNextStage");
		}
		lastCheckAtTotalDays = tree.GetDouble("lastCheckAtTotalDays");
		roomness = tree.GetInt("roomness");
		Pruned = tree.GetBool("pruned");
		LastPrunedTotalDays = tree.GetDecimal("lastPrunedTotalDays");
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetDouble("transitionHoursLeft", transitionHoursLeft);
		tree.SetDouble("lastCheckAtTotalDays", lastCheckAtTotalDays);
		tree.SetBool("pruned", Pruned);
		tree.SetInt("roomness", roomness);
		tree.SetDouble("lastPrunedTotalDays", LastPrunedTotalDays);
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
	{
		double daysleft = transitionHoursLeft / (double)Api.World.Calendar.HoursPerDay;
		if (!IsRipe())
		{
			string code = ((base.Block.Variant["state"] == "empty") ? "flowering" : "ripen");
			if (daysleft < 1.0)
			{
				sb.AppendLine(Lang.Get("berrybush-" + code + "-1day"));
			}
			else
			{
				sb.AppendLine(Lang.Get("berrybush-" + code + "-xdays", (int)daysleft));
			}
			if (roomness > 0)
			{
				sb.AppendLine(Lang.Get("greenhousetempbonus"));
			}
		}
	}

	public bool IsSuitableFor(Entity entity, CreatureDiet diet)
	{
		if (diet == null)
		{
			return false;
		}
		if (!IsRipe())
		{
			return false;
		}
		return diet.Matches(EnumFoodCategory.NoNutrition, creatureDietFoodTags);
	}

	public float ConsumeOnePortion(Entity entity)
	{
		AssetLocation loc = base.Block.CodeWithVariant("state", "empty");
		if (!loc.Valid)
		{
			Api.World.BlockAccessor.RemoveBlockEntity(Pos);
			return 0f;
		}
		Block nextBlock = Api.World.GetBlock(loc);
		if (nextBlock?.Code == null)
		{
			return 0f;
		}
		BlockBehaviorHarvestable bbh = base.Block.GetBehavior<BlockBehaviorHarvestable>();
		bbh?.harvestedStacks?.Foreach(delegate(BlockDropItemStack harvestedStack)
		{
			Api.World.SpawnItemEntity(harvestedStack?.GetNextItemStack(), Pos);
		});
		Api.World.PlaySoundAt(bbh?.harvestingSound, Pos, 0.0);
		Api.World.BlockAccessor.ExchangeBlock(nextBlock.BlockId, Pos);
		MarkDirty(redrawOnClient: true);
		return 0.1f;
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		if (Api.Side == EnumAppSide.Server)
		{
			Api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this);
		}
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Server)
		{
			Api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this);
		}
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (Pruned)
		{
			mesher.AddMeshData((base.Block as BlockBerryBush).GetPrunedMesh(Pos));
			return true;
		}
		return base.OnTesselation(mesher, tessThreadTesselator);
	}
}
