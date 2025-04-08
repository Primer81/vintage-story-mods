using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class EntityBehaviorPlayerInventory : EntityBehaviorTexturedClothing
{
	private bool slotModifiedRegistered;

	private float accum;

	private IPlayer Player => (entity as EntityPlayer).Player;

	public override InventoryBase Inventory => Player?.InventoryManager.GetOwnInventory("character") as InventoryBase;

	public override string InventoryClassName => "gear";

	public override string PropertyName()
	{
		return "playerinventory";
	}

	public EntityBehaviorPlayerInventory(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		base.OnEntityDespawn(despawn);
		IInventory inv = Player?.InventoryManager.GetOwnInventory("backpack");
		if (inv != null)
		{
			inv.SlotModified -= base.Inventory_SlotModifiedBackpack;
		}
	}

	protected override void loadInv()
	{
	}

	public override void storeInv()
	{
	}

	public override void OnGameTick(float deltaTime)
	{
		if (!slotModifiedRegistered)
		{
			slotModifiedRegistered = true;
			IInventory inv = Player?.InventoryManager.GetOwnInventory("backpack");
			if (inv != null)
			{
				inv.SlotModified += base.Inventory_SlotModifiedBackpack;
			}
		}
		base.OnGameTick(deltaTime);
		accum += deltaTime;
		if (accum > 1f)
		{
			entity.Attributes.SetBool("hasProtectiveEyeGear", Inventory != null && Inventory.FirstOrDefault((ItemSlot slot) => !slot.Empty && (slot.Itemstack.Collectible.Attributes?.IsTrue("eyeprotective") ?? false)) != null);
		}
	}

	public override void OnTesselation(ref Shape entityShape, string shapePathForLogging, ref bool shapeIsCloned, ref string[] willDeleteElements)
	{
		IInventory backPackInv = Player?.InventoryManager.GetOwnInventory("backpack");
		Dictionary<string, ItemSlot> uniqueGear = new Dictionary<string, ItemSlot>();
		int i = 0;
		while (backPackInv != null && i < 4)
		{
			ItemSlot slot = backPackInv[i];
			if (!slot.Empty)
			{
				uniqueGear[slot.Itemstack.Class.ToString() + slot.Itemstack.Collectible.Id] = slot;
			}
			i++;
		}
		foreach (KeyValuePair<string, ItemSlot> val in uniqueGear)
		{
			entityShape = addGearToShape(entityShape, val.Value, "default", shapePathForLogging, ref shapeIsCloned, ref willDeleteElements);
		}
		base.OnTesselation(ref entityShape, shapePathForLogging, ref shapeIsCloned, ref willDeleteElements);
	}

	public override void OnEntityDeath(DamageSource damageSourceForDeath)
	{
		Api.Event.EnqueueMainThreadTask(delegate
		{
			EntityServerProperties server = entity.Properties.Server;
			if (server == null || !(server.Attributes?.GetBool("keepContents")).GetValueOrDefault())
			{
				Player.InventoryManager.OnDeath();
			}
			EntityServerProperties server2 = entity.Properties.Server;
			if (server2 != null && (server2.Attributes?.GetBool("dropArmorOnDeath")).GetValueOrDefault())
			{
				foreach (ItemSlot current in Inventory)
				{
					if (!current.Empty)
					{
						JsonObject itemAttributes = current.Itemstack.ItemAttributes;
						if (itemAttributes != null && itemAttributes["protectionModifiers"].Exists)
						{
							Api.World.SpawnItemEntity(current.Itemstack, entity.ServerPos.XYZ);
							current.Itemstack = null;
							current.MarkDirty();
						}
					}
				}
			}
		}, "dropinventoryondeath");
	}
}
