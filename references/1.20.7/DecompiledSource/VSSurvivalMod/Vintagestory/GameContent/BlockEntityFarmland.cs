using System;
using System.Collections.Generic;
using System.Linq;
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

public class BlockEntityFarmland : BlockEntity, IFarmlandBlockEntity, IAnimalFoodSource, IPointOfInterest, ITexPositionSource
{
	protected enum EnumWaterSearchResult
	{
		Found,
		NotFound,
		Deferred
	}

	protected static Random rand = new Random();

	public static OrderedDictionary<string, float> Fertilities = new OrderedDictionary<string, float>
	{
		{ "verylow", 5f },
		{ "low", 25f },
		{ "medium", 50f },
		{ "compost", 65f },
		{ "high", 80f }
	};

	protected HashSet<string> PermaBoosts = new HashSet<string>();

	protected float totalHoursWaterRetention;

	protected BlockPos upPos;

	protected double totalHoursForNextStage;

	protected double totalHoursLastUpdate;

	protected float[] nutrients = new float[3];

	protected float[] slowReleaseNutrients = new float[3];

	protected Dictionary<string, float> fertilizerOverlayStrength;

	protected float moistureLevel;

	protected double lastWaterSearchedTotalHours;

	protected TreeAttribute cropAttrs = new TreeAttribute();

	public int[] originalFertility = new int[3];

	protected bool unripeCropColdDamaged;

	protected bool unripeHeatDamaged;

	protected bool ripeCropColdDamaged;

	protected bool saltExposed;

	protected float[] damageAccum = new float[Enum.GetValues(typeof(EnumCropStressType)).Length];

	private BlockFarmland blockFarmland;

	protected Vec3d tmpPos = new Vec3d();

	protected float lastWaterDistance = 99f;

	protected double lastMoistureLevelUpdateTotalDays;

	public int roomness;

	protected bool allowundergroundfarming;

	protected bool allowcropDeath;

	protected float fertilityRecoverySpeed = 0.25f;

	protected float growthRateMul = 1f;

	protected MeshData fertilizerQuad;

	protected TextureAtlasPosition fertilizerTexturePos;

	private ICoreClientAPI capi;

	private string[] creatureFoodTags;

	private bool farmlandIsAtChunkEdge;

	public bool IsVisiblyMoist => (double)moistureLevel > 0.1;

	public double TotalHoursForNextStage => totalHoursForNextStage;

	public double TotalHoursFertilityCheck => totalHoursLastUpdate;

	public float[] Nutrients => nutrients;

	public float MoistureLevel => moistureLevel;

	public int[] OriginalFertility => originalFertility;

	public BlockPos UpPos => upPos;

	public ITreeAttribute CropAttributes => cropAttrs;

	public Vec3d Position => Pos.ToVec3d().Add(0.5, 1.0, 0.5);

	public string Type => "food";

	BlockPos IFarmlandBlockEntity.Pos => Pos;

	public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

	public TextureAtlasPosition this[string textureCode] => fertilizerTexturePos;

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		blockFarmland = base.Block as BlockFarmland;
		if (blockFarmland == null)
		{
			return;
		}
		capi = api as ICoreClientAPI;
		totalHoursWaterRetention = Api.World.Calendar.HoursPerDay * 4f;
		upPos = Pos.UpCopy();
		allowundergroundfarming = Api.World.Config.GetBool("allowUndergroundFarming");
		allowcropDeath = Api.World.Config.GetBool("allowCropDeath", defaultValue: true);
		fertilityRecoverySpeed = Api.World.Config.GetFloat("fertilityRecoverySpeed", fertilityRecoverySpeed);
		growthRateMul = (float)Api.World.Config.GetDecimal("cropGrowthRateMul", growthRateMul);
		creatureFoodTags = base.Block.Attributes["foodTags"].AsArray<string>();
		if (api is ICoreServerAPI)
		{
			if (Api.World.Config.GetBool("processCrops", defaultValue: true))
			{
				RegisterGameTickListener(Update, 3300 + rand.Next(400));
			}
			api.ModLoader.GetModSystem<POIRegistry>().AddPOI(this);
		}
		updateFertilizerQuad();
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		base.OnBlockPlaced(byItemStack);
	}

	public void OnCreatedFromSoil(Block block)
	{
		string fertility = block.LastCodePart(1);
		if (block is BlockFarmland)
		{
			fertility = block.LastCodePart();
		}
		originalFertility[0] = (int)Fertilities[fertility];
		originalFertility[1] = (int)Fertilities[fertility];
		originalFertility[2] = (int)Fertilities[fertility];
		nutrients[0] = originalFertility[0];
		nutrients[1] = originalFertility[1];
		nutrients[2] = originalFertility[2];
		totalHoursLastUpdate = Api.World.Calendar.TotalHours;
		tryUpdateMoistureLevel(Api.World.Calendar.TotalDays, searchNearbyWater: true);
	}

	public bool OnBlockInteract(IPlayer byPlayer)
	{
		ItemStack stack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
		JsonObject obj = stack?.Collectible?.Attributes?["fertilizerProps"];
		if (obj == null || !obj.Exists)
		{
			return false;
		}
		FertilizerProps props = obj.AsObject<FertilizerProps>();
		if (props == null)
		{
			return false;
		}
		float nAdd = Math.Min(Math.Max(0f, 150f - slowReleaseNutrients[0]), props.N);
		float pAdd = Math.Min(Math.Max(0f, 150f - slowReleaseNutrients[1]), props.P);
		float kAdd = Math.Min(Math.Max(0f, 150f - slowReleaseNutrients[2]), props.K);
		slowReleaseNutrients[0] += nAdd;
		slowReleaseNutrients[1] += pAdd;
		slowReleaseNutrients[2] += kAdd;
		if (props.PermaBoost != null && !PermaBoosts.Contains(props.PermaBoost.Code))
		{
			originalFertility[0] += props.PermaBoost.N;
			originalFertility[1] += props.PermaBoost.P;
			originalFertility[2] += props.PermaBoost.K;
			PermaBoosts.Add(props.PermaBoost.Code);
		}
		string fertCode = stack.Collectible.Attributes["fertilizerTextureCode"].AsString();
		if (fertCode != null)
		{
			if (fertilizerOverlayStrength == null)
			{
				fertilizerOverlayStrength = new Dictionary<string, float>();
			}
			fertilizerOverlayStrength.TryGetValue(fertCode, out var prevValue);
			fertilizerOverlayStrength[fertCode] = prevValue + Math.Max(nAdd, Math.Max(kAdd, pAdd));
		}
		updateFertilizerQuad();
		byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(1);
		byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
		(byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
		Api.World.PlaySoundAt(Api.World.BlockAccessor.GetBlock(Pos).Sounds.Hit, (double)Pos.X + 0.5, (double)Pos.InternalY + 0.75, (double)Pos.Z + 0.5, byPlayer, randomizePitch: true, 12f);
		MarkDirty();
		return true;
	}

	public void OnCropBlockBroken()
	{
		ripeCropColdDamaged = false;
		unripeCropColdDamaged = false;
		unripeHeatDamaged = false;
		for (int i = 0; i < damageAccum.Length; i++)
		{
			damageAccum[i] = 0f;
		}
		MarkDirty(redrawOnClient: true);
	}

	public ItemStack[] GetDrops(ItemStack[] drops)
	{
		BlockEntityDeadCrop beDeadCrop = Api.World.BlockAccessor.GetBlockEntity(upPos) as BlockEntityDeadCrop;
		bool isDead = beDeadCrop != null;
		if (!ripeCropColdDamaged && !unripeCropColdDamaged && !unripeHeatDamaged && !isDead)
		{
			return drops;
		}
		if (!Api.World.Config.GetString("harshWinters").ToBool(defaultValue: true))
		{
			return drops;
		}
		List<ItemStack> stacks = new List<ItemStack>();
		BlockCropProperties cropProps = GetCrop()?.CropProps;
		if (cropProps == null)
		{
			return drops;
		}
		float mul = 1f;
		if (ripeCropColdDamaged)
		{
			mul = cropProps.ColdDamageRipeMul;
		}
		if (unripeHeatDamaged || unripeCropColdDamaged)
		{
			mul = cropProps.DamageGrowthStuntMul;
		}
		if (isDead)
		{
			mul = ((beDeadCrop.deathReason == EnumCropStressType.Eaten) ? 0f : Math.Max(cropProps.ColdDamageRipeMul, cropProps.DamageGrowthStuntMul));
		}
		string[] debuffUnaffectedDrops = base.Block.Attributes?["debuffUnaffectedDrops"].AsArray<string>();
		foreach (ItemStack stack in drops)
		{
			if (WildcardUtil.Match(debuffUnaffectedDrops, stack.Collectible.Code.ToShortString()))
			{
				stacks.Add(stack);
				continue;
			}
			float q = (float)stack.StackSize * mul;
			float frac = q - (float)(int)q;
			stack.StackSize = (int)q + ((Api.World.Rand.NextDouble() > (double)frac) ? 1 : 0);
			if (stack.StackSize > 0)
			{
				stacks.Add(stack);
			}
		}
		MarkDirty(redrawOnClient: true);
		return stacks.ToArray();
	}

	protected float GetNearbyWaterDistance(out EnumWaterSearchResult result, float hoursPassed)
	{
		float waterDistance = 99f;
		farmlandIsAtChunkEdge = false;
		bool saltWater = false;
		Api.World.BlockAccessor.SearchFluidBlocks(new BlockPos(Pos.X - 4, Pos.Y, Pos.Z - 4), new BlockPos(Pos.X + 4, Pos.Y, Pos.Z + 4), delegate(Block block, BlockPos pos)
		{
			if (block.LiquidCode == "water")
			{
				waterDistance = Math.Min(waterDistance, Math.Max(Math.Abs(pos.X - Pos.X), Math.Abs(pos.Z - Pos.Z)));
			}
			if (block.LiquidCode == "saltwater")
			{
				saltWater = true;
			}
			return true;
		}, delegate
		{
			farmlandIsAtChunkEdge = true;
		});
		if (saltWater)
		{
			damageAccum[4] += hoursPassed;
		}
		result = EnumWaterSearchResult.Deferred;
		if (farmlandIsAtChunkEdge)
		{
			return 99f;
		}
		lastWaterSearchedTotalHours = Api.World.Calendar.TotalHours;
		if (waterDistance < 4f)
		{
			result = EnumWaterSearchResult.Found;
			return waterDistance;
		}
		result = EnumWaterSearchResult.NotFound;
		return 99f;
	}

	private bool tryUpdateMoistureLevel(double totalDays, bool searchNearbyWater)
	{
		float dist = 99f;
		if (searchNearbyWater)
		{
			dist = GetNearbyWaterDistance(out var res, 0f);
			switch (res)
			{
			case EnumWaterSearchResult.Deferred:
				return false;
			default:
				dist = 99f;
				break;
			case EnumWaterSearchResult.Found:
				break;
			}
			lastWaterDistance = dist;
		}
		if (updateMoistureLevel(totalDays, dist))
		{
			UpdateFarmlandBlock();
		}
		return true;
	}

	private bool updateMoistureLevel(double totalDays, float waterDistance)
	{
		bool skyExposed = Api.World.BlockAccessor.GetRainMapHeightAt(Pos.X, Pos.Z) <= ((GetCrop() == null) ? Pos.Y : (Pos.Y + 1));
		return updateMoistureLevel(totalDays, waterDistance, skyExposed);
	}

	private bool updateMoistureLevel(double totalDays, float waterDistance, bool skyExposed, ClimateCondition baseClimate = null)
	{
		tmpPos.Set((double)Pos.X + 0.5, (double)Pos.Y + 0.5, (double)Pos.Z + 0.5);
		float minMoisture = GameMath.Clamp(1f - waterDistance / 4f, 0f, 1f);
		if (lastMoistureLevelUpdateTotalDays > Api.World.Calendar.TotalDays)
		{
			lastMoistureLevelUpdateTotalDays = Api.World.Calendar.TotalDays;
			return false;
		}
		double hoursPassed = Math.Min((totalDays - lastMoistureLevelUpdateTotalDays) * (double)Api.World.Calendar.HoursPerDay, totalHoursWaterRetention);
		if (hoursPassed < 0.029999999329447746)
		{
			moistureLevel = Math.Max(moistureLevel, minMoisture);
			return false;
		}
		moistureLevel = Math.Max(minMoisture, moistureLevel - (float)hoursPassed / totalHoursWaterRetention);
		if (skyExposed)
		{
			if (baseClimate == null && hoursPassed > 0.0)
			{
				baseClimate = Api.World.BlockAccessor.GetClimateAt(Pos, EnumGetClimateMode.WorldGenValues, totalDays - hoursPassed / (double)Api.World.Calendar.HoursPerDay / 2.0);
			}
			while (hoursPassed > 0.0)
			{
				double rainLevel = blockFarmland.wsys.GetPrecipitation(Pos, totalDays - hoursPassed / (double)Api.World.Calendar.HoursPerDay, baseClimate);
				moistureLevel = GameMath.Clamp(moistureLevel + (float)rainLevel / 3f, 0f, 1f);
				hoursPassed -= 1.0;
			}
		}
		lastMoistureLevelUpdateTotalDays = totalDays;
		return true;
	}

	private void Update(float dt)
	{
		if (!(Api as ICoreServerAPI).World.IsFullyLoadedChunk(Pos))
		{
			return;
		}
		double nowTotalHours = Api.World.Calendar.TotalHours;
		double hourIntervall = 3.0 + rand.NextDouble();
		Block cropBlock = GetCrop();
		bool hasCrop = cropBlock != null;
		bool skyExposed = Api.World.BlockAccessor.GetRainMapHeightAt(Pos.X, Pos.Z) <= (hasCrop ? (Pos.Y + 1) : Pos.Y);
		if (nowTotalHours - totalHoursLastUpdate < hourIntervall)
		{
			if (!(totalHoursLastUpdate > nowTotalHours))
			{
				if (updateMoistureLevel(nowTotalHours / (double)Api.World.Calendar.HoursPerDay, lastWaterDistance, skyExposed))
				{
					UpdateFarmlandBlock();
				}
				return;
			}
			double rollback = totalHoursLastUpdate - nowTotalHours;
			totalHoursForNextStage -= rollback;
			lastMoistureLevelUpdateTotalDays -= rollback;
			lastWaterSearchedTotalHours -= rollback;
			totalHoursLastUpdate = nowTotalHours;
		}
		int lightpenalty = 0;
		if (!allowundergroundfarming)
		{
			lightpenalty = Math.Max(0, Api.World.SeaLevel - Pos.Y);
		}
		int sunlight = Api.World.BlockAccessor.GetLightLevel(upPos, EnumLightLevelType.MaxLight);
		double lightGrowthSpeedFactor = GameMath.Clamp(1f - (float)(blockFarmland.DelayGrowthBelowSunLight - sunlight - lightpenalty) * blockFarmland.LossPerLevel, 0f, 1f);
		Block upblock = Api.World.BlockAccessor.GetBlock(upPos);
		Block deadCropBlock = Api.World.GetBlock(new AssetLocation("deadcrop"));
		double hoursNextStage = GetHoursForNextStage();
		double lightHoursPenalty = hoursNextStage / lightGrowthSpeedFactor - hoursNextStage;
		double totalHoursNextGrowthState = totalHoursForNextStage + lightHoursPenalty;
		EnumSoilNutrient? currentlyConsumedNutrient = null;
		if (upblock.CropProps != null)
		{
			currentlyConsumedNutrient = upblock.CropProps.RequiredNutrient;
		}
		bool growTallGrass = false;
		float[] npkRegain = new float[3];
		float waterDistance = 99f;
		totalHoursLastUpdate = Math.Max(totalHoursLastUpdate, nowTotalHours - (double)((float)Api.World.Calendar.DaysPerYear * Api.World.Calendar.HoursPerDay));
		bool hasRipeCrop = HasRipeCrop();
		if (!skyExposed)
		{
			Room room = blockFarmland.roomreg?.GetRoomForPosition(upPos);
			roomness = ((room != null && room.SkylightCount > room.NonSkylightCount && room.ExitCount == 0) ? 1 : 0);
		}
		else
		{
			roomness = 0;
		}
		bool nearbyWaterTested = false;
		ClimateCondition conds = null;
		while (nowTotalHours - totalHoursLastUpdate > hourIntervall)
		{
			if (!nearbyWaterTested)
			{
				waterDistance = GetNearbyWaterDistance(out var res, (float)hourIntervall);
				switch (res)
				{
				case EnumWaterSearchResult.Deferred:
					return;
				case EnumWaterSearchResult.NotFound:
					waterDistance = 99f;
					break;
				}
				nearbyWaterTested = true;
				lastWaterDistance = waterDistance;
			}
			totalHoursLastUpdate += hourIntervall;
			hourIntervall = 3.0 + rand.NextDouble();
			if (conds == null)
			{
				conds = Api.World.BlockAccessor.GetClimateAt(Pos, EnumGetClimateMode.ForSuppliedDate_TemperatureRainfallOnly, totalHoursLastUpdate / (double)Api.World.Calendar.HoursPerDay);
				if (conds == null)
				{
					break;
				}
			}
			else
			{
				Api.World.BlockAccessor.GetClimateAt(Pos, conds, EnumGetClimateMode.ForSuppliedDate_TemperatureRainfallOnly, totalHoursLastUpdate / (double)Api.World.Calendar.HoursPerDay);
			}
			updateMoistureLevel(totalHoursLastUpdate / (double)Api.World.Calendar.HoursPerDay, waterDistance, skyExposed, conds);
			if (roomness > 0)
			{
				conds.Temperature += 5f;
			}
			if (!hasCrop)
			{
				ripeCropColdDamaged = false;
				unripeCropColdDamaged = false;
				unripeHeatDamaged = false;
				for (int k = 0; k < damageAccum.Length; k++)
				{
					damageAccum[k] = 0f;
				}
			}
			else
			{
				if (cropBlock?.CropProps != null && conds.Temperature < cropBlock.CropProps.ColdDamageBelow)
				{
					if (hasRipeCrop)
					{
						ripeCropColdDamaged = true;
					}
					else
					{
						unripeCropColdDamaged = true;
						damageAccum[2] += (float)hourIntervall;
					}
				}
				else
				{
					damageAccum[2] = Math.Max(0f, damageAccum[2] - (float)hourIntervall / 10f);
				}
				if (cropBlock?.CropProps != null && conds.Temperature > cropBlock.CropProps.HeatDamageAbove && hasCrop)
				{
					unripeHeatDamaged = true;
					damageAccum[1] += (float)hourIntervall;
				}
				else
				{
					damageAccum[1] = Math.Max(0f, damageAccum[1] - (float)hourIntervall / 10f);
				}
				for (int l = 0; l < damageAccum.Length; l++)
				{
					float dmg = damageAccum[l];
					if (!allowcropDeath)
					{
						dmg = (damageAccum[l] = 0f);
					}
					if (dmg > 48f)
					{
						Api.World.BlockAccessor.SetBlock(deadCropBlock.Id, upPos);
						BlockEntityDeadCrop obj = Api.World.BlockAccessor.GetBlockEntity(upPos) as BlockEntityDeadCrop;
						obj.Inventory[0].Itemstack = new ItemStack(cropBlock);
						obj.deathReason = (EnumCropStressType)l;
						hasCrop = false;
						break;
					}
				}
			}
			float growthChance = GameMath.Clamp(conds.Temperature / 10f, 0f, 10f);
			if (rand.NextDouble() > (double)growthChance)
			{
				continue;
			}
			growTallGrass |= rand.NextDouble() < 0.006;
			npkRegain[0] = (hasRipeCrop ? 0f : fertilityRecoverySpeed);
			npkRegain[1] = (hasRipeCrop ? 0f : fertilityRecoverySpeed);
			npkRegain[2] = (hasRipeCrop ? 0f : fertilityRecoverySpeed);
			if (currentlyConsumedNutrient.HasValue)
			{
				npkRegain[(int)currentlyConsumedNutrient.Value] /= 3f;
			}
			for (int j = 0; j < 3; j++)
			{
				nutrients[j] += Math.Max(0f, npkRegain[j] + Math.Min(0f, (float)originalFertility[j] - nutrients[j] - npkRegain[j]));
				if (slowReleaseNutrients[j] > 0f)
				{
					float release = Math.Min(0.25f, slowReleaseNutrients[j]);
					nutrients[j] = Math.Min(100f, nutrients[j] + release);
					slowReleaseNutrients[j] = Math.Max(0f, slowReleaseNutrients[j] - release);
				}
				else if (nutrients[j] > (float)originalFertility[j])
				{
					nutrients[j] = Math.Max(originalFertility[j], nutrients[j] - 0.05f);
				}
			}
			if (fertilizerOverlayStrength != null && fertilizerOverlayStrength.Count > 0)
			{
				string[] array = fertilizerOverlayStrength.Keys.ToArray();
				foreach (string code in array)
				{
					float newStr = fertilizerOverlayStrength[code] - fertilityRecoverySpeed;
					if (newStr < 0f)
					{
						fertilizerOverlayStrength.Remove(code);
					}
					else
					{
						fertilizerOverlayStrength[code] = newStr;
					}
				}
			}
			if (!((double)moistureLevel < 0.1) && totalHoursNextGrowthState <= totalHoursLastUpdate)
			{
				TryGrowCrop(totalHoursForNextStage);
				hasRipeCrop = HasRipeCrop();
				totalHoursForNextStage += hoursNextStage;
				totalHoursNextGrowthState = totalHoursForNextStage + lightHoursPenalty;
				hoursNextStage = GetHoursForNextStage();
			}
		}
		if (growTallGrass && upblock.BlockMaterial == EnumBlockMaterial.Air)
		{
			double rnd = rand.NextDouble() * (double)blockFarmland.TotalWeedChance;
			for (int i = 0; i < blockFarmland.WeedNames.Length; i++)
			{
				rnd -= (double)blockFarmland.WeedNames[i].Chance;
				if (rnd <= 0.0)
				{
					Block weedsBlock = Api.World.GetBlock(blockFarmland.WeedNames[i].Code);
					if (weedsBlock != null)
					{
						Api.World.BlockAccessor.SetBlock(weedsBlock.BlockId, upPos);
					}
					break;
				}
			}
		}
		updateFertilizerQuad();
		UpdateFarmlandBlock();
		Api.World.BlockAccessor.MarkBlockEntityDirty(Pos);
	}

	public double GetHoursForNextStage()
	{
		Block block = GetCrop();
		if (block == null)
		{
			return 99999999.0;
		}
		float totalDays = block.CropProps.TotalGrowthDays;
		totalDays = ((!(totalDays > 0f)) ? (block.CropProps.TotalGrowthMonths * (float)Api.World.Calendar.DaysPerMonth) : (totalDays / 12f * (float)Api.World.Calendar.DaysPerMonth));
		return Api.World.Calendar.HoursPerDay * totalDays / (float)block.CropProps.GrowthStages * (1f / GetGrowthRate(block.CropProps.RequiredNutrient)) * (float)(0.9 + 0.2 * rand.NextDouble()) / growthRateMul;
	}

	public float GetGrowthRate(EnumSoilNutrient nutrient)
	{
		float moistFactor = (float)Math.Pow(Math.Max(0.01, (double)(moistureLevel * 100f / 70f) - 0.143), 0.35);
		if (nutrients[(int)nutrient] > 75f)
		{
			return moistFactor * 1.1f;
		}
		if (nutrients[(int)nutrient] > 50f)
		{
			return moistFactor * 1f;
		}
		if (nutrients[(int)nutrient] > 35f)
		{
			return moistFactor * 0.9f;
		}
		if (nutrients[(int)nutrient] > 20f)
		{
			return moistFactor * 0.6f;
		}
		if (nutrients[(int)nutrient] > 5f)
		{
			return moistFactor * 0.3f;
		}
		return moistFactor * 0.1f;
	}

	public float GetGrowthRate()
	{
		BlockCropProperties cropProps = GetCrop()?.CropProps;
		if (cropProps != null)
		{
			return GetGrowthRate(cropProps.RequiredNutrient);
		}
		return 1f;
	}

	public float GetDeathChance(int nutrientIndex)
	{
		if (nutrients[nutrientIndex] <= 5f)
		{
			return 0.5f;
		}
		return 0f;
	}

	public bool TryPlant(Block block)
	{
		if (CanPlant() && block.CropProps != null)
		{
			Api.World.BlockAccessor.SetBlock(block.BlockId, upPos);
			totalHoursForNextStage = Api.World.Calendar.TotalHours + GetHoursForNextStage();
			CropBehavior[] behaviors = block.CropProps.Behaviors;
			for (int i = 0; i < behaviors.Length; i++)
			{
				behaviors[i].OnPlanted(Api);
			}
			return true;
		}
		return false;
	}

	public bool CanPlant()
	{
		Block block = Api.World.BlockAccessor.GetBlock(upPos);
		if (block != null)
		{
			return block.BlockMaterial == EnumBlockMaterial.Air;
		}
		return true;
	}

	public bool HasUnripeCrop()
	{
		Block block = GetCrop();
		if (block != null)
		{
			return GetCropStage(block) < block.CropProps.GrowthStages;
		}
		return false;
	}

	public bool HasRipeCrop()
	{
		Block block = GetCrop();
		if (block != null)
		{
			return GetCropStage(block) >= block.CropProps.GrowthStages;
		}
		return false;
	}

	public bool TryGrowCrop(double currentTotalHours)
	{
		Block block = GetCrop();
		if (block == null)
		{
			return false;
		}
		int currentGrowthStage = GetCropStage(block);
		if (currentGrowthStage < block.CropProps.GrowthStages)
		{
			int newGrowthStage = currentGrowthStage + 1;
			Block nextBlock = Api.World.GetBlock(block.CodeWithParts(newGrowthStage.ToString() ?? ""));
			if (nextBlock == null)
			{
				return false;
			}
			if (block.CropProps.Behaviors != null)
			{
				EnumHandling handled = EnumHandling.PassThrough;
				bool result = false;
				CropBehavior[] behaviors = block.CropProps.Behaviors;
				for (int i = 0; i < behaviors.Length; i++)
				{
					result = behaviors[i].TryGrowCrop(Api, this, currentTotalHours, newGrowthStage, ref handled);
					if (handled == EnumHandling.PreventSubsequent)
					{
						return result;
					}
				}
				if (handled == EnumHandling.PreventDefault)
				{
					return result;
				}
			}
			if (Api.World.BlockAccessor.GetBlockEntity(upPos) == null)
			{
				Api.World.BlockAccessor.SetBlock(nextBlock.BlockId, upPos);
			}
			else
			{
				Api.World.BlockAccessor.ExchangeBlock(nextBlock.BlockId, upPos);
			}
			ConsumeNutrients(block);
			return true;
		}
		return false;
	}

	private void ConsumeNutrients(Block cropBlock)
	{
		float nutrientLoss = cropBlock.CropProps.NutrientConsumption / (float)cropBlock.CropProps.GrowthStages;
		nutrients[(int)cropBlock.CropProps.RequiredNutrient] = Math.Max(0f, nutrients[(int)cropBlock.CropProps.RequiredNutrient] - nutrientLoss);
		UpdateFarmlandBlock();
	}

	private void UpdateFarmlandBlock()
	{
		int nowLevel = GetFertilityLevel((originalFertility[0] + originalFertility[1] + originalFertility[2]) / 3);
		Block farmlandBlock = Api.World.BlockAccessor.GetBlock(Pos);
		Block nextFarmlandBlock = Api.World.GetBlock(farmlandBlock.CodeWithParts(IsVisiblyMoist ? "moist" : "dry", Fertilities.GetKeyAtIndex(nowLevel)));
		if (nextFarmlandBlock == null)
		{
			Api.World.BlockAccessor.RemoveBlockEntity(Pos);
		}
		else if (farmlandBlock.BlockId != nextFarmlandBlock.BlockId)
		{
			Api.World.BlockAccessor.ExchangeBlock(nextFarmlandBlock.BlockId, Pos);
			Api.World.BlockAccessor.MarkBlockEntityDirty(Pos);
			Api.World.BlockAccessor.MarkBlockDirty(Pos);
		}
	}

	internal int GetFertilityLevel(float fertiltyValue)
	{
		int i = 0;
		foreach (KeyValuePair<string, float> fertility in Fertilities)
		{
			if (fertility.Value >= fertiltyValue)
			{
				return i;
			}
			i++;
		}
		return Fertilities.Count - 1;
	}

	internal Block GetCrop()
	{
		Block block = Api.World.BlockAccessor.GetBlock(upPos);
		if (block == null || block.CropProps == null)
		{
			return null;
		}
		return block;
	}

	internal int GetCropStage(Block block)
	{
		int.TryParse(block.LastCodePart(), out var stage);
		return stage;
	}

	private void updateFertilizerQuad()
	{
		if (capi == null)
		{
			return;
		}
		AssetLocation loc = new AssetLocation();
		if (fertilizerOverlayStrength == null || fertilizerOverlayStrength.Count == 0)
		{
			bool num = fertilizerQuad != null;
			fertilizerQuad = null;
			if (num)
			{
				MarkDirty(redrawOnClient: true);
			}
			return;
		}
		int i = 0;
		foreach (KeyValuePair<string, float> val in fertilizerOverlayStrength)
		{
			string intensity = "low";
			if (val.Value > 50f)
			{
				intensity = "med";
			}
			if (val.Value > 100f)
			{
				intensity = "high";
			}
			if (i > 0)
			{
				loc.Path += "++0~";
			}
			AssetLocation assetLocation = loc;
			assetLocation.Path = assetLocation.Path + "block/soil/farmland/fertilizer/" + val.Key + "-" + intensity;
			i++;
		}
		capi.BlockTextureAtlas.GetOrInsertTexture(loc, out var _, out var newFertilizerTexturePos);
		if (fertilizerTexturePos != newFertilizerTexturePos)
		{
			fertilizerTexturePos = newFertilizerTexturePos;
			genFertilizerQuad();
			MarkDirty(redrawOnClient: true);
		}
	}

	private void genFertilizerQuad()
	{
		Shape shape = capi.Assets.TryGet(new AssetLocation("shapes/block/farmland-fertilizer.json")).ToObject<Shape>();
		capi.Tesselator.TesselateShape(new TesselationMetaData
		{
			TypeForLogging = "farmland fertilizer quad",
			TexSource = this
		}, shape, out fertilizerQuad);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		nutrients[0] = tree.GetFloat("n");
		nutrients[1] = tree.GetFloat("p");
		nutrients[2] = tree.GetFloat("k");
		slowReleaseNutrients[0] = tree.GetFloat("slowN");
		slowReleaseNutrients[1] = tree.GetFloat("slowP");
		slowReleaseNutrients[2] = tree.GetFloat("slowK");
		moistureLevel = tree.GetFloat("moistureLevel");
		lastWaterSearchedTotalHours = tree.GetDouble("lastWaterSearchedTotalHours");
		if (!tree.HasAttribute("originalFertilityN"))
		{
			originalFertility[0] = tree.GetInt("originalFertility");
			originalFertility[1] = tree.GetInt("originalFertility");
			originalFertility[2] = tree.GetInt("originalFertility");
		}
		else
		{
			originalFertility[0] = tree.GetInt("originalFertilityN");
			originalFertility[1] = tree.GetInt("originalFertilityP");
			originalFertility[2] = tree.GetInt("originalFertilityK");
		}
		if (tree.HasAttribute("totalHoursForNextStage"))
		{
			totalHoursForNextStage = tree.GetDouble("totalHoursForNextStage");
			totalHoursLastUpdate = tree.GetDouble("totalHoursFertilityCheck");
		}
		else
		{
			totalHoursForNextStage = tree.GetDouble("totalDaysForNextStage") * 24.0;
			totalHoursLastUpdate = tree.GetDouble("totalDaysFertilityCheck") * 24.0;
		}
		lastMoistureLevelUpdateTotalDays = tree.GetDouble("lastMoistureLevelUpdateTotalDays");
		cropAttrs = tree["cropAttrs"] as TreeAttribute;
		if (cropAttrs == null)
		{
			cropAttrs = new TreeAttribute();
		}
		lastWaterDistance = tree.GetFloat("lastWaterDistance");
		unripeCropColdDamaged = tree.GetBool("unripeCropExposedToFrost");
		ripeCropColdDamaged = tree.GetBool("ripeCropExposedToFrost");
		unripeHeatDamaged = tree.GetBool("unripeHeatDamaged");
		saltExposed = tree.GetBool("saltExposed");
		roomness = tree.GetInt("roomness");
		string[] permaboosts = (tree as TreeAttribute).GetStringArray("permaBoosts");
		if (permaboosts != null)
		{
			PermaBoosts.AddRange(permaboosts);
		}
		ITreeAttribute ftree = tree.GetTreeAttribute("fertilizerOverlayStrength");
		if (ftree != null)
		{
			fertilizerOverlayStrength = new Dictionary<string, float>();
			foreach (KeyValuePair<string, IAttribute> val in ftree)
			{
				fertilizerOverlayStrength[val.Key] = (val.Value as FloatAttribute).value;
			}
		}
		else
		{
			fertilizerOverlayStrength = null;
		}
		updateFertilizerQuad();
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetFloat("n", nutrients[0]);
		tree.SetFloat("p", nutrients[1]);
		tree.SetFloat("k", nutrients[2]);
		tree.SetFloat("slowN", slowReleaseNutrients[0]);
		tree.SetFloat("slowP", slowReleaseNutrients[1]);
		tree.SetFloat("slowK", slowReleaseNutrients[2]);
		tree.SetFloat("moistureLevel", moistureLevel);
		tree.SetDouble("lastWaterSearchedTotalHours", lastWaterSearchedTotalHours);
		tree.SetInt("originalFertilityN", originalFertility[0]);
		tree.SetInt("originalFertilityP", originalFertility[1]);
		tree.SetInt("originalFertilityK", originalFertility[2]);
		tree.SetDouble("totalHoursForNextStage", totalHoursForNextStage);
		tree.SetDouble("totalHoursFertilityCheck", totalHoursLastUpdate);
		tree.SetDouble("lastMoistureLevelUpdateTotalDays", lastMoistureLevelUpdateTotalDays);
		tree.SetFloat("lastWaterDistance", lastWaterDistance);
		tree.SetBool("ripeCropExposedToFrost", ripeCropColdDamaged);
		tree.SetBool("unripeCropExposedToFrost", unripeCropColdDamaged);
		tree.SetBool("unripeHeatDamaged", unripeHeatDamaged);
		tree.SetBool("saltExposed", damageAccum[4] > 1f);
		(tree as TreeAttribute).SetStringArray("permaBoosts", PermaBoosts.ToArray());
		tree.SetInt("roomness", roomness);
		tree["cropAttrs"] = cropAttrs;
		if (fertilizerOverlayStrength == null)
		{
			return;
		}
		TreeAttribute ftree = (TreeAttribute)(tree["fertilizerOverlayStrength"] = new TreeAttribute());
		foreach (KeyValuePair<string, float> val in fertilizerOverlayStrength)
		{
			ftree.SetFloat(val.Key, val.Value);
		}
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		BlockCropProperties cropProps = GetCrop()?.CropProps;
		if (cropProps != null)
		{
			dsc.AppendLine(Lang.Get("Required Nutrient: {0}", cropProps.RequiredNutrient));
			dsc.AppendLine(Lang.Get("Growth Stage: {0} / {1}", GetCropStage(GetCrop()), cropProps.GrowthStages));
			dsc.AppendLine();
		}
		dsc.AppendLine(Lang.Get("farmland-nutrientlevels", Math.Round(nutrients[0], 1), Math.Round(nutrients[1], 1), Math.Round(nutrients[2], 1)));
		float snn = (float)Math.Round(slowReleaseNutrients[0], 1);
		float snp = (float)Math.Round(slowReleaseNutrients[1], 1);
		float snk = (float)Math.Round(slowReleaseNutrients[2], 1);
		if (snn > 0f || snp > 0f || snk > 0f)
		{
			List<string> nutrs = new List<string>();
			if (snn > 0f)
			{
				nutrs.Add(Lang.Get("+{0}% N", snn));
			}
			if (snp > 0f)
			{
				nutrs.Add(Lang.Get("+{0}% P", snp));
			}
			if (snk > 0f)
			{
				nutrs.Add(Lang.Get("+{0}% K", snk));
			}
			dsc.AppendLine(Lang.Get("farmland-activefertilizer", string.Join(", ", nutrs)));
		}
		if (cropProps == null)
		{
			float speedn = (float)Math.Round(100f * GetGrowthRate(EnumSoilNutrient.N), 0);
			float speedp = (float)Math.Round(100f * GetGrowthRate(EnumSoilNutrient.P), 0);
			float speedk = (float)Math.Round(100f * GetGrowthRate(EnumSoilNutrient.K), 0);
			string colorn = ColorUtil.Int2Hex(GuiStyle.DamageColorGradient[(int)Math.Min(99f, speedn)]);
			string colorp = ColorUtil.Int2Hex(GuiStyle.DamageColorGradient[(int)Math.Min(99f, speedp)]);
			string colork = ColorUtil.Int2Hex(GuiStyle.DamageColorGradient[(int)Math.Min(99f, speedk)]);
			dsc.AppendLine(Lang.Get("farmland-growthspeeds", colorn, speedn, colorp, speedp, colork, speedk));
		}
		else
		{
			float speed = (float)Math.Round(100f * GetGrowthRate(cropProps.RequiredNutrient), 0);
			string color = ColorUtil.Int2Hex(GuiStyle.DamageColorGradient[(int)Math.Min(99f, speed)]);
			dsc.AppendLine(Lang.Get("farmland-growthspeed", color, speed, cropProps.RequiredNutrient));
		}
		float moisture = (float)Math.Round(moistureLevel * 100f, 0);
		string colorm = ColorUtil.Int2Hex(GuiStyle.DamageColorGradient[(int)Math.Min(99f, moisture)]);
		dsc.AppendLine(Lang.Get("farmland-moisture", colorm, moisture));
		if ((ripeCropColdDamaged || unripeCropColdDamaged || unripeHeatDamaged) && cropProps != null)
		{
			if (ripeCropColdDamaged)
			{
				dsc.AppendLine(Lang.Get("farmland-ripecolddamaged", (int)(cropProps.ColdDamageRipeMul * 100f)));
			}
			else if (unripeCropColdDamaged)
			{
				dsc.AppendLine(Lang.Get("farmland-unripecolddamaged", (int)(cropProps.DamageGrowthStuntMul * 100f)));
			}
			else if (unripeHeatDamaged)
			{
				dsc.AppendLine(Lang.Get("farmland-unripeheatdamaged", (int)(cropProps.DamageGrowthStuntMul * 100f)));
			}
		}
		if (roomness > 0)
		{
			dsc.AppendLine(Lang.Get("greenhousetempbonus"));
		}
		if (saltExposed)
		{
			dsc.AppendLine(Lang.Get("farmland-saltdamage"));
		}
		dsc.ToString();
	}

	public void WaterFarmland(float dt, bool waterNeightbours = true)
	{
		float prevLevel = moistureLevel;
		moistureLevel = Math.Min(1f, moistureLevel + dt / 2f);
		if (waterNeightbours)
		{
			BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
			foreach (BlockFacing neib in hORIZONTALS)
			{
				BlockPos npos = Pos.AddCopy(neib);
				if (Api.World.BlockAccessor.GetBlockEntity(npos) is BlockEntityFarmland bef)
				{
					bef.WaterFarmland(dt / 3f, waterNeightbours: false);
				}
			}
		}
		updateMoistureLevel(Api.World.Calendar.TotalDays, lastWaterDistance);
		UpdateFarmlandBlock();
		if ((double)(moistureLevel - prevLevel) > 0.05)
		{
			MarkDirty(redrawOnClient: true);
		}
	}

	public bool IsSuitableFor(Entity entity, CreatureDiet diet)
	{
		if (diet == null)
		{
			return false;
		}
		if (GetCrop() == null)
		{
			return false;
		}
		return diet.Matches(EnumFoodCategory.NoNutrition, creatureFoodTags);
	}

	public float ConsumeOnePortion(Entity entity)
	{
		Block cropBlock = GetCrop();
		if (cropBlock == null)
		{
			return 0f;
		}
		Block deadCropBlock = Api.World.GetBlock(new AssetLocation("deadcrop"));
		Api.World.BlockAccessor.SetBlock(deadCropBlock.Id, upPos);
		BlockEntityDeadCrop obj = Api.World.BlockAccessor.GetBlockEntity(upPos) as BlockEntityDeadCrop;
		obj.Inventory[0].Itemstack = new ItemStack(cropBlock);
		obj.deathReason = EnumCropStressType.Eaten;
		return 1f;
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		mesher.AddMeshData(fertilizerQuad);
		return false;
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
}
