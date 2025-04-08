using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockEntityBoiler : BlockEntityLiquidContainer, IFirePit
{
	private MeshData firepitMesh;

	public int firepitStage;

	private double lastTickTotalHours;

	public float fuelHours;

	private float distillationAccum;

	public static AssetLocation[] firepitShapeBlockCodes = new AssetLocation[8]
	{
		null,
		new AssetLocation("firepit-construct1"),
		new AssetLocation("firepit-construct2"),
		new AssetLocation("firepit-construct3"),
		new AssetLocation("firepit-construct4"),
		new AssetLocation("firepit-cold"),
		new AssetLocation("firepit-lit"),
		new AssetLocation("firepit-extinct")
	};

	public override string InventoryClassName => "boiler";

	public virtual float SoundLevel => 0.66f;

	public bool IsBurning
	{
		get
		{
			if (firepitStage == 6)
			{
				return fuelHours > 0f;
			}
			return false;
		}
	}

	public bool IsSmoldering
	{
		get
		{
			if (firepitStage == 6)
			{
				return fuelHours > -3f;
			}
			return false;
		}
	}

	public float InputStackTemp
	{
		get
		{
			return InputStack?.Collectible.GetTemperature(Api.World, inventory[0].Itemstack) ?? 0f;
		}
		set
		{
			InputStack.Collectible.SetTemperature(Api.World, inventory[0].Itemstack, value);
		}
	}

	public DistillationProps DistProps => InputStack?.ItemAttributes?["distillationProps"].AsObject<DistillationProps>();

	public ItemStack InputStack => inventory[0]?.Itemstack;

	public BlockEntityBoiler()
	{
		inventory = new InventoryGeneric(1, null, null);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		RegisterGameTickListener(onBurnTick, 100);
		loadMesh();
		if (firepitStage == 6 && IsBurning)
		{
			GetBehavior<BEBehaviorFirepitAmbient>()?.ToggleAmbientSounds(on: true);
		}
	}

	private void onBurnTick(float dt)
	{
		if (firepitStage == 6 && !IsBurning)
		{
			GetBehavior<BEBehaviorFirepitAmbient>()?.ToggleAmbientSounds(on: false);
			firepitStage++;
			MarkDirty(redrawOnClient: true);
		}
		if (IsBurning)
		{
			heatLiquid(dt);
		}
		double dh = Api.World.Calendar.TotalHours - lastTickTotalHours;
		if (dh > 0.10000000149011612)
		{
			if (IsBurning)
			{
				fuelHours -= (float)dh;
			}
			lastTickTotalHours = Api.World.Calendar.TotalHours;
		}
		DistillationProps props = DistProps;
		if (!(InputStackTemp >= 75f) || props == null)
		{
			return;
		}
		distillationAccum += dt * props.Ratio;
		if (!(distillationAccum >= 0.2f))
		{
			return;
		}
		distillationAccum -= 0.2f;
		for (int i = 0; i < 4; i++)
		{
			if (Api.World.BlockAccessor.GetBlockEntity(Pos.AddCopy(BlockFacing.HORIZONTALS[i])) is BlockEntityCondenser becd)
			{
				props?.DistilledStack.Resolve(Api.World, "distillationprops");
				if (becd.ReceiveDistillate(inventory[0], props))
				{
					break;
				}
			}
		}
	}

	public void heatLiquid(float dt)
	{
		if (!inventory[0].Empty && InputStackTemp < 100f)
		{
			InputStackTemp += dt * 2f;
		}
	}

	public bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		bool addGrass = hotbarSlot.Itemstack?.Collectible is ItemDryGrass && firepitStage == 0;
		bool addFireWood = hotbarSlot.Itemstack?.Collectible is ItemFirewood && firepitStage >= 1 && firepitStage <= 4;
		bool reignite = hotbarSlot.Itemstack?.Collectible is ItemFirewood && firepitStage >= 5 && fuelHours <= 6f;
		if (addGrass || addFireWood || reignite)
		{
			if (!reignite)
			{
				firepitStage++;
			}
			else if (firepitStage == 7)
			{
				firepitStage = 5;
			}
			MarkDirty(redrawOnClient: true);
			hotbarSlot.TakeOut(1);
			(byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			Block block = Api.World.GetBlock(firepitShapeBlockCodes[firepitStage]);
			if (block?.Sounds != null)
			{
				Api.World.PlaySoundAt(block.Sounds.Place, Pos, 0.0, byPlayer);
			}
		}
		if (addGrass)
		{
			return true;
		}
		if (addFireWood || reignite)
		{
			fuelHours = Math.Max(2f, fuelHours + 2f);
			return true;
		}
		return false;
	}

	public bool CanIgnite()
	{
		return firepitStage == 5;
	}

	public void TryIgnite()
	{
		if (CanIgnite())
		{
			firepitStage++;
			GetBehavior<BEBehaviorFirepitAmbient>()?.ToggleAmbientSounds(on: true);
			MarkDirty(redrawOnClient: true);
			lastTickTotalHours = Api.World.Calendar.TotalHours;
		}
	}

	private void loadMesh()
	{
		if (Api.Side != EnumAppSide.Server)
		{
			if (firepitStage <= 0)
			{
				firepitMesh = null;
				return;
			}
			Block block = Api.World.GetBlock(firepitShapeBlockCodes[firepitStage]);
			ICoreClientAPI capi = Api as ICoreClientAPI;
			firepitMesh = capi.TesselatorManager.GetDefaultBlockMesh(block);
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		firepitStage = tree.GetInt("firepitConstructionStage");
		lastTickTotalHours = tree.GetDouble("lastTickTotalHours");
		fuelHours = tree.GetFloat("fuelHours");
		if (Api != null)
		{
			loadMesh();
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetInt("firepitConstructionStage", firepitStage);
		tree.SetDouble("lastTickTotalHours", lastTickTotalHours);
		tree.SetFloat("fuelHours", fuelHours);
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		mesher.AddMeshData(firepitMesh);
		return base.OnTesselation(mesher, tessThreadTesselator);
	}
}
