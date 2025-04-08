using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityBasketTrap : BlockEntityDisplay, IAnimalFoodSource, IPointOfInterest
{
	protected ICoreServerAPI sapi;

	private InventoryGeneric inv;

	private AssetLocation destroyedShapeLoc;

	private AssetLocation trappedShapeLoc;

	public EnumTrapState TrapState;

	private float rotationYDeg;

	private float[] rotMat;

	public override InventoryBase Inventory => inv;

	public override string InventoryClassName => "baskettrap";

	public override int DisplayedItems => (TrapState == EnumTrapState.Ready) ? 1 : 0;

	public override string AttributeTransformCode => "baskettrap";

	private BlockEntityAnimationUtil animUtil => GetBehavior<BEBehaviorAnimatable>().animUtil;

	public Vec3d Position => Pos.ToVec3d().Add(0.5, 0.25, 0.5);

	public string Type
	{
		get
		{
			if (!inv.Empty)
			{
				return "food";
			}
			return "nothing";
		}
	}

	public float RotationYDeg
	{
		get
		{
			return rotationYDeg;
		}
		set
		{
			rotationYDeg = value;
			rotMat = Matrixf.Create().Translate(0.5f, 0f, 0.5f).RotateYDeg(rotationYDeg - 90f)
				.Translate(-0.5f, 0f, -0.5f)
				.Values;
		}
	}

	public BlockEntityBasketTrap()
	{
		inv = new InventoryGeneric(1, null, null);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		inv.LateInitialize("baskettrap-" + Pos, api);
		destroyedShapeLoc = AssetLocation.Create(base.Block.Attributes["destroyedShape"].AsString(), base.Block.Code.Domain).WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
		trappedShapeLoc = AssetLocation.Create(base.Block.Attributes["trappedShape"].AsString(), base.Block.Code.Domain).WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
		sapi = api as ICoreServerAPI;
		if (api.Side == EnumAppSide.Client)
		{
			RegisterGameTickListener(OnClientTick, 1000);
			animUtil?.InitializeAnimator("baskettrap", null, null, new Vec3f(0f, rotationYDeg, 0f));
			if (TrapState == EnumTrapState.Trapped)
			{
				animUtil?.StartAnimation(new AnimationMetaData
				{
					Animation = "triggered",
					Code = "triggered"
				});
			}
		}
		else
		{
			sapi.ModLoader.GetModSystem<POIRegistry>().AddPOI(this);
		}
	}

	private void OnClientTick(float dt)
	{
		if (TrapState == EnumTrapState.Trapped && !inv.Empty && Api.World.Rand.NextDouble() > 0.8 && BlockBehaviorCreatureContainer.GetStillAliveDays(Api.World, inv[0].Itemstack) > 0.0 && animUtil.activeAnimationsByAnimCode.Count < 2)
		{
			string anim = ((Api.World.Rand.NextDouble() > 0.5) ? "hopshake" : "shaking");
			animUtil?.StartAnimation(new AnimationMetaData
			{
				Animation = anim,
				Code = anim
			});
			Api.World.PlaySoundAt(new AssetLocation("sounds/block/reedtrapshake*"), Pos, -0.25, null, randomizePitch: true, 16f);
		}
	}

	public bool Interact(IPlayer player, BlockSelection blockSel)
	{
		if (TrapState == EnumTrapState.Ready || TrapState == EnumTrapState.Destroyed)
		{
			return true;
		}
		if (inv[0].Empty)
		{
			ItemStack stack = new ItemStack(base.Block);
			if (TrapState == EnumTrapState.Empty)
			{
				tryReadyTrap(player);
			}
			else
			{
				if (!player.InventoryManager.ActiveHotbarSlot.Empty)
				{
					return true;
				}
				if (!player.InventoryManager.TryGiveItemstack(stack))
				{
					Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.2, 0.5));
				}
				Api.World.BlockAccessor.SetBlock(0, Pos);
				Api.World.Logger.Audit("{0} Took 1x{1} at {2}.", player.PlayerName, stack.Collectible.Code, blockSel.Position);
			}
		}
		else
		{
			if (!player.InventoryManager.TryGiveItemstack(inv[0].Itemstack))
			{
				Api.World.SpawnItemEntity(inv[0].Itemstack, Pos.ToVec3d().Add(0.5, 0.2, 0.5));
			}
			Api.World.BlockAccessor.SetBlock(0, Pos);
			Api.World.Logger.Audit("{0} Took 1x{1} with {2} at {3}.", player.PlayerName, inv[0].Itemstack.Collectible.Code, inv[0].Itemstack.Attributes.GetString("creaturecode"), blockSel.Position);
		}
		return true;
	}

	private void tryReadyTrap(IPlayer player)
	{
		ItemSlot heldSlot = player.InventoryManager.ActiveHotbarSlot;
		if (heldSlot.Empty)
		{
			return;
		}
		CollectibleObject collobj = heldSlot?.Itemstack.Collectible;
		if (heldSlot.Empty)
		{
			return;
		}
		if (collobj.NutritionProps == null)
		{
			JsonObject attributes = collobj.Attributes;
			if (attributes == null || !attributes["foodTags"].Exists)
			{
				return;
			}
		}
		TrapState = EnumTrapState.Ready;
		inv[0].Itemstack = heldSlot.TakeOut(1);
		heldSlot.MarkDirty();
		MarkDirty(redrawOnClient: true);
	}

	public bool IsSuitableFor(Entity entity, CreatureDiet diet)
	{
		if (TrapState != EnumTrapState.Ready)
		{
			return false;
		}
		if (inv[0]?.Itemstack == null || diet == null)
		{
			return false;
		}
		bool num = entity != null && (entity.Properties?.Attributes?.IsTrue("basketCatchable")).GetValueOrDefault();
		bool dietMatches = diet.Matches(inv[0].Itemstack);
		return num && dietMatches;
	}

	public float ConsumeOnePortion(Entity entity)
	{
		sapi.Event.EnqueueMainThreadTask(delegate
		{
			TrapAnimal(entity);
		}, "trapanimal");
		return 1f;
	}

	private void TrapAnimal(Entity entity)
	{
		animUtil?.StartAnimation(new AnimationMetaData
		{
			Animation = "triggered",
			Code = "triggered"
		});
		float trapChance = entity.Properties.Attributes["trapChance"].AsFloat(0.5f);
		if (Api.World.Rand.NextDouble() < (double)trapChance)
		{
			JsonItemStack jstack = base.Block.Attributes["creatureContainer"].AsObject<JsonItemStack>();
			jstack.Resolve(Api.World, "creature container of " + base.Block.Code);
			inv[0].Itemstack = jstack.ResolvedItemstack;
			BlockBehaviorCreatureContainer.CatchCreature(inv[0], entity);
		}
		else
		{
			inv[0].Itemstack = null;
			float trapDestroyChance = entity.Properties.Attributes["trapDestroyChance"].AsFloat();
			if (Api.World.Rand.NextDouble() < (double)trapDestroyChance)
			{
				TrapState = EnumTrapState.Destroyed;
				MarkDirty(redrawOnClient: true);
				Api.World.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), Pos, -0.25, null, randomizePitch: false, 16f);
				return;
			}
		}
		TrapState = EnumTrapState.Trapped;
		MarkDirty(redrawOnClient: true);
		Api.World.PlaySoundAt(new AssetLocation("sounds/block/reedtrapshut"), Pos, -0.25, null, randomizePitch: false, 16f);
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

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		TrapState = (EnumTrapState)tree.GetInt("trapState");
		RotationYDeg = tree.GetFloat("rotationYDeg");
		if (TrapState == EnumTrapState.Trapped)
		{
			animUtil?.StartAnimation(new AnimationMetaData
			{
				Animation = "triggered",
				Code = "triggered"
			});
		}
		RedrawAfterReceivingTreeAttributes(worldForResolving);
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetInt("trapState", (int)TrapState);
		tree.SetFloat("rotationYDeg", rotationYDeg);
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		if (TrapState == EnumTrapState.Trapped && !inv.Empty)
		{
			ItemStack stack = inv[0].Itemstack;
			stack.Collectible.GetBehavior<BlockBehaviorCreatureContainer>()?.AddCreatureInfo(stack, dsc, Api.World);
		}
		else
		{
			dsc.Append(BlockEntityShelf.PerishableInfoCompact(Api, inv[0], 0f));
		}
	}

	protected override float[][] genTransformationMatrices()
	{
		tfMatrices = new float[1][];
		for (int i = 0; i < 1; i++)
		{
			tfMatrices[i] = new Matrixf().Translate(0.5f, 0.1f, 0.5f).Scale(0.75f, 0.75f, 0.75f).Translate(-0.5f, 0f, -0.5f)
				.Values;
		}
		return tfMatrices;
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (TrapState == EnumTrapState.Destroyed)
		{
			mesher.AddMeshData(GetOrCreateMesh(destroyedShapeLoc), rotMat);
			return true;
		}
		if (!base.OnTesselation(mesher, tessThreadTesselator))
		{
			mesher.AddMeshData(capi.TesselatorManager.GetDefaultBlockMesh(base.Block), rotMat);
		}
		return true;
	}

	public MeshData GetCurrentMesh(ITexPositionSource texSource)
	{
		switch (TrapState)
		{
		case EnumTrapState.Empty:
		case EnumTrapState.Ready:
			return GetOrCreateMesh(base.Block.Shape.Base.Clone().WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json"));
		case EnumTrapState.Trapped:
			return GetOrCreateMesh(trappedShapeLoc, texSource);
		case EnumTrapState.Destroyed:
			return GetOrCreateMesh(destroyedShapeLoc, texSource);
		default:
			return null;
		}
	}

	public MeshData GetOrCreateMesh(AssetLocation loc, ITexPositionSource texSource = null)
	{
		return ObjectCacheUtil.GetOrCreate(Api, string.Concat("destroyedBasketTrap-", loc, (texSource == null) ? "-d" : "-t"), delegate
		{
			Shape shape = Api.Assets.Get<Shape>(loc);
			if (texSource == null)
			{
				texSource = new ShapeTextureSource(capi, shape, loc.ToShortString());
			}
			(Api as ICoreClientAPI).Tesselator.TesselateShape("basket trap decal", Api.Assets.Get<Shape>(loc), out var modeldata, texSource, null, 0, 0, 0);
			return modeldata;
		});
	}
}
