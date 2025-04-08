using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class InWorldContainer
{
	protected RoomRegistry roomReg;

	protected Room room;

	protected ICoreAPI Api;

	protected float temperatureCached = -1000f;

	protected PositionProviderDelegate positionProvider;

	protected Action onRequireSyncToClient;

	public InventorySupplierDelegate inventorySupplier;

	private string treeAttrKey;

	private InventoryBase prevInventory;

	private bool didInit;

	public Room Room => room;

	public InventoryBase Inventory => inventorySupplier();

	public InWorldContainer(InventorySupplierDelegate inventorySupplier, string treeAttrKey)
	{
		this.inventorySupplier = inventorySupplier;
		this.treeAttrKey = treeAttrKey;
	}

	public void Init(ICoreAPI Api, PositionProviderDelegate positionProvider, Action onRequireSyncToClient)
	{
		this.Api = Api;
		this.positionProvider = positionProvider;
		this.onRequireSyncToClient = onRequireSyncToClient;
		roomReg = Api.ModLoader.GetModSystem<RoomRegistry>();
		LateInit();
	}

	public void Reset()
	{
		didInit = false;
	}

	public void LateInit()
	{
		if (Inventory == null || didInit)
		{
			return;
		}
		if (prevInventory != null && Inventory != prevInventory)
		{
			prevInventory.OnAcquireTransitionSpeed -= Inventory_OnAcquireTransitionSpeed;
			if (Api.Side == EnumAppSide.Client)
			{
				prevInventory.OnInventoryOpened -= Inventory_OnInventoryOpenedClient;
			}
		}
		didInit = true;
		Inventory.ResolveBlocksOrItems();
		Inventory.OnAcquireTransitionSpeed += Inventory_OnAcquireTransitionSpeed;
		if (Api.Side == EnumAppSide.Client)
		{
			Inventory.OnInventoryOpened += Inventory_OnInventoryOpenedClient;
		}
		prevInventory = Inventory;
	}

	private void Inventory_OnInventoryOpenedClient(IPlayer player)
	{
		OnTick(1f);
	}

	public virtual void OnTick(float dt)
	{
		if (Api.Side == EnumAppSide.Client)
		{
			return;
		}
		temperatureCached = -1000f;
		if (!HasTransitionables())
		{
			return;
		}
		room = roomReg.GetRoomForPosition(positionProvider());
		if (room.AnyChunkUnloaded != 0)
		{
			return;
		}
		foreach (ItemSlot slot in Inventory)
		{
			if (slot.Itemstack != null)
			{
				AssetLocation codeBefore = slot.Itemstack.Collectible.Code;
				slot.Itemstack.Collectible.UpdateAndGetTransitionStates(Api.World, slot);
				if (slot.Itemstack?.Collectible.Code != codeBefore)
				{
					onRequireSyncToClient();
				}
			}
		}
		temperatureCached = -1000f;
	}

	protected virtual bool HasTransitionables()
	{
		foreach (ItemSlot item in Inventory)
		{
			ItemStack stack = item.Itemstack;
			if (stack != null && stack.Collectible.RequiresTransitionableTicking(Api.World, stack))
			{
				return true;
			}
		}
		return false;
	}

	protected virtual float Inventory_OnAcquireTransitionSpeed(EnumTransitionType transType, ItemStack stack, float baseMul)
	{
		float positionAwarePerishRate = ((Api != null && transType == EnumTransitionType.Perish) ? GetPerishRate() : 1f);
		if (transType == EnumTransitionType.Dry || transType == EnumTransitionType.Melt)
		{
			positionAwarePerishRate = 0.25f;
		}
		return baseMul * positionAwarePerishRate;
	}

	public virtual float GetPerishRate()
	{
		BlockPos sealevelpos = positionProvider().Copy();
		sealevelpos.Y = Api.World.SeaLevel;
		float temperature = temperatureCached;
		if (temperature < -999f)
		{
			temperature = Api.World.BlockAccessor.GetClimateAt(sealevelpos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, Api.World.Calendar.TotalDays).Temperature;
			if (Api.Side == EnumAppSide.Server)
			{
				temperatureCached = temperature;
			}
		}
		if (room == null)
		{
			room = roomReg.GetRoomForPosition(positionProvider());
		}
		float soilTempWeight = 0f;
		float skyLightProportion = (float)room.SkylightCount / (float)Math.Max(1, room.SkylightCount + room.NonSkylightCount);
		if (room.IsSmallRoom)
		{
			soilTempWeight = 1f;
			soilTempWeight -= 0.4f * skyLightProportion;
			soilTempWeight -= 0.5f * GameMath.Clamp((float)room.NonCoolingWallCount / (float)Math.Max(1, room.CoolingWallCount), 0f, 1f);
		}
		int lightlevel = Api.World.BlockAccessor.GetLightLevel(positionProvider(), EnumLightLevelType.OnlySunLight);
		float lightImportance = 0.1f;
		lightImportance = (room.IsSmallRoom ? (lightImportance + (0.3f * soilTempWeight + 1.75f * skyLightProportion)) : ((!((float)room.ExitCount <= 0.1f * (float)(room.CoolingWallCount + room.NonCoolingWallCount))) ? (lightImportance + 0.5f * skyLightProportion) : (lightImportance + 1.25f * skyLightProportion)));
		lightImportance = GameMath.Clamp(lightImportance, 0f, 1.5f);
		float airTemp = temperature + (float)GameMath.Clamp(lightlevel - 11, 0, 10) * lightImportance;
		float cellarTemp = 5f;
		float hereTemp = GameMath.Lerp(airTemp, cellarTemp, soilTempWeight);
		hereTemp = Math.Min(hereTemp, airTemp);
		return Math.Max(0.1f, Math.Min(2.4f, (float)Math.Pow(3.0, (double)(hereTemp / 19f) - 1.2) - 0.1f));
	}

	public void ReloadRoom()
	{
		room = roomReg.GetRoomForPosition(positionProvider());
	}

	public void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
	{
		foreach (ItemSlot slot in Inventory)
		{
			slot.Itemstack?.Collectible.OnStoreCollectibleMappings(Api.World, slot, blockIdMapping, itemIdMapping);
		}
	}

	public void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
	{
		foreach (ItemSlot slot in Inventory)
		{
			if (slot.Itemstack != null)
			{
				if (!slot.Itemstack.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
				{
					slot.Itemstack = null;
				}
				else
				{
					slot.Itemstack.Collectible.OnLoadCollectibleMappings(worldForResolve, slot, oldBlockIdMapping, oldItemIdMapping, resolveImports);
				}
				if (slot.Itemstack?.Collectible is IResolvableCollectible resolvable)
				{
					resolvable.Resolve(slot, worldForResolve, resolveImports);
				}
			}
		}
	}

	public void ToTreeAttributes(ITreeAttribute tree)
	{
		if (Inventory != null)
		{
			ITreeAttribute invtree = new TreeAttribute();
			Inventory.ToTreeAttributes(invtree);
			tree[treeAttrKey] = invtree;
		}
	}

	public void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		Inventory.FromTreeAttributes(tree.GetTreeAttribute(treeAttrKey));
	}
}
