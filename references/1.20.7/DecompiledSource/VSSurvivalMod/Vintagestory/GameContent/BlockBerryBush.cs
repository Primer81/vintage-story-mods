using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockBerryBush : BlockPlant
{
	private MeshData[] prunedmeshes;

	private WorldInteraction[] interactions;

	public string State => Variant["state"];

	public string Type => Variant["type"];

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		interactions = ObjectCacheUtil.GetOrCreate(api, "berryBushInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (Item current in api.World.Items)
			{
				if (current.Tool.GetValueOrDefault() == EnumTool.Shears)
				{
					list.Add(new ItemStack(current));
				}
			}
			ItemStack[] sstacks = list.ToArray();
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-berrybush-prune",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = sstacks,
					GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityBerryBush { Pruned: false }) ? sstacks : null
				}
			};
		});
	}

	public MeshData GetPrunedMesh(BlockPos pos)
	{
		if (api == null)
		{
			return null;
		}
		if (prunedmeshes == null)
		{
			genPrunedMeshes();
		}
		int rnd = ((RandomizeAxes == EnumRandomizeAxes.XYZ) ? GameMath.MurmurHash3Mod(pos.X, pos.Y, pos.Z, prunedmeshes.Length) : GameMath.MurmurHash3Mod(pos.X, 0, pos.Z, prunedmeshes.Length));
		return prunedmeshes[rnd];
	}

	private void genPrunedMeshes()
	{
		ICoreClientAPI capi = api as ICoreClientAPI;
		prunedmeshes = new MeshData[Shape.BakedAlternates.Length];
		string[] selems = new string[4] { "Berries", "branchesN", "branchesS", "Leaves" };
		if (State == "empty")
		{
			selems = selems.Remove("Berries");
		}
		for (int i = 0; i < Shape.BakedAlternates.Length; i++)
		{
			CompositeShape cshape = Shape.BakedAlternates[i];
			Shape shape = capi.TesselatorManager.GetCachedShape(cshape.Base);
			capi.Tesselator.TesselateShape(this, shape, out prunedmeshes[i], Shape.RotateXYZCopy, null, selems);
		}
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityBerryBush { Pruned: false } bebush && byPlayer != null && (byPlayer.InventoryManager?.ActiveHotbarSlot?.Itemstack?.Collectible?.Tool).GetValueOrDefault() == EnumTool.Shears)
		{
			bebush.Prune();
			return true;
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override bool CanPlantStay(IBlockAccessor blockAccessor, BlockPos pos)
	{
		Block belowBlock = blockAccessor.GetBlock(pos.DownCopy());
		if (belowBlock.Fertility > 0)
		{
			return true;
		}
		if (!(belowBlock is BlockBerryBush))
		{
			return false;
		}
		if (blockAccessor.GetBlock(pos.DownCopy(2)).Fertility > 0)
		{
			JsonObject attributes = Attributes;
			if (attributes != null && attributes.IsTrue("stackable"))
			{
				return belowBlock.Attributes?.IsTrue("stackable") ?? false;
			}
		}
		return false;
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		if (Textures == null || Textures.Count == 0)
		{
			return 0;
		}
		BakedCompositeTexture tex = Textures?.First().Value?.Baked;
		if (tex == null)
		{
			return 0;
		}
		int color = capi.BlockTextureAtlas.GetRandomColor(tex.TextureSubId, rndIndex);
		return capi.World.ApplyColorMapOnRgba("climatePlantTint", "seasonalFoliage", color, pos.X, pos.Y, pos.Z);
	}

	public override int GetColor(ICoreClientAPI capi, BlockPos pos)
	{
		int color = base.GetColorWithoutTint(capi, pos);
		return capi.World.ApplyColorMapOnRgba("climatePlantTint", "seasonalFoliage", color, pos.X, pos.Y, pos.Z);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		ItemStack[] drops = base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
		ItemStack[] array = drops;
		foreach (ItemStack drop in array)
		{
			if (!(drop.Collectible is BlockBerryBush))
			{
				float dropRate = 1f;
				JsonObject attributes = Attributes;
				if (attributes != null && attributes.IsTrue("forageStatAffected"))
				{
					dropRate *= byPlayer?.Entity.Stats.GetBlended("forageDropRate") ?? 1f;
				}
				drop.StackSize = GameMath.RoundRandom(api.World.Rand, (float)drop.StackSize * dropRate);
			}
		}
		return drops;
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer).Append(interactions);
	}
}
