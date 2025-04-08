using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class EntityBehaviorBodyTemperature : EntityBehavior
{
	private ITreeAttribute tempTree;

	private ICoreAPI api;

	private EntityAgent eagent;

	private float accum;

	private float slowaccum;

	private float veryslowaccum;

	private BlockPos plrpos = new BlockPos();

	private BlockPos tmpPos = new BlockPos();

	private bool inEnclosedRoom;

	private float tempChange;

	private float clothingBonus;

	private float damagingFreezeHours;

	private int sprinterCounter;

	private double lastWearableHoursTotalUpdate;

	private float bodyTemperatureResistance;

	private ICachingBlockAccessor blockAccess;

	public float NormalBodyTemperature;

	private bool firstTick;

	private long lastMoveMs;

	public float CurBodyTemperature
	{
		get
		{
			return tempTree.GetFloat("bodytemp");
		}
		set
		{
			tempTree.SetFloat("bodytemp", value);
			entity.WatchedAttributes.MarkPathDirty("bodyTemp");
		}
	}

	protected float nearHeatSourceStrength
	{
		get
		{
			return tempTree.GetFloat("nearHeatSourceStrength");
		}
		set
		{
			tempTree.SetFloat("nearHeatSourceStrength", value);
		}
	}

	public float Wetness
	{
		get
		{
			return entity.WatchedAttributes.GetFloat("wetness");
		}
		set
		{
			entity.WatchedAttributes.SetFloat("wetness", value);
		}
	}

	public double LastWetnessUpdateTotalHours
	{
		get
		{
			return entity.WatchedAttributes.GetDouble("lastWetnessUpdateTotalHours");
		}
		set
		{
			entity.WatchedAttributes.SetDouble("lastWetnessUpdateTotalHours", value);
		}
	}

	public double BodyTempUpdateTotalHours
	{
		get
		{
			return tempTree.GetDouble("bodyTempUpdateTotalHours");
		}
		set
		{
			tempTree.SetDouble("bodyTempUpdateTotalHours", value);
			entity.WatchedAttributes.MarkPathDirty("bodyTemp");
		}
	}

	public EntityBehaviorBodyTemperature(Entity entity)
		: base(entity)
	{
		eagent = entity as EntityAgent;
	}

	public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
	{
		api = entity.World.Api;
		blockAccess = api.World.GetCachingBlockAccessor(synchronize: false, relight: false);
		tempTree = entity.WatchedAttributes.GetTreeAttribute("bodyTemp");
		NormalBodyTemperature = typeAttributes["defaultBodyTemperature"].AsFloat(37f);
		if (tempTree == null)
		{
			entity.WatchedAttributes.SetAttribute("bodyTemp", tempTree = new TreeAttribute());
			CurBodyTemperature = NormalBodyTemperature + 4f;
			BodyTempUpdateTotalHours = api.World.Calendar.TotalHours;
			LastWetnessUpdateTotalHours = api.World.Calendar.TotalHours;
		}
		else
		{
			BodyTempUpdateTotalHours = api.World.Calendar.TotalHours;
			LastWetnessUpdateTotalHours = api.World.Calendar.TotalHours;
			bodyTemperatureResistance = entity.World.Config.GetString("bodyTemperatureResistance").ToFloat();
		}
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		blockAccess?.Dispose();
		blockAccess = null;
	}

	public override void OnGameTick(float deltaTime)
	{
		if (!firstTick && api.Side == EnumAppSide.Client && entity.Properties.Client.Renderer is EntityShapeRenderer esr)
		{
			esr.getFrostAlpha = delegate
			{
				float temperature = api.World.BlockAccessor.GetClimateAt(entity.Pos.AsBlockPos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, api.World.Calendar.TotalDays).Temperature;
				float num2 = GameMath.Clamp((NormalBodyTemperature - CurBodyTemperature) / 4f - 0.5f, 0f, 1f);
				return GameMath.Clamp((Math.Max(0f, 0f - temperature) - 5f) / 5f, 0f, 1f) * num2;
			};
		}
		firstTick = true;
		updateFreezingAnimState();
		accum += deltaTime;
		slowaccum += deltaTime;
		veryslowaccum += deltaTime;
		plrpos.Set((int)entity.Pos.X, (int)entity.Pos.Y, (int)entity.Pos.Z);
		if (veryslowaccum > 10f && damagingFreezeHours > 3f)
		{
			if (api.World.Config.GetString("harshWinters").ToBool(defaultValue: true))
			{
				entity.ReceiveDamage(new DamageSource
				{
					DamageTier = 0,
					Source = EnumDamageSource.Weather,
					Type = EnumDamageType.Frost
				}, 0.2f);
			}
			veryslowaccum = 0f;
			if (eagent.Controls.Sprint)
			{
				sprinterCounter = GameMath.Clamp(sprinterCounter + 1, 0, 10);
			}
			else
			{
				sprinterCounter = GameMath.Clamp(sprinterCounter - 1, 0, 10);
			}
		}
		if (slowaccum > 3f)
		{
			if (api.Side == EnumAppSide.Server)
			{
				Room room = api.ModLoader.GetModSystem<RoomRegistry>().GetRoomForPosition(plrpos);
				inEnclosedRoom = room.ExitCount == 0 || room.SkylightCount < room.NonSkylightCount;
				nearHeatSourceStrength = 0f;
				double px = entity.Pos.X;
				double py = entity.Pos.Y + 0.9;
				double pz = entity.Pos.Z;
				double proximityPower = (inEnclosedRoom ? 0.875 : 1.25);
				BlockPos min;
				BlockPos max;
				if (inEnclosedRoom && room.Location.SizeX >= 1 && room.Location.SizeY >= 1 && room.Location.SizeZ >= 1)
				{
					min = new BlockPos(room.Location.MinX, room.Location.MinY, room.Location.MinZ);
					max = new BlockPos(room.Location.MaxX, room.Location.MaxY, room.Location.MaxZ);
				}
				else
				{
					min = plrpos.AddCopy(-3, -3, -3);
					max = plrpos.AddCopy(3, 3, 3);
				}
				blockAccess.Begin();
				blockAccess.WalkBlocks(min, max, delegate(Block block, int x, int y, int z)
				{
					IHeatSource @interface = block.GetInterface<IHeatSource>(api.World, tmpPos.Set(x, y, z));
					if (@interface != null)
					{
						float num = Math.Min(1f, 9f / (8f + (float)Math.Pow(tmpPos.DistanceSqToNearerEdge(px, py, pz), proximityPower)));
						nearHeatSourceStrength += @interface.GetHeatStrength(api.World, tmpPos, plrpos) * num;
					}
				});
			}
			updateWearableConditions();
			entity.WatchedAttributes.MarkPathDirty("bodyTemp");
			slowaccum = 0f;
		}
		if (!(accum > 1f) || api.Side != EnumAppSide.Server)
		{
			return;
		}
		EntityPlayer eplr = entity as EntityPlayer;
		IPlayer plr = eplr?.Player;
		if (api.Side == EnumAppSide.Server)
		{
			IServerPlayer obj = plr as IServerPlayer;
			if (obj == null || obj.ConnectionState != EnumClientState.Playing)
			{
				return;
			}
		}
		if ((plr != null && plr.WorldData.CurrentGameMode == EnumGameMode.Creative) || (plr != null && plr.WorldData.CurrentGameMode == EnumGameMode.Spectator))
		{
			CurBodyTemperature = NormalBodyTemperature;
			entity.WatchedAttributes.SetFloat("freezingEffectStrength", 0f);
			return;
		}
		if (plr != null && (eplr.Controls.TriesToMove || eplr.Controls.Jump || eplr.Controls.LeftMouseDown || eplr.Controls.RightMouseDown))
		{
			lastMoveMs = entity.World.ElapsedMilliseconds;
		}
		ClimateCondition conds = api.World.BlockAccessor.GetClimateAt(plrpos);
		if (conds == null)
		{
			return;
		}
		Vec3d windspeed = api.World.BlockAccessor.GetWindSpeedAt(plrpos);
		bool rainExposed = api.World.BlockAccessor.GetRainMapHeightAt(plrpos) <= plrpos.Y;
		Wetness = GameMath.Clamp(Wetness + conds.Rainfall * (rainExposed ? 0.06f : 0f) * ((conds.Temperature < -1f) ? 0.05f : 1f) + (float)(entity.Swimming ? 1 : 0) - (float)Math.Max(0.0, (api.World.Calendar.TotalHours - LastWetnessUpdateTotalHours) * (double)GameMath.Clamp(nearHeatSourceStrength, 1f, 2f)), 0f, 1f);
		LastWetnessUpdateTotalHours = api.World.Calendar.TotalHours;
		accum = 0f;
		float sprintBonus = (float)sprinterCounter / 2f;
		float wetnessDebuff = (float)Math.Max(0.0, (double)Wetness - 0.1) * 15f;
		float hereTemperature = conds.Temperature + clothingBonus + sprintBonus - wetnessDebuff;
		float tempDiff = hereTemperature - GameMath.Clamp(hereTemperature, bodyTemperatureResistance, 50f);
		if (tempDiff == 0f)
		{
			tempDiff = Math.Max(hereTemperature - bodyTemperatureResistance, 0f);
		}
		float ambientTempChange = GameMath.Clamp(tempDiff / 6f, -6f, 6f);
		tempChange = nearHeatSourceStrength + (inEnclosedRoom ? 1f : (0f - (float)Math.Max((windspeed.Length() - 0.15) * 2.0, 0.0) + ambientTempChange));
		EntityBehaviorTiredness behavior = entity.GetBehavior<EntityBehaviorTiredness>();
		if (behavior != null && behavior.IsSleeping)
		{
			if (inEnclosedRoom)
			{
				tempChange = GameMath.Clamp(NormalBodyTemperature - CurBodyTemperature, -0.15f, 0.15f);
			}
			else if (!rainExposed)
			{
				tempChange += GameMath.Clamp(NormalBodyTemperature - CurBodyTemperature, 1f, 1f);
			}
		}
		if (entity.IsOnFire)
		{
			tempChange = Math.Max(25f, tempChange);
		}
		float tempUpdateHoursPassed = (float)(api.World.Calendar.TotalHours - BodyTempUpdateTotalHours);
		if (!((double)tempUpdateHoursPassed > 0.01))
		{
			return;
		}
		if ((double)tempChange < -0.5 || tempChange > 0f)
		{
			if ((double)tempChange > 0.5)
			{
				tempChange *= 2f;
			}
			CurBodyTemperature = GameMath.Clamp(CurBodyTemperature + tempChange * tempUpdateHoursPassed, 31f, 45f);
		}
		BodyTempUpdateTotalHours = api.World.Calendar.TotalHours;
		float str = GameMath.Clamp((NormalBodyTemperature - CurBodyTemperature) / 4f - 0.5f, 0f, 1f);
		entity.WatchedAttributes.SetFloat("freezingEffectStrength", str);
		if (NormalBodyTemperature - CurBodyTemperature > 4f)
		{
			damagingFreezeHours += tempUpdateHoursPassed;
		}
		else
		{
			damagingFreezeHours = 0f;
		}
	}

	private void updateFreezingAnimState()
	{
		float str = entity.WatchedAttributes.GetFloat("freezingEffectStrength");
		bool held = (entity as EntityAgent)?.LeftHandItemSlot?.Itemstack != null || (entity as EntityAgent)?.RightHandItemSlot?.Itemstack != null;
		EnumGameMode? mode = (entity as EntityPlayer)?.Player?.WorldData?.CurrentGameMode;
		if ((damagingFreezeHours > 0f || (double)str > 0.4) && mode.GetValueOrDefault() != EnumGameMode.Creative && mode.GetValueOrDefault() != EnumGameMode.Spectator && entity.Alive)
		{
			if (held)
			{
				entity.StartAnimation("coldidleheld");
				entity.StopAnimation("coldidle");
			}
			else
			{
				entity.StartAnimation("coldidle");
				entity.StopAnimation("coldidleheld");
			}
		}
		else if (entity.AnimManager.IsAnimationActive("coldidle") || entity.AnimManager.IsAnimationActive("coldidleheld"))
		{
			entity.StopAnimation("coldidle");
			entity.StopAnimation("coldidleheld");
		}
	}

	public void didConsume(ItemStack stack, float intensity = 1f)
	{
		Math.Abs(stack.Collectible.GetTemperature(api.World, stack) - CurBodyTemperature);
		_ = 10f;
	}

	private void updateWearableConditions()
	{
		double hoursPassed = api.World.Calendar.TotalHours - lastWearableHoursTotalUpdate;
		if (hoursPassed < -1.0)
		{
			lastWearableHoursTotalUpdate = api.World.Calendar.TotalHours;
		}
		else
		{
			if (hoursPassed < 0.5)
			{
				return;
			}
			EntityAgent obj = entity as EntityAgent;
			clothingBonus = 0f;
			float conditionloss = 0f;
			if (entity.World.ElapsedMilliseconds - lastMoveMs <= 3000)
			{
				conditionloss = (0f - (float)hoursPassed) / 1296f;
			}
			EntityBehaviorPlayerInventory bh = obj?.GetBehavior<EntityBehaviorPlayerInventory>();
			if (bh?.Inventory != null)
			{
				foreach (ItemSlot slot in bh.Inventory)
				{
					if (slot.Itemstack?.Collectible is ItemWearable { IsArmor: false } wearableItem)
					{
						clothingBonus += wearableItem.GetWarmth(slot);
						wearableItem.ChangeCondition(slot, conditionloss);
					}
				}
			}
			lastWearableHoursTotalUpdate = api.World.Calendar.TotalHours;
		}
	}

	public override void OnEntityRevive()
	{
		BodyTempUpdateTotalHours = api.World.Calendar.TotalHours;
		LastWetnessUpdateTotalHours = api.World.Calendar.TotalHours;
		Wetness = 0f;
		CurBodyTemperature = NormalBodyTemperature + 4f;
	}

	public override string PropertyName()
	{
		return "bodytemperature";
	}
}
