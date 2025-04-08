using System;
using System.Collections.Generic;
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

public class EntityBehaviorHarvestable : EntityBehaviorContainer
{
	private const float minimumWeight = 0.5f;

	protected BlockDropItemStack[] jsonDrops;

	protected InventoryGeneric inv;

	protected GuiDialogCreatureContents dlg;

	private float baseHarvestDuration;

	private bool harshWinters;

	private bool fixedWeight;

	private float accum;

	private WorldInteraction[] interactions;

	private bool GotCrushed
	{
		get
		{
			if (!entity.WatchedAttributes.HasAttribute("deathReason") || entity.WatchedAttributes.GetInt("deathReason") != 2)
			{
				if (entity.WatchedAttributes.HasAttribute("deathDamageType"))
				{
					return entity.WatchedAttributes.GetInt("deathDamageType") == 9;
				}
				return false;
			}
			return true;
		}
	}

	private bool GotElectrocuted
	{
		get
		{
			if (entity.WatchedAttributes.HasAttribute("deathDamageType"))
			{
				return entity.WatchedAttributes.GetInt("deathDamageType") == 11;
			}
			return false;
		}
	}

	private bool GotAcidified
	{
		get
		{
			if (entity.WatchedAttributes.HasAttribute("deathDamageType"))
			{
				return entity.WatchedAttributes.GetInt("deathDamageType") == 14;
			}
			return false;
		}
	}

	public float AnimalWeight
	{
		get
		{
			return entity.WatchedAttributes.GetFloat("animalWeight", 1f);
		}
		set
		{
			entity.WatchedAttributes.SetFloat("animalWeight", value);
		}
	}

	public double LastWeightUpdateTotalHours
	{
		get
		{
			return entity.WatchedAttributes.GetDouble("lastWeightUpdateTotalHours", 1.0);
		}
		set
		{
			entity.WatchedAttributes.SetDouble("lastWeightUpdateTotalHours", value);
		}
	}

	protected float dropQuantityMultiplier
	{
		get
		{
			if (GotCrushed)
			{
				return 0.5f;
			}
			if (GotAcidified)
			{
				return 0.25f;
			}
			if (entity.WatchedAttributes.GetString("deathByEntity") != null && !entity.WatchedAttributes.HasAttribute("deathByPlayer"))
			{
				return 0.4f;
			}
			return 1f;
		}
	}

	public bool Harvestable
	{
		get
		{
			if (!entity.Alive)
			{
				return !IsHarvested;
			}
			return false;
		}
	}

	public bool IsHarvested => entity.WatchedAttributes.GetBool("harvested");

	public override InventoryBase Inventory => inv;

	public override string InventoryClassName => "harvestableInv";

	public float GetHarvestDuration(Entity forEntity)
	{
		return baseHarvestDuration * forEntity.Stats.GetBlended("animalHarvestingTime");
	}

	public EntityBehaviorHarvestable(Entity entity)
		: base(entity)
	{
		if (entity.World.Side == EnumAppSide.Client)
		{
			entity.WatchedAttributes.RegisterModifiedListener("harvestableInv", onDropsModified);
		}
		harshWinters = entity.World.Config.GetString("harshWinters").ToBool(defaultValue: true);
	}

	public override void AfterInitialized(bool onSpawn)
	{
		if (onSpawn)
		{
			LastWeightUpdateTotalHours = Math.Max(1.0, entity.World.Calendar.TotalHours - 168.0);
			AnimalWeight = (fixedWeight ? 1f : (0.66f + 0.2f * (float)entity.World.Rand.NextDouble()));
		}
		else if (fixedWeight)
		{
			AnimalWeight = 1f;
		}
	}

	public override void OnGameTick(float deltaTime)
	{
		if (entity.World.Side != EnumAppSide.Server)
		{
			return;
		}
		accum += deltaTime;
		if (!(accum > 1.5f))
		{
			return;
		}
		accum = 0f;
		if (!harshWinters || fixedWeight)
		{
			AnimalWeight = 1f;
			return;
		}
		double totalHours = entity.World.Calendar.TotalHours;
		double startHours = LastWeightUpdateTotalHours;
		double hoursPerDay = entity.World.Calendar.HoursPerDay;
		totalHours = Math.Min(totalHours, startHours + hoursPerDay * (double)entity.World.Calendar.DaysPerMonth);
		if (startHours < totalHours - 1.0)
		{
			double lastEatenTotalHours = entity.WatchedAttributes.GetDouble("lastMealEatenTotalHours", -9999.0);
			double fourmonthsHours = (double)(4 * entity.World.Calendar.DaysPerMonth) * hoursPerDay;
			double oneweekHours = 7.0 * hoursPerDay;
			BlockPos pos = entity.Pos.AsBlockPos;
			float weight = AnimalWeight;
			float previousweight = weight;
			float step = 3f;
			float baseTemperature = 0f;
			ClimateCondition conds = null;
			do
			{
				startHours += (double)step;
				double mealHourDiff = startHours - lastEatenTotalHours;
				if (mealHourDiff < 0.0)
				{
					mealHourDiff = fourmonthsHours;
				}
				if (!(mealHourDiff < fourmonthsHours))
				{
					if (weight <= 0.5f)
					{
						startHours = totalHours;
						break;
					}
					if (conds == null)
					{
						conds = entity.World.BlockAccessor.GetClimateAt(pos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, startHours / hoursPerDay);
						if (conds == null)
						{
							base.OnGameTick(deltaTime);
							return;
						}
						baseTemperature = conds.WorldGenTemperature;
					}
					else
					{
						conds.Temperature = baseTemperature;
						entity.World.BlockAccessor.GetClimateAt(pos, conds, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, startHours / hoursPerDay);
					}
					if (conds.Temperature <= 0f)
					{
						weight = Math.Max(0.5f, weight - step * 0.001f);
					}
				}
				else
				{
					bool ateRecently = mealHourDiff < oneweekHours;
					weight = Math.Min(1f, weight + step * (0.001f + (ateRecently ? 0.05f : 0f)));
				}
			}
			while (startHours < totalHours - 1.0);
			if (weight != previousweight)
			{
				AnimalWeight = weight;
			}
		}
		LastWeightUpdateTotalHours = startHours;
	}

	private void Inv_SlotModified(int slotid)
	{
		TreeAttribute tree = new TreeAttribute();
		inv.ToTreeAttributes(tree);
		entity.WatchedAttributes["harvestableInv"] = tree;
		entity.WatchedAttributes.MarkPathDirty("harvestableInv");
	}

	private void Inv_OnInventoryClosed(IPlayer player)
	{
		if (inv.Empty && entity.GetBehavior<EntityBehaviorDeadDecay>() != null)
		{
			entity.GetBehavior<EntityBehaviorDeadDecay>().DecayNow();
		}
	}

	private void onDropsModified()
	{
		if (entity.WatchedAttributes["harvestableInv"] is TreeAttribute tree)
		{
			int toadd = tree.GetInt("qslots") - inv.Count;
			inv.AddSlots(toadd);
			inv.FromTreeAttributes(tree);
			if (toadd > 0)
			{
				GuiDialogCreatureContents guiDialogCreatureContents = dlg;
				if (guiDialogCreatureContents != null && guiDialogCreatureContents.IsOpened())
				{
					dlg.Compose("carcasscontents");
				}
			}
		}
		entity.World.BlockAccessor.GetChunkAtBlockPos(entity.ServerPos.XYZ.AsBlockPos)?.MarkModified();
	}

	public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
	{
		inv = new InventoryGeneric(typeAttributes["quantitySlots"].AsInt(4), "harvestableContents-" + entity.EntityId, entity.Api);
		if (entity.WatchedAttributes["harvestableInv"] is TreeAttribute tree)
		{
			inv.FromTreeAttributes(tree);
		}
		inv.PutLocked = true;
		if (entity.World.Side == EnumAppSide.Server)
		{
			inv.SlotModified += Inv_SlotModified;
			inv.OnInventoryClosed += Inv_OnInventoryClosed;
		}
		base.Initialize(properties, typeAttributes);
		if (entity.World.Side == EnumAppSide.Server)
		{
			jsonDrops = typeAttributes["drops"].AsObject<BlockDropItemStack[]>();
		}
		baseHarvestDuration = typeAttributes["duration"].AsFloat(5f);
		fixedWeight = typeAttributes["fixedweight"].AsBool();
	}

	public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
	{
		bool inRange = (byEntity.World.Side == EnumAppSide.Client && byEntity.Pos.SquareDistanceTo(entity.Pos) <= 5f) || (byEntity.World.Side == EnumAppSide.Server && byEntity.Pos.SquareDistanceTo(entity.Pos) <= 14f);
		if (!IsHarvested || !inRange)
		{
			return;
		}
		EntityPlayer entityplr = byEntity as EntityPlayer;
		IPlayer player = entity.World.PlayerByUid(entityplr.PlayerUID);
		player.InventoryManager.OpenInventory(inv);
		if (entity.World.Side == EnumAppSide.Client && dlg == null)
		{
			dlg = new GuiDialogCreatureContents(inv, entity, entity.Api as ICoreClientAPI, "carcasscontents");
			if (dlg.TryOpen())
			{
				(entity.World.Api as ICoreClientAPI).Network.SendPacketClient(inv.Open(player));
			}
			dlg.OnClosed += delegate
			{
				dlg.Dispose();
				dlg = null;
			};
		}
	}

	public override void OnReceivedClientPacket(IServerPlayer player, int packetid, byte[] data, ref EnumHandling handled)
	{
		if (packetid < 1000 && inv.HasOpened(player))
		{
			inv.InvNetworkUtil.HandleClientPacket(player, packetid, data);
			handled = EnumHandling.PreventSubsequent;
		}
	}

	public void SetHarvested(IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		if (entity.WatchedAttributes.GetBool("harvested"))
		{
			return;
		}
		entity.WatchedAttributes.SetBool("harvested", value: true);
		if (entity.World.Side != EnumAppSide.Client)
		{
			JsonObject attributes = entity.Properties.Attributes;
			if (attributes == null || !attributes["isMechanical"].AsBool())
			{
				dropQuantityMultiplier *= byPlayer.Entity.Stats.GetBlended("animalLootDropRate");
			}
			generateDrops(byPlayer, dropQuantityMultiplier);
		}
	}

	private void generateDrops(IPlayer byPlayer, float dropQuantityMultiplier)
	{
		List<ItemStack> todrop = new List<ItemStack>();
		for (int j = 0; j < jsonDrops.Length; j++)
		{
			BlockDropItemStack dstack = jsonDrops[j];
			if (dstack.Tool.HasValue && (byPlayer == null || dstack.Tool != byPlayer.InventoryManager.ActiveTool))
			{
				continue;
			}
			dstack.Resolve(entity.World, "BehaviorHarvestable ", entity.Code);
			float extraMul = 1f;
			if (dstack.DropModbyStat != null)
			{
				extraMul = (byPlayer?.Entity?.Stats.GetBlended(dstack.DropModbyStat)).GetValueOrDefault();
			}
			ItemStack stack2 = dstack.GetNextItemStack(this.dropQuantityMultiplier * dropQuantityMultiplier * extraMul);
			if (stack2 == null)
			{
				continue;
			}
			if (stack2.Collectible.NutritionProps != null || stack2.Collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack?.Collectible?.NutritionProps != null)
			{
				float weightedStackSize = (float)stack2.StackSize * AnimalWeight;
				stack2.StackSize = GameMath.RoundRandom(entity.World.Rand, weightedStackSize);
			}
			if (stack2.StackSize != 0)
			{
				if (stack2.Collectible is IResolvableCollectible irc)
				{
					DummySlot slot = new DummySlot(stack2);
					irc.Resolve(slot, entity.World);
					stack2 = slot.Itemstack;
				}
				todrop.Add(stack2);
				if (dstack.LastDrop)
				{
					break;
				}
			}
		}
		entity.GetInterfaces<IHarvestableDrops>()?.ForEach(delegate(IHarvestableDrops hInterface)
		{
			hInterface.GetHarvestableDrops(entity.World, entity.ServerPos.AsBlockPos, byPlayer)?.Foreach(delegate(ItemStack stack)
			{
				todrop.Add(stack);
			});
		});
		inv.AddSlots(todrop.Count - inv.Count);
		ItemStack[] resolvedDrops = todrop.ToArray();
		TreeAttribute tree = new TreeAttribute();
		for (int i = 0; i < resolvedDrops.Length; i++)
		{
			inv[i].Itemstack = resolvedDrops[i];
		}
		inv.ToTreeAttributes(tree);
		entity.WatchedAttributes["harvestableInv"] = tree;
		entity.WatchedAttributes.MarkPathDirty("harvestableInv");
		entity.WatchedAttributes.MarkPathDirty("harvested");
		if (entity.World.Side == EnumAppSide.Server)
		{
			entity.World.BlockAccessor.GetChunkAtBlockPos(entity.ServerPos.AsBlockPos).MarkModified();
		}
	}

	public override WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player, ref EnumHandling handled)
	{
		interactions = ObjectCacheUtil.GetOrCreate(world.Api, "harvestableEntityInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (Item current in world.Items)
			{
				if (!(current.Code == null) && current.Tool == EnumTool.Knife)
				{
					list.Add(new ItemStack(current));
				}
			}
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-creature-harvest",
					MouseButton = EnumMouseButton.Right,
					HotKeyCode = "shift",
					Itemstacks = list.ToArray()
				}
			};
		});
		if (entity.Alive || IsHarvested)
		{
			return null;
		}
		return interactions;
	}

	public override void GetInfoText(StringBuilder infotext)
	{
		if (!entity.Alive)
		{
			if (GotCrushed)
			{
				infotext.AppendLine(Lang.Get("Looks crushed. Won't be able to harvest as much from this carcass."));
			}
			if (GotElectrocuted)
			{
				infotext.AppendLine(Lang.Get("Looks partially charred, perhaps due to a lightning strike."));
			}
			if (GotAcidified)
			{
				infotext.AppendLine(Lang.Get("Looks partially dissolved, likely due to high acidity."));
			}
			string deathByEntityCode = entity.WatchedAttributes.GetString("deathByEntity");
			if (deathByEntityCode != null && !entity.WatchedAttributes.HasAttribute("deathByPlayer"))
			{
				string code = "deadcreature-killed";
				EntityProperties props = entity.World.GetEntityType(new AssetLocation(deathByEntityCode));
				if (props != null)
				{
					JsonObject attributes = props.Attributes;
					if (attributes != null && attributes["killedByInfoText"].Exists)
					{
						code = props.Attributes["killedByInfoText"].AsString();
					}
				}
				infotext.AppendLine(Lang.Get(code));
			}
		}
		if (!fixedWeight)
		{
			if (AnimalWeight >= 0.95f)
			{
				infotext.AppendLine(Lang.Get("creature-weight-good"));
			}
			else if (AnimalWeight >= 0.75f)
			{
				infotext.AppendLine(Lang.Get("creature-weight-ok"));
			}
			else if (AnimalWeight >= 0.5f)
			{
				infotext.AppendLine(Lang.Get("creature-weight-low"));
			}
			else
			{
				infotext.AppendLine(Lang.Get("creature-weight-starving"));
			}
		}
		base.GetInfoText(infotext);
	}

	public override string PropertyName()
	{
		return "harvestable";
	}
}
