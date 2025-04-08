using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockCrop : Block, IDrawYAdjustable
{
	protected static readonly float defaultGrowthProbability = 0.8f;

	protected float tickGrowthProbability;

	protected float onFarmlandVerticalOffset = -0.0625f;

	protected MeshData onFarmLandMesh;

	protected CompositeShape onFarmlandCshape;

	public static float WildCropDropMul = 0.25f;

	public int CurrentCropStage
	{
		get
		{
			int.TryParse(LastCodePart(), out var stage);
			return stage;
		}
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		if (Code.Path.Contains("sunflower"))
		{
			waveFlagMinY = 0.1f;
		}
		else
		{
			waveFlagMinY = 0.5f;
		}
		tickGrowthProbability = ((Attributes?["tickGrowthProbability"] != null) ? Attributes["tickGrowthProbability"].AsFloat(defaultGrowthProbability) : defaultGrowthProbability);
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		onFarmlandVerticalOffset = (Attributes?["onFarmlandVerticalOffset"].AsFloat(-0.0625f)).Value;
		onFarmlandCshape = Attributes?["onFarmlandShape"].AsObject<CompositeShape>();
		if (RandomDrawOffset > 0)
		{
			JsonObject overrider = Attributes?["overrideRandomDrawOffset"];
			if (overrider != null && overrider.Exists)
			{
				RandomDrawOffset = overrider.AsInt(1);
			}
		}
	}

	public float AdjustYPosition(BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
	{
		if (!(chunkExtBlocks[extIndex3d + TileSideEnum.MoveIndex[5]] is BlockFarmland))
		{
			return 0f;
		}
		return onFarmlandVerticalOffset;
	}

	public override void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
	{
		if (onFarmlandCshape == null)
		{
			return;
		}
		if (onFarmLandMesh == null)
		{
			Shape shape = api.Assets.TryGet(onFarmlandCshape.Base).ToObject<Shape>();
			if (shape == null)
			{
				onFarmlandCshape = null;
				return;
			}
			onFarmlandVerticalOffset = 0f;
			(api as ICoreClientAPI).Tesselator.TesselateShape(this, shape, out onFarmLandMesh);
		}
		sourceMesh = onFarmLandMesh;
	}

	public override void OnDecalTesselation(IWorldAccessor world, MeshData decalMesh, BlockPos pos)
	{
		if (world.BlockAccessor.GetLightLevel(pos, EnumLightLevelType.OnlySunLight) >= 14)
		{
			decalMesh.SetWindFlag(waveFlagMinY, (int)VertexFlags.WindMode);
		}
		else
		{
			decalMesh.ClearWindFlags();
		}
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		dsc.AppendLine(Lang.Get("Stage: {0}/{1}", CurrentCropStage, CropProps.GrowthStages));
	}

	public override bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object extra)
	{
		extra = null;
		if (offThreadRandom.NextDouble() < (double)tickGrowthProbability && IsNotOnFarmland(world, pos))
		{
			extra = GetNextGrowthStageBlock(world, pos);
			return true;
		}
		return false;
	}

	public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
	{
		Block block = extra as Block;
		world.BlockAccessor.ExchangeBlock(block.BlockId, pos);
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position.DownCopy()) is BlockEntityFarmland befarmland && befarmland.OnBlockInteract(byPlayer))
		{
			return true;
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, BlockPatchAttributes attributes = null)
	{
		if (blockAccessor.GetBlockBelow(pos).Fertility == 0)
		{
			return false;
		}
		if (blockAccessor.GetBlock(pos).IsReplacableBy(this))
		{
			blockAccessor.SetBlock(BlockId, pos);
			return true;
		}
		return false;
	}

	public int CurrentStage()
	{
		int.TryParse(LastCodePart(), out var stage);
		return stage;
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		BlockEntityFarmland befarmland = world.BlockAccessor.GetBlockEntity(pos.DownCopy()) as BlockEntityFarmland;
		if (befarmland == null)
		{
			dropQuantityMultiplier *= byPlayer?.Entity.Stats.GetBlended("wildCropDropRate") ?? 1f;
		}
		SplitDropStacks = false;
		ItemStack[] drops = base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
		if (befarmland == null)
		{
			List<ItemStack> moddrops = new List<ItemStack>();
			ItemStack[] array = drops;
			foreach (ItemStack drop in array)
			{
				if (!(drop.Item is ItemPlantableSeed))
				{
					drop.StackSize = GameMath.RoundRandom(world.Rand, WildCropDropMul * (float)drop.StackSize);
				}
				if (drop.StackSize > 0)
				{
					moddrops.Add(drop);
				}
			}
			drops = moddrops.ToArray();
		}
		if (befarmland != null)
		{
			drops = befarmland.GetDrops(drops);
		}
		return drops;
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
		(world.BlockAccessor.GetBlockEntity(pos.DownCopy()) as BlockEntityFarmland)?.OnCropBlockBroken();
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		Block block = world.BlockAccessor.GetBlock(pos.DownCopy());
		if (block is BlockFarmland)
		{
			return block.GetPlacedBlockInfo(world, pos.DownCopy(), forPlayer);
		}
		return Lang.Get("Required Nutrient: {0}", CropProps.RequiredNutrient) + "\n" + Lang.Get("Growth Stage: {0} / {1}", CurrentStage(), CropProps.GrowthStages);
	}

	private bool IsNotOnFarmland(IWorldAccessor world, BlockPos pos)
	{
		return !world.BlockAccessor.GetBlock(pos.DownCopy()).FirstCodePart().Equals("farmland");
	}

	private Block GetNextGrowthStageBlock(IWorldAccessor world, BlockPos pos)
	{
		int nextStage = CurrentStage() + 1;
		if (world.GetBlock(CodeWithParts(nextStage.ToString())) == null)
		{
			nextStage = 1;
		}
		return world.GetBlock(CodeWithParts(nextStage.ToString()));
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-crop-breaktoharvest",
				MouseButton = EnumMouseButton.Left,
				ShouldApply = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => CropProps.GrowthStages == CurrentCropStage
			}
		}.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}
}
