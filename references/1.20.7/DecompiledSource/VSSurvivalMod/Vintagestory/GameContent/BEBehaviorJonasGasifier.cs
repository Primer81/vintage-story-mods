using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BEBehaviorJonasGasifier : BlockEntityBehavior, INetworkedLight
{
	private ControlPoint cp;

	private bool lit;

	private ModSystemControlPoints modSys;

	private string networkCode;

	private InventoryGeneric inventory;

	public double burnStartTotalHours;

	public bool HasFuel => !inventory[0].Empty;

	public bool Lit
	{
		get
		{
			return lit;
		}
		set
		{
			lit = value;
		}
	}

	public BEBehaviorJonasGasifier(BlockEntity blockentity)
		: base(blockentity)
	{
		inventory = new InventoryGeneric(1, null, null);
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		Api = api;
		registerToControlPoint();
		inventory.LateInitialize("jonasgasifier-" + base.Pos.X + "/" + base.Pos.Y + "/" + base.Pos.Z, api);
		inventory.Pos = base.Pos;
		inventory.ResolveBlocksOrItems();
		Blockentity.RegisterGameTickListener(onTick, 2001, 12);
		base.Initialize(api, properties);
	}

	private void onTick(float dt)
	{
		if (!lit)
		{
			return;
		}
		double hoursPassed = Math.Min(2400.0, Api.World.Calendar.TotalHours - burnStartTotalHours);
		while (hoursPassed > 8.0)
		{
			burnStartTotalHours += 8.0;
			inventory[0].TakeOut(1);
			if (inventory.Empty)
			{
				lit = false;
				UpdateState();
				break;
			}
		}
	}

	public void setNetwork(string networkCode)
	{
		this.networkCode = networkCode;
		registerToControlPoint();
		Blockentity.MarkDirty(redrawOnClient: true);
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		base.OnBlockPlaced(byItemStack);
		lit = false;
	}

	private void registerToControlPoint()
	{
		if (networkCode != null)
		{
			modSys = Api.ModLoader.GetModSystem<ModSystemControlPoints>();
			AssetLocation controlpointcode = AssetLocation.Create(networkCode, base.Block.Code.Domain);
			cp = modSys[controlpointcode];
			cp.ControlData = lit;
		}
	}

	internal void Interact(IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (!slot.Empty && slot.Itemstack.Collectible.CombustibleProps != null && slot.Itemstack.Collectible.CombustibleProps.BurnTemperature >= 1100 && slot.TryPutInto(Api.World, inventory[0]) > 0)
		{
			Api.World.PlaySoundAt(new AssetLocation("sounds/block/charcoal"), base.Pos, 0.0, byPlayer);
			(byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			slot.MarkDirty();
			Blockentity.MarkDirty(redrawOnClient: true);
		}
	}

	public void UpdateState()
	{
		if (cp != null)
		{
			cp.ControlData = lit;
			cp.Trigger();
		}
		Blockentity.MarkDirty(redrawOnClient: true);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		lit = tree.GetBool("lit");
		networkCode = tree.GetString("networkCode");
		if (networkCode == "")
		{
			networkCode = null;
		}
		burnStartTotalHours = tree.GetDouble("burnStartTotalHours");
		inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBool("lit", lit);
		tree.SetString("networkCode", networkCode);
		tree.SetDouble("burnStartTotalHours", burnStartTotalHours);
		if (inventory != null)
		{
			ITreeAttribute invtree = new TreeAttribute();
			inventory.ToTreeAttributes(invtree);
			tree["inventory"] = invtree;
		}
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
		if (Api.World.Side == EnumAppSide.Server)
		{
			inventory.DropAll(base.Pos.ToVec3d().Add(0.5, 0.5, 0.5));
		}
		base.OnBlockBroken(byPlayer);
	}

	public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
	{
		foreach (ItemSlot slot in inventory)
		{
			slot.Itemstack?.Collectible.OnStoreCollectibleMappings(Api.World, slot, blockIdMapping, itemIdMapping);
		}
	}

	public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
	{
		foreach (ItemSlot slot in inventory)
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
			}
		}
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		base.GetBlockInfo(forPlayer, dsc);
		if (!inventory[0].Empty)
		{
			dsc.AppendLine(Lang.Get("Contents: {0}x {1}", inventory[0].StackSize, inventory[0].GetStackName()));
		}
		if (Api is ICoreClientAPI capi && capi.Settings.Bool["extendedDebugInfo"])
		{
			dsc.AppendLine("network code: " + networkCode);
			dsc.AppendLine(lit ? "On" : "Off");
		}
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (HasFuel)
		{
			mesher.AddMeshData(genMesh(new AssetLocation("shapes/block/machine/jonas/gasifier-coal" + (lit ? "-lit" : "") + ".json")));
		}
		return base.OnTesselation(mesher, tessThreadTesselator);
	}

	private MeshData genMesh(AssetLocation assetLocation)
	{
		return ObjectCacheUtil.GetOrCreate(Api, "gasifiermesh-" + assetLocation.Path + "-" + base.Block.Shape.rotateY, delegate
		{
			Shape shape = Api.Assets.TryGet(assetLocation).ToObject<Shape>();
			(Api as ICoreClientAPI).Tesselator.TesselateShape(base.Block, shape, out var modeldata, base.Block.Shape.RotateXYZCopy);
			return modeldata;
		});
	}
}
