using System.Collections.Generic;
using System.Text;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockFruitTreeBranch : BlockFruitTreePart, ITexPositionSource, ICustomTreeFellingBehavior, ICustomHandbookPageContent
{
	private Block branchBlock;

	private BlockFruitTreeFoliage foliageBlock;

	private ICoreClientAPI capi;

	public FruitTreeWorldGenConds[] WorldGenConds;

	public Dictionary<string, FruitTreeShape> Shapes = new Dictionary<string, FruitTreeShape>();

	public Dictionary<string, FruitTreeTypeProperties> TypeProps;

	private string curTreeType;

	private Shape curTessShape;

	public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			foliageBlock.foliageProps.TryGetValue(curTreeType, out var props);
			if (props != null)
			{
				TextureAtlasPosition texPos = props.GetOrLoadTexture(capi, textureCode);
				if (texPos != null)
				{
					return texPos;
				}
			}
			AssetLocation texturePath = null;
			Shape shape = curTessShape;
			if (shape != null && shape.Textures.TryGetValue(textureCode, out texturePath))
			{
				return capi.BlockTextureAtlas[texturePath];
			}
			return null;
		}
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		capi = api as ICoreClientAPI;
		branchBlock = api.World.GetBlock(CodeWithVariant("type", "branch"));
		foliageBlock = api.World.GetBlock(AssetLocation.Create(Attributes["foliageBlock"].AsString(), Code.Domain)) as BlockFruitTreeFoliage;
		TypeProps = Attributes["fruittreeProperties"].AsObject<Dictionary<string, FruitTreeTypeProperties>>();
		Dictionary<string, CompositeShape> dictionary = Attributes["shapes"].AsObject<Dictionary<string, CompositeShape>>();
		WorldGenConds = Attributes["worldgen"].AsObject<FruitTreeWorldGenConds[]>();
		foreach (KeyValuePair<string, CompositeShape> val in dictionary)
		{
			Shape shape = Vintagestory.API.Common.Shape.TryGet(api, val.Value.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/"));
			Shapes[val.Key] = new FruitTreeShape
			{
				Shape = shape,
				CShape = val.Value
			};
		}
		LCGRandom rnd = new LCGRandom(api.World.Seed);
		foreach (KeyValuePair<string, FruitTreeTypeProperties> prop in TypeProps)
		{
			BlockDropItemStack[] fruitStacks = prop.Value.FruitStacks;
			for (int i = 0; i < fruitStacks.Length; i++)
			{
				fruitStacks[i].Resolve(api.World, "fruit tree FruitStacks ", Code);
			}
			(api as ICoreServerAPI)?.RegisterTreeGenerator(new AssetLocation("fruittree-" + prop.Key), delegate(IBlockAccessor blockAccessor, BlockPos pos, TreeGenParams treegenParams)
			{
				GrowTree(blockAccessor, pos, prop.Key, treegenParams.size, rnd);
			});
		}
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return false;
		}
		bool num = world.BlockAccessor.GetBlock(blockSel.Position.DownCopy()).Fertility > 0;
		BlockEntityFruitTreeBranch aimedBe = world.BlockAccessor.GetBlockEntity(blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position) as BlockEntityFruitTreeBranch;
		bool grafted = blockSel.Face != BlockFacing.DOWN && aimedBe != null && (aimedBe.SideGrowth & (1 << blockSel.Face.Index)) > 0;
		if (!num && !grafted)
		{
			failureCode = "fruittreecutting";
			return false;
		}
		if (grafted && TypeProps.TryGetValue(aimedBe.TreeType, out var rootProps) && TypeProps.TryGetValue(itemstack.Attributes.GetString("type"), out var selfProprs) && rootProps.CycleType != selfProprs.CycleType)
		{
			failureCode = "fruittreecutting-ctypemix";
			return false;
		}
		return DoPlaceBlock(world, byPlayer, blockSel, itemstack);
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		if (itemstack.Collectible.Variant["type"] == "cutting")
		{
			curTreeType = itemstack.Attributes.GetString("type");
			if (curTreeType == null)
			{
				return;
			}
			Dictionary<string, MultiTextureMeshRef> dict = ObjectCacheUtil.GetOrCreate(capi, "cuttingMeshRefs", () => new Dictionary<string, MultiTextureMeshRef>());
			if (!dict.TryGetValue(curTreeType, out var meshref))
			{
				curTessShape = capi.TesselatorManager.GetCachedShape(Shape.Base);
				capi.Tesselator.TesselateShape("fruittreecutting", curTessShape, out var meshdata, this, null, 0, 0, 0);
				dict[curTreeType] = (renderinfo.ModelRef = capi.Render.UploadMultiTextureMesh(meshdata));
			}
			else
			{
				renderinfo.ModelRef = meshref;
			}
		}
		base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		base.OnUnloaded(api);
		if (capi == null)
		{
			return;
		}
		Dictionary<string, MultiTextureMeshRef> dict = ObjectCacheUtil.GetOrCreate(capi, "cuttingMeshRefs", () => new Dictionary<string, MultiTextureMeshRef>());
		if (dict == null)
		{
			return;
		}
		foreach (KeyValuePair<string, MultiTextureMeshRef> item in dict)
		{
			item.Value.Dispose();
		}
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		ItemStack itemStack = base.OnPickBlock(world, pos);
		BlockEntityFruitTreeBranch be = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityFruitTreeBranch;
		itemStack.Attributes.SetString("type", be?.TreeType ?? "pinkapple");
		return itemStack;
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		BlockDropItemStack[] drops = base.GetDropsForHandbook(handbookStack, forPlayer);
		BlockDropItemStack[] array = drops;
		foreach (BlockDropItemStack drop in array)
		{
			if (drop.ResolvedItemstack.Collectible is BlockFruitTreeBranch)
			{
				drop.ResolvedItemstack.Attributes.SetString("type", handbookStack.Attributes.GetString("type") ?? "pinkapple");
			}
		}
		return drops;
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		ItemStack[] stacks = base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
		BlockEntityFruitTreeBranch be = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityFruitTreeBranch;
		bool alive = be != null && be.FoliageState != EnumFoliageState.Dead;
		for (int i = 0; i < stacks.Length; i++)
		{
			ItemStack stack = stacks[i];
			if (stack.Collectible is BlockFruitTreeBranch)
			{
				stack.Attributes.SetString("type", be?.TreeType);
			}
			if (stack.Collectible.Variant["type"] == "cutting" && !alive)
			{
				stacks[i] = new ItemStack(world.GetItem(new AssetLocation("firewood")), 2);
			}
		}
		return stacks;
	}

	public override bool ShouldMergeFace(int facingIndex, Block neighbourBlock, int intraChunkIndex3d)
	{
		if (this == branchBlock)
		{
			return (facingIndex == 1 || facingIndex == 2 || facingIndex == 4) & (neighbourBlock == this || neighbourBlock == branchBlock);
		}
		return false;
	}

	public EnumTreeFellingBehavior GetTreeFellingBehavior(BlockPos pos, Vec3i fromDir, int spreadIndex)
	{
		if (!(api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityFruitTreeBranch { PartType: EnumTreePartType.Branch } bebranch))
		{
			return EnumTreeFellingBehavior.Chop;
		}
		if (bebranch.GrowthDir.IsVertical)
		{
			return EnumTreeFellingBehavior.Chop;
		}
		return EnumTreeFellingBehavior.NoChop;
	}

	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return (blockAccessor.GetBlockEntity(pos) as BlockEntityFruitTreeBranch)?.GetColSelBox() ?? base.GetCollisionBoxes(blockAccessor, pos);
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return (blockAccessor.GetBlockEntity(pos) as BlockEntityFruitTreeBranch)?.GetColSelBox() ?? base.GetCollisionBoxes(blockAccessor, pos);
	}

	public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityFruitTreeBranch bebranch)
		{
			FruitTreeRootBH rootbh = bebranch.GetBehavior<FruitTreeRootBH>();
			if (rootbh != null && rootbh.IsYoung && bebranch.PartType != EnumTreePartType.Cutting)
			{
				return Lang.Get("fruittree-young-" + bebranch.TreeType);
			}
			string code = "fruittree-branch-";
			if (bebranch.PartType == EnumTreePartType.Cutting)
			{
				code = "fruittree-cutting-";
			}
			else if (bebranch.PartType == EnumTreePartType.Stem || rootbh != null)
			{
				code = "fruittree-stem-";
			}
			return Lang.Get(code + bebranch.TreeType);
		}
		return base.GetPlacedBlockName(world, pos);
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		string type = itemStack.Attributes?.GetString("type", "unknown") ?? "unknown";
		return Lang.Get("fruittree-cutting-" + type);
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
	}

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldgenRandom, BlockPatchAttributes attributes = null)
	{
		if (blockAccessor.GetBlockBelow(pos).Fertility <= 20)
		{
			return false;
		}
		ClimateCondition climate = blockAccessor.GetClimateAt(pos, EnumGetClimateMode.WorldGenValues);
		int rnd = worldgenRandom.NextInt(WorldGenConds.Length);
		int len = WorldGenConds.Length;
		for (int i = 0; i < len; i++)
		{
			FruitTreeWorldGenConds conds = WorldGenConds[(i + rnd) % len];
			if (conds.MinTemp <= climate.Temperature && conds.MaxTemp >= climate.Temperature && conds.MinRain <= climate.Rainfall && conds.MaxRain >= climate.Rainfall && worldgenRandom.NextFloat() <= conds.Chance)
			{
				blockAccessor.SetBlock(BlockId, pos);
				blockAccessor.SpawnBlockEntity(EntityClass, pos);
				BlockEntityFruitTreeBranch obj = blockAccessor.GetBlockEntity(pos) as BlockEntityFruitTreeBranch;
				obj.TreeType = conds.Type;
				obj.FastForwardGrowth = worldgenRandom.NextFloat();
				return true;
			}
		}
		return false;
	}

	public void GrowTree(IBlockAccessor blockAccessor, BlockPos pos, string type, float growthRel, IRandom random)
	{
		pos = pos.UpCopy();
		blockAccessor.SetBlock(BlockId, pos);
		BlockEntityFruitTreeBranch be = api.ClassRegistry.CreateBlockEntity(EntityClass) as BlockEntityFruitTreeBranch;
		be.Pos = pos.Copy();
		be.TreeType = type;
		be.FastForwardGrowth = growthRel;
		blockAccessor.SpawnBlockEntity(be);
	}

	public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
	{
		if (world.BlockAccessor.GetBlockEntity(blockPos) is BlockEntityFruitTreeBranch be && be.FastForwardGrowth.HasValue)
		{
			be.CreateBehaviors(this, api.World);
			be.Initialize(api);
			be.MarkDirty(redrawOnClient: true);
		}
		else
		{
			base.OnBlockPlaced(world, blockPos, byItemStack);
		}
	}

	public void OnHandbookPageComposed(List<RichTextComponentBase> components, ItemSlot inSlot, ICoreClientAPI capi, ItemStack[] allStacks, ActionConsumable<string> openDetailPageFor)
	{
		string type = inSlot.Itemstack.Attributes?.GetString("type", "unknown") ?? "unknown";
		if (TypeProps.TryGetValue(type, out var props))
		{
			StringBuilder sb = new StringBuilder();
			if (props.CycleType == EnumTreeCycleType.Deciduous)
			{
				sb.AppendLine(Lang.Get("Must experience {0} game hours below {1}°C in the cold season to bear fruit in the following year.", props.VernalizationHours.avg, props.VernalizationTemp.avg));
				sb.AppendLine(Lang.Get("Will die if exposed to {0}°C or colder", props.DieBelowTemp.avg));
			}
			else
			{
				sb.AppendLine(Lang.Get("Evergreen tree. Will die if exposed to {0} °C or colder", props.DieBelowTemp.avg));
			}
			sb.AppendLine();
			sb.AppendLine(Lang.Get("handbook-fruittree-note-averages"));
			float marginTop = 7f;
			components.Add(new ClearFloatTextComponent(capi, marginTop));
			components.Add(new RichTextComponent(capi, Lang.Get("Growing properties") + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));
			components.Add(new RichTextComponent(capi, sb.ToString(), CairoFont.WhiteSmallText()));
			components.Add(new ClearFloatTextComponent(capi, marginTop));
			components.Add(new RichTextComponent(capi, Lang.Get("fruittree-produces") + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));
			BlockDropItemStack[] fruitStacks = props.FruitStacks;
			foreach (BlockDropItemStack stack in fruitStacks)
			{
				components.Add(new ItemstackTextComponent(capi, stack.ResolvedItemstack, 40.0, 0.0, EnumFloat.Inline));
			}
		}
	}
}
