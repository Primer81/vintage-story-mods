using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockPlant : Block, IDrawYAdjustable, IWithDrawnHeight
{
	private Block snowLayerBlock;

	private Block tallGrassBlock;

	protected bool climateColorMapping;

	protected bool tallGrassColorMapping;

	private int ExtraBend;

	protected bool disappearOnSoilRemoved;

	public int drawnHeight { get; set; }

	public virtual bool skipPlantCheck { get; set; }

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		if (api.Side == EnumAppSide.Client)
		{
			snowLayerBlock = api.World.GetBlock(new AssetLocation("snowlayer-1"));
			tallGrassBlock = api.World.GetBlock(new AssetLocation("tallgrass-tall-free"));
			if (RandomDrawOffset > 0)
			{
				JsonObject overrider = Attributes?["overrideRandomDrawOffset"];
				if (overrider != null && overrider.Exists)
				{
					RandomDrawOffset = overrider.AsInt(1);
				}
			}
		}
		disappearOnSoilRemoved = Attributes?["disappearOnSoilRemoved"].AsBool() ?? false;
		climateColorMapping = EntityClass == "Sapling";
		tallGrassColorMapping = Code.Path == "flower-lilyofthevalley-free";
		ExtraBend = (Attributes?["extraBend"].AsInt() ?? 0) << 29;
		drawnHeight = (Attributes?["drawnHeight"]?.AsInt(48)).GetValueOrDefault(48);
	}

	public float AdjustYPosition(BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
	{
		if (!(chunkExtBlocks[extIndex3d + TileSideEnum.MoveIndex[5]] is BlockFarmland))
		{
			return 0f;
		}
		return -0.0625f;
	}

	public override void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
	{
		if (VertexFlags.WindMode == EnumWindBitMode.NormalWind)
		{
			bool waveOff = ((lightRgbsByCorner[24] >> 24) & 0xFF) < 159;
			setLeaveWaveFlags(sourceMesh, waveOff);
		}
	}

	private void setLeaveWaveFlags(MeshData sourceMesh, bool off)
	{
		int all = VertexFlags.All;
		int clearFlags = -33554432;
		int verticesCount = sourceMesh.VerticesCount;
		int windToApply = all | ExtraBend;
		int[] sourceMeshFlags = sourceMesh.Flags;
		float[] sourceMeshXyz = sourceMesh.xyz;
		for (int vertexNum = 0; vertexNum < verticesCount; vertexNum++)
		{
			int flag = sourceMeshFlags[vertexNum] & clearFlags;
			if (!off && (double)sourceMeshXyz[vertexNum * 3 + 1] > 0.5)
			{
				flag |= windToApply;
			}
			sourceMeshFlags[vertexNum] = flag;
		}
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (skipPlantCheck)
		{
			return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
		}
		if (Variant.ContainsKey("side"))
		{
			return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
		}
		if (CanPlantStay(world.BlockAccessor, blockSel.Position))
		{
			return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
		}
		failureCode = "requirefertileground";
		return false;
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		if (!skipPlantCheck && !CanPlantStay(world.BlockAccessor, pos))
		{
			if (world.BlockAccessor.GetBlock(pos.DownCopy()).Id == 0 && disappearOnSoilRemoved)
			{
				world.BlockAccessor.SetBlock(0, pos);
			}
			else
			{
				world.BlockAccessor.BreakBlock(pos, null);
			}
		}
		base.OnNeighbourBlockChange(world, pos, neibpos);
	}

	public virtual bool CanPlantStay(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (Variant.ContainsKey("side"))
		{
			BlockFacing facing = BlockFacing.FromCode(Variant["side"]);
			BlockPos npos = pos.AddCopy(facing);
			return blockAccessor.GetBlock(npos).CanAttachBlockAt(blockAccessor, this, npos, facing.Opposite);
		}
		if (blockAccessor.GetBlockBelow(pos).Fertility <= 0)
		{
			return false;
		}
		return true;
	}

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, BlockPatchAttributes attributes = null)
	{
		if (!CanPlantStay(blockAccessor, pos))
		{
			return false;
		}
		bool canPlace = true;
		BlockPos tmpPos = pos.Copy();
		for (int x = -1; x < 2; x++)
		{
			for (int z = -1; z < 2; z++)
			{
				tmpPos.Set(pos.X + x, pos.Y, pos.Z + z);
				if (blockAccessor.GetBlock(tmpPos, 1) is BlockWaterLilyGiant)
				{
					canPlace = false;
				}
			}
		}
		if (!canPlace)
		{
			return false;
		}
		return base.TryPlaceBlockForWorldGen(blockAccessor, pos, onBlockFace, worldGenRand, attributes);
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		if (snowLevel > 0f)
		{
			return snowLayerBlock.GetRandomColor(capi, pos, facing, rndIndex);
		}
		if (tallGrassColorMapping)
		{
			return tallGrassBlock.GetRandomColor(capi, pos, BlockFacing.UP, rndIndex);
		}
		int color = base.GetRandomColor(capi, pos, facing, rndIndex);
		if (climateColorMapping)
		{
			color = capi.World.ApplyColorMapOnRgba(ClimateColorMap, SeasonColorMap, color, pos.X, pos.Y, pos.Z);
		}
		return color;
	}

	public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
	{
		if (EntityClass != null && world.BlockAccessor.GetBlockEntity(pos) is BlockEntitySapling bes)
		{
			return bes.GetBlockName();
		}
		return base.GetPlacedBlockName(world, pos);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		if (EntityClass != null && world.BlockAccessor.GetBlockEntity(pos) is BlockEntitySapling bes)
		{
			return bes.GetDrops();
		}
		return base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
	}

	public override int GetColor(ICoreClientAPI capi, BlockPos pos)
	{
		if (snowLevel > 0f)
		{
			return snowLayerBlock.GetColor(capi, pos);
		}
		if (tallGrassColorMapping)
		{
			return tallGrassBlock.GetColor(capi, pos);
		}
		return base.GetColor(capi, pos);
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (snowLevel > 1f || RandomDrawOffset > 7)
		{
			return SelectionBoxes;
		}
		return base.GetSelectionBoxes(blockAccessor, pos);
	}

	public EnumTreeFellingBehavior GetTreeFellingBehavior(BlockPos pos, Vec3i fromDir, int spreadIndex)
	{
		return EnumTreeFellingBehavior.ChopSpreadVertical;
	}
}
