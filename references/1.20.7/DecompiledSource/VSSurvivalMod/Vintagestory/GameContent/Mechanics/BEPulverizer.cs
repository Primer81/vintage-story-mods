using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent.Mechanics;

public class BEPulverizer : BlockEntityDisplay
{
	private readonly AssetLocation pounderName = new AssetLocation("pounder-oak");

	private readonly AssetLocation toggleName = new AssetLocation("pulverizertoggle-oak");

	public Vec4f lightRbs = new Vec4f();

	private float rotateY;

	private InventoryPulverizer inv;

	internal Matrixf mat = new Matrixf();

	private BEBehaviorMPPulverizer pvBh;

	public bool hasAxle;

	public bool hasLPounder;

	public bool hasRPounder;

	public int CapMetalTierL;

	public int CapMetalTierR;

	public int CapMetalIndexL;

	public int CapMetalIndexR;

	private float accumLeft;

	private float accumRight;

	public BlockFacing Facing { get; protected set; } = BlockFacing.NORTH;


	public virtual Vec4f LightRgba => lightRbs;

	public override InventoryBase Inventory => inv;

	public override string InventoryClassName => "pulverizer";

	public bool hasPounderCaps => !inv[2].Empty;

	public bool IsComplete
	{
		get
		{
			if (hasAxle && hasLPounder && hasRPounder)
			{
				return hasPounderCaps;
			}
			return false;
		}
	}

	public override int DisplayedItems => 2;

	public BEPulverizer()
	{
		inv = new InventoryPulverizer(this, 3);
		inv.SlotModified += Inv_SlotModified;
	}

	private void Inv_SlotModified(int t1)
	{
		updateMeshes();
	}

	public override void Initialize(ICoreAPI api)
	{
		Facing = BlockFacing.FromCode(base.Block.Variant["side"]);
		if (Facing == null)
		{
			Facing = BlockFacing.NORTH;
		}
		switch (Facing.Index)
		{
		case 0:
			rotateY = 180f;
			break;
		case 1:
			rotateY = 90f;
			break;
		case 3:
			rotateY = 270f;
			break;
		}
		mat.Translate(0.5f, 0.5f, 0.5f);
		mat.RotateYDeg(rotateY);
		mat.Translate(-0.5f, -0.5f, -0.5f);
		base.Initialize(api);
		inv.LateInitialize(InventoryClassName + "-" + Pos, api);
		if (api.World.Side == EnumAppSide.Server)
		{
			RegisterGameTickListener(OnServerTick, 200);
		}
		pvBh = GetBehavior<BEBehaviorMPPulverizer>();
	}

	private void OnServerTick(float dt)
	{
		if (!IsComplete)
		{
			return;
		}
		float nwspeed = pvBh.Network?.Speed ?? 0f;
		nwspeed = Math.Abs(nwspeed * 3f) * pvBh.GearedRatio;
		if (!inv[0].Empty)
		{
			accumLeft += dt * nwspeed;
			if (accumLeft > 5f)
			{
				accumLeft = 0f;
				Crush(0, CapMetalTierL, -0.25);
			}
		}
		if (!inv[1].Empty)
		{
			accumRight += dt * nwspeed;
			if (accumRight > 5f)
			{
				accumRight = 0f;
				Crush(1, CapMetalTierR, 0.25);
			}
		}
	}

	private void Crush(int slot, int capTier, double xOffset)
	{
		ItemStack inputStack = inv[slot].TakeOut(1);
		CrushingProperties props = inputStack.Collectible.CrushingProps;
		ItemStack outputStack = null;
		if (props != null)
		{
			outputStack = props.CrushedStack?.ResolvedItemstack.Clone();
			if (outputStack != null)
			{
				outputStack.StackSize = GameMath.RoundRandom(Api.World.Rand, props.Quantity.nextFloat(outputStack.StackSize, Api.World.Rand));
			}
			if (outputStack.StackSize <= 0)
			{
				return;
			}
		}
		Vec3d position = mat.TransformVector(new Vec4d(xOffset * 0.999, 0.1, 0.8, 0.0)).XYZ.Add(Pos).Add(0.5, 0.0, 0.5);
		double lengthways = Api.World.Rand.NextDouble() * 0.07 - 0.035;
		double sideways = Api.World.Rand.NextDouble() * 0.03 - 0.005;
		Vec3d velocity = new Vec3d((Facing.Axis == EnumAxis.Z) ? sideways : lengthways, Api.World.Rand.NextDouble() * 0.02 - 0.01, (Facing.Axis == EnumAxis.Z) ? lengthways : sideways);
		bool tierPassed = outputStack != null && inputStack.Collectible.CrushingProps.HardnessTier <= capTier;
		Api.World.SpawnItemEntity(tierPassed ? outputStack : inputStack, position, velocity);
		MarkDirty(redrawOnClient: true);
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		base.OnTesselation(mesher, tesselator);
		ICoreClientAPI capi = Api as ICoreClientAPI;
		MeshData meshTop = ObjectCacheUtil.GetOrCreate(capi, "pulverizertopmesh-" + rotateY, delegate
		{
			Shape shape2 = Shape.TryGet(capi, "shapes/block/wood/mechanics/pulverizer-top.json");
			capi.Tesselator.TesselateShape(base.Block, shape2, out var modeldata2, new Vec3f(0f, rotateY, 0f));
			return modeldata2;
		});
		MeshData meshBase = ObjectCacheUtil.GetOrCreate(capi, "pulverizerbasemesh-" + rotateY, delegate
		{
			Shape shape = Shape.TryGet(capi, "shapes/block/wood/mechanics/pulverizer-base.json");
			capi.Tesselator.TesselateShape(base.Block, shape, out var modeldata, new Vec3f(0f, rotateY, 0f));
			return modeldata;
		});
		mesher.AddMeshData(meshTop);
		mesher.AddMeshData(meshBase);
		for (int i = 0; i < Behaviors.Count; i++)
		{
			Behaviors[i].OnTesselation(mesher, tesselator);
		}
		return true;
	}

	public ItemStack[] getDrops(IWorldAccessor world, ItemStack pulvFrame)
	{
		int pounders = 0;
		if (hasLPounder)
		{
			pounders++;
		}
		if (hasRPounder)
		{
			pounders++;
		}
		ItemStack[] result = new ItemStack[pounders + ((!hasAxle) ? 1 : 2)];
		int index = 0;
		result[index++] = pulvFrame;
		for (int i = 0; i < pounders; i++)
		{
			result[index++] = new ItemStack(world.GetItem(pounderName));
		}
		if (hasAxle)
		{
			result[index] = new ItemStack(world.GetItem(toggleName));
		}
		return result;
	}

	public bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot handslot = byPlayer.InventoryManager.ActiveHotbarSlot;
		Vec4d vec = new Vec4d(blockSel.HitPosition.X, blockSel.HitPosition.Y, blockSel.HitPosition.Z, 1.0);
		Vec4d vec4d = mat.TransformVector(vec);
		int a = ((Facing.Axis == EnumAxis.Z) ? 1 : 0);
		ItemSlot targetSlot = ((vec4d.X < 0.5) ? inv[a] : inv[1 - a]);
		if (handslot.Empty)
		{
			TryTake(targetSlot, byPlayer);
		}
		else
		{
			if (TryAddPart(handslot, byPlayer))
			{
				Api.World.PlaySoundAt(base.Block.Sounds.Place, Pos, -0.25, byPlayer);
				(Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
				return true;
			}
			if (handslot.Itemstack.Collectible.CrushingProps != null)
			{
				TryPut(handslot, targetSlot);
			}
		}
		return true;
	}

	private bool TryAddPart(ItemSlot slot, IPlayer toPlayer)
	{
		if (!hasAxle && slot.Itemstack.Collectible.Code.Path == "pulverizertoggle-oak")
		{
			if (toPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
			{
				slot.TakeOut(1);
				slot.MarkDirty();
			}
			hasAxle = true;
			MarkDirty(redrawOnClient: true);
			return true;
		}
		if ((!hasLPounder || !hasRPounder) && slot.Itemstack.Collectible.Code.Path == "pounder-oak")
		{
			if (toPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
			{
				slot.TakeOut(1);
				slot.MarkDirty();
			}
			if (hasLPounder)
			{
				hasRPounder = true;
			}
			hasLPounder = true;
			MarkDirty(redrawOnClient: true);
			return true;
		}
		if (slot.Itemstack.Collectible.FirstCodePart() == "poundercap")
		{
			if (hasLPounder && hasRPounder)
			{
				if (slot.Itemstack.StackSize < 2)
				{
					(Api as ICoreClientAPI)?.TriggerIngameError(this, "require2caps", Lang.Get("Please add 2 caps at the same time!"));
					return true;
				}
				ItemStack stack = slot.TakeOut(2);
				if (!inv[2].Empty && !toPlayer.InventoryManager.TryGiveItemstack(inv[2].Itemstack, slotNotifyEffect: true))
				{
					Api.World.SpawnItemEntity(inv[2].Itemstack, Pos);
				}
				inv[2].Itemstack = stack;
				slot.MarkDirty();
				MarkDirty(redrawOnClient: true);
			}
			else
			{
				(Api as ICoreClientAPI)?.TriggerIngameError(this, "require2pounders", Lang.Get("Please add pounders before adding caps!"));
			}
			return true;
		}
		return false;
	}

	private void TryPut(ItemSlot fromSlot, ItemSlot intoSlot)
	{
		if (fromSlot.TryPutInto(Api.World, intoSlot) > 0)
		{
			fromSlot.MarkDirty();
			MarkDirty(redrawOnClient: true);
		}
	}

	private void TryTake(ItemSlot fromSlot, IPlayer toPlayer)
	{
		ItemStack stack = fromSlot.TakeOut(1);
		if (!toPlayer.InventoryManager.TryGiveItemstack(stack))
		{
			Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.1, 0.5));
		}
		MarkDirty(redrawOnClient: true);
	}

	public override void updateMeshes()
	{
		string metal = "nometal";
		if (!inv[2].Empty)
		{
			metal = inv[2].Itemstack.Collectible.Variant["metal"];
		}
		MetalPropertyVariant metalvar = null;
		if (metal != null)
		{
			Api.ModLoader.GetModSystem<SurvivalCoreSystem>().metalsByCode.TryGetValue(metal, out metalvar);
		}
		CapMetalTierL = (CapMetalTierR = Math.Max(metalvar?.Tier ?? 0, 0));
		CapMetalIndexL = (CapMetalIndexR = Math.Max(0, PulverizerRenderer.metals.IndexOf(metal)));
		base.updateMeshes();
	}

	protected override float[][] genTransformationMatrices()
	{
		float[][] tfMatrices = new float[2][];
		for (int index = 0; index < 2; index++)
		{
			float x = ((index % 2 == 0) ? (23f / 32f) : (9f / 32f));
			Matrixf mat = new Matrixf().Set(this.mat.Values);
			mat.Translate(x - 0.5f, 0.25f, -9f / 32f);
			mat.Translate(0.5f, 0f, 0.5f);
			mat.Scale(0.6f, 0.6f, 0.6f);
			mat.Translate(-0.5f, 0f, -0.5f);
			tfMatrices[index] = mat.Values;
		}
		return tfMatrices;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		hasLPounder = tree.GetBool("hasLPounder");
		hasRPounder = tree.GetBool("hasRPounder");
		hasAxle = tree.GetBool("hasAxle");
		RedrawAfterReceivingTreeAttributes(worldAccessForResolve);
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBool("hasLPounder", hasLPounder);
		tree.SetBool("hasRPounder", hasRPounder);
		tree.SetBool("hasAxle", hasAxle);
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
	{
		sb.AppendLine(Lang.Get("Pulverizing:"));
		bool empty = true;
		for (int i = 0; i < 2; i++)
		{
			if (!inv[i].Empty)
			{
				empty = false;
				sb.AppendLine("  " + inv[i].StackSize + " x " + inv[i].GetStackName());
			}
		}
		if (empty)
		{
			sb.AppendLine("  " + Lang.Get("nothing"));
		}
	}
}
