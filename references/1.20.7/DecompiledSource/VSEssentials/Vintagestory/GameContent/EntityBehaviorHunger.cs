using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class EntityBehaviorHunger : EntityBehavior
{
	private ITreeAttribute hungerTree;

	private EntityAgent entityAgent;

	private float hungerCounter;

	private int sprintCounter;

	private long listenerId;

	private long lastMoveMs;

	private ICoreAPI api;

	private float detoxCounter;

	public float SaturationLossDelayFruit
	{
		get
		{
			return hungerTree.GetFloat("saturationlossdelayfruit");
		}
		set
		{
			hungerTree.SetFloat("saturationlossdelayfruit", value);
			entity.WatchedAttributes.MarkPathDirty("hunger");
		}
	}

	public float SaturationLossDelayVegetable
	{
		get
		{
			return hungerTree.GetFloat("saturationlossdelayvegetable");
		}
		set
		{
			hungerTree.SetFloat("saturationlossdelayvegetable", value);
			entity.WatchedAttributes.MarkPathDirty("hunger");
		}
	}

	public float SaturationLossDelayProtein
	{
		get
		{
			return hungerTree.GetFloat("saturationlossdelayprotein");
		}
		set
		{
			hungerTree.SetFloat("saturationlossdelayprotein", value);
			entity.WatchedAttributes.MarkPathDirty("hunger");
		}
	}

	public float SaturationLossDelayGrain
	{
		get
		{
			return hungerTree.GetFloat("saturationlossdelaygrain");
		}
		set
		{
			hungerTree.SetFloat("saturationlossdelaygrain", value);
			entity.WatchedAttributes.MarkPathDirty("hunger");
		}
	}

	public float SaturationLossDelayDairy
	{
		get
		{
			return hungerTree.GetFloat("saturationlossdelaydairy");
		}
		set
		{
			hungerTree.SetFloat("saturationlossdelaydairy", value);
			entity.WatchedAttributes.MarkPathDirty("hunger");
		}
	}

	public float Saturation
	{
		get
		{
			return hungerTree.GetFloat("currentsaturation");
		}
		set
		{
			hungerTree.SetFloat("currentsaturation", value);
			entity.WatchedAttributes.MarkPathDirty("hunger");
		}
	}

	public float MaxSaturation
	{
		get
		{
			return hungerTree.GetFloat("maxsaturation");
		}
		set
		{
			hungerTree.SetFloat("maxsaturation", value);
			entity.WatchedAttributes.MarkPathDirty("hunger");
		}
	}

	public float FruitLevel
	{
		get
		{
			return hungerTree.GetFloat("fruitLevel");
		}
		set
		{
			hungerTree.SetFloat("fruitLevel", value);
			entity.WatchedAttributes.MarkPathDirty("hunger");
		}
	}

	public float VegetableLevel
	{
		get
		{
			return hungerTree.GetFloat("vegetableLevel");
		}
		set
		{
			hungerTree.SetFloat("vegetableLevel", value);
			entity.WatchedAttributes.MarkPathDirty("hunger");
		}
	}

	public float ProteinLevel
	{
		get
		{
			return hungerTree.GetFloat("proteinLevel");
		}
		set
		{
			hungerTree.SetFloat("proteinLevel", value);
			entity.WatchedAttributes.MarkPathDirty("hunger");
		}
	}

	public float GrainLevel
	{
		get
		{
			return hungerTree.GetFloat("grainLevel");
		}
		set
		{
			hungerTree.SetFloat("grainLevel", value);
			entity.WatchedAttributes.MarkPathDirty("hunger");
		}
	}

	public float DairyLevel
	{
		get
		{
			return hungerTree.GetFloat("dairyLevel");
		}
		set
		{
			hungerTree.SetFloat("dairyLevel", value);
			entity.WatchedAttributes.MarkPathDirty("hunger");
		}
	}

	public EntityBehaviorHunger(Entity entity)
		: base(entity)
	{
		entityAgent = entity as EntityAgent;
	}

	public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
	{
		hungerTree = entity.WatchedAttributes.GetTreeAttribute("hunger");
		api = entity.World.Api;
		if (hungerTree == null)
		{
			entity.WatchedAttributes.SetAttribute("hunger", hungerTree = new TreeAttribute());
			Saturation = typeAttributes["currentsaturation"].AsFloat(1500f);
			MaxSaturation = typeAttributes["maxsaturation"].AsFloat(1500f);
			SaturationLossDelayFruit = 0f;
			SaturationLossDelayVegetable = 0f;
			SaturationLossDelayGrain = 0f;
			SaturationLossDelayProtein = 0f;
			SaturationLossDelayDairy = 0f;
			FruitLevel = 0f;
			VegetableLevel = 0f;
			GrainLevel = 0f;
			ProteinLevel = 0f;
			DairyLevel = 0f;
		}
		listenerId = entity.World.RegisterGameTickListener(SlowTick, 6000);
		UpdateNutrientHealthBoost();
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		base.OnEntityDespawn(despawn);
		entity.World.UnregisterGameTickListener(listenerId);
	}

	public override void DidAttack(DamageSource source, EntityAgent targetEntity, ref EnumHandling handled)
	{
		ConsumeSaturation(3f);
	}

	public virtual void ConsumeSaturation(float amount)
	{
		ReduceSaturation(amount / 10f);
	}

	public override void OnEntityReceiveSaturation(float saturation, EnumFoodCategory foodCat = EnumFoodCategory.Unknown, float saturationLossDelay = 10f, float nutritionGainMultiplier = 1f)
	{
		float maxsat = MaxSaturation;
		bool full = Saturation >= maxsat;
		Saturation = Math.Min(maxsat, Saturation + saturation);
		switch (foodCat)
		{
		case EnumFoodCategory.Fruit:
			if (!full)
			{
				FruitLevel = Math.Min(maxsat, FruitLevel + saturation / 2.5f * nutritionGainMultiplier);
			}
			SaturationLossDelayFruit = Math.Max(SaturationLossDelayFruit, saturationLossDelay);
			break;
		case EnumFoodCategory.Vegetable:
			if (!full)
			{
				VegetableLevel = Math.Min(maxsat, VegetableLevel + saturation / 2.5f * nutritionGainMultiplier);
			}
			SaturationLossDelayVegetable = Math.Max(SaturationLossDelayVegetable, saturationLossDelay);
			break;
		case EnumFoodCategory.Protein:
			if (!full)
			{
				ProteinLevel = Math.Min(maxsat, ProteinLevel + saturation / 2.5f * nutritionGainMultiplier);
			}
			SaturationLossDelayProtein = Math.Max(SaturationLossDelayProtein, saturationLossDelay);
			break;
		case EnumFoodCategory.Grain:
			if (!full)
			{
				GrainLevel = Math.Min(maxsat, GrainLevel + saturation / 2.5f * nutritionGainMultiplier);
			}
			SaturationLossDelayGrain = Math.Max(SaturationLossDelayGrain, saturationLossDelay);
			break;
		case EnumFoodCategory.Dairy:
			if (!full)
			{
				DairyLevel = Math.Min(maxsat, DairyLevel + saturation / 2.5f * nutritionGainMultiplier);
			}
			SaturationLossDelayDairy = Math.Max(SaturationLossDelayDairy, saturationLossDelay);
			break;
		}
		UpdateNutrientHealthBoost();
	}

	public override void OnGameTick(float deltaTime)
	{
		if (entity is EntityPlayer)
		{
			EntityPlayer plr = (EntityPlayer)entity;
			EnumGameMode mode = entity.World.PlayerByUid(plr.PlayerUID).WorldData.CurrentGameMode;
			detox(deltaTime);
			if (mode == EnumGameMode.Creative || mode == EnumGameMode.Spectator)
			{
				return;
			}
			if (plr.Controls.TriesToMove || plr.Controls.Jump || plr.Controls.LeftMouseDown || plr.Controls.RightMouseDown)
			{
				lastMoveMs = entity.World.ElapsedMilliseconds;
			}
		}
		if (entityAgent != null && entityAgent.Controls.Sprint)
		{
			sprintCounter++;
		}
		hungerCounter += deltaTime;
		if (hungerCounter > 10f)
		{
			bool num = entity.World.ElapsedMilliseconds - lastMoveMs > 3000;
			float multiplierPerGameSec = entity.Api.World.Calendar.SpeedOfTime * entity.Api.World.Calendar.CalendarSpeedMul;
			float satLossMultiplier = GlobalConstants.HungerSpeedModifier / 30f;
			if (num)
			{
				satLossMultiplier /= 4f;
			}
			satLossMultiplier *= 1.2f * (8f + (float)sprintCounter / 15f) / 10f;
			satLossMultiplier *= entity.Stats.GetBlended("hungerrate");
			ReduceSaturation(satLossMultiplier * multiplierPerGameSec);
			hungerCounter = 0f;
			sprintCounter = 0;
			detox(deltaTime);
		}
	}

	private void detox(float dt)
	{
		detoxCounter += dt;
		if (detoxCounter > 1f)
		{
			float intox = entity.WatchedAttributes.GetFloat("intoxication");
			if (intox > 0f)
			{
				entity.WatchedAttributes.SetFloat("intoxication", Math.Max(0f, intox - 0.005f));
			}
			detoxCounter = 0f;
		}
	}

	private bool ReduceSaturation(float satLossMultiplier)
	{
		bool isondelay = false;
		satLossMultiplier *= GlobalConstants.HungerSpeedModifier;
		if (SaturationLossDelayFruit > 0f)
		{
			SaturationLossDelayFruit -= 10f * satLossMultiplier;
			isondelay = true;
		}
		else
		{
			FruitLevel = Math.Max(0f, FruitLevel - Math.Max(0.5f, 0.001f * FruitLevel) * satLossMultiplier * 0.25f);
		}
		if (SaturationLossDelayVegetable > 0f)
		{
			SaturationLossDelayVegetable -= 10f * satLossMultiplier;
			isondelay = true;
		}
		else
		{
			VegetableLevel = Math.Max(0f, VegetableLevel - Math.Max(0.5f, 0.001f * VegetableLevel) * satLossMultiplier * 0.25f);
		}
		if (SaturationLossDelayProtein > 0f)
		{
			SaturationLossDelayProtein -= 10f * satLossMultiplier;
			isondelay = true;
		}
		else
		{
			ProteinLevel = Math.Max(0f, ProteinLevel - Math.Max(0.5f, 0.001f * ProteinLevel) * satLossMultiplier * 0.25f);
		}
		if (SaturationLossDelayGrain > 0f)
		{
			SaturationLossDelayGrain -= 10f * satLossMultiplier;
			isondelay = true;
		}
		else
		{
			GrainLevel = Math.Max(0f, GrainLevel - Math.Max(0.5f, 0.001f * GrainLevel) * satLossMultiplier * 0.25f);
		}
		if (SaturationLossDelayDairy > 0f)
		{
			SaturationLossDelayDairy -= 10f * satLossMultiplier;
			isondelay = true;
		}
		else
		{
			DairyLevel = Math.Max(0f, DairyLevel - Math.Max(0.5f, 0.001f * DairyLevel) * satLossMultiplier * 0.25f / 2f);
		}
		UpdateNutrientHealthBoost();
		if (isondelay)
		{
			hungerCounter -= 10f;
			return true;
		}
		float prevSaturation = Saturation;
		if (prevSaturation > 0f)
		{
			Saturation = Math.Max(0f, prevSaturation - satLossMultiplier * 10f);
			sprintCounter = 0;
		}
		return false;
	}

	public void UpdateNutrientHealthBoost()
	{
		float fruitRel = FruitLevel / MaxSaturation;
		float grainRel = GrainLevel / MaxSaturation;
		float vegetableRel = VegetableLevel / MaxSaturation;
		float proteinRel = ProteinLevel / MaxSaturation;
		float dairyRel = DairyLevel / MaxSaturation;
		EntityBehaviorHealth behavior = entity.GetBehavior<EntityBehaviorHealth>();
		float healthGain = 2.5f * (fruitRel + grainRel + vegetableRel + proteinRel + dairyRel);
		behavior.SetMaxHealthModifiers("nutrientHealthMod", healthGain);
	}

	private void SlowTick(float dt)
	{
		if (entity is EntityPlayer)
		{
			EntityPlayer plr = (EntityPlayer)entity;
			if (entity.World.PlayerByUid(plr.PlayerUID).WorldData.CurrentGameMode == EnumGameMode.Creative)
			{
				return;
			}
		}
		bool harshWinters = entity.World.Config.GetString("harshWinters").ToBool(defaultValue: true);
		float temperature = entity.World.BlockAccessor.GetClimateAt(entity.Pos.AsBlockPos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, entity.World.Calendar.TotalDays).Temperature;
		if (temperature >= 2f || !harshWinters)
		{
			entity.Stats.Remove("hungerrate", "resistcold");
		}
		else
		{
			float diff = GameMath.Clamp(2f - temperature, 0f, 10f);
			Room room = entity.World.Api.ModLoader.GetModSystem<RoomRegistry>().GetRoomForPosition(entity.Pos.AsBlockPos);
			entity.Stats.Set("hungerrate", "resistcold", (room.ExitCount == 0) ? 0f : (diff / 40f), persistent: true);
		}
		if (Saturation <= 0f)
		{
			entity.ReceiveDamage(new DamageSource
			{
				Source = EnumDamageSource.Internal,
				Type = EnumDamageType.Hunger
			}, 0.125f);
			sprintCounter = 0;
		}
	}

	public override string PropertyName()
	{
		return "hunger";
	}

	public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
	{
		if (damageSource.Type == EnumDamageType.Heal && damageSource.Source == EnumDamageSource.Revive)
		{
			SaturationLossDelayFruit = 60f;
			SaturationLossDelayVegetable = 60f;
			SaturationLossDelayProtein = 60f;
			SaturationLossDelayGrain = 60f;
			SaturationLossDelayDairy = 60f;
			Saturation = MaxSaturation / 2f;
			VegetableLevel /= 2f;
			ProteinLevel /= 2f;
			FruitLevel /= 2f;
			DairyLevel /= 2f;
			GrainLevel /= 2f;
		}
	}
}
