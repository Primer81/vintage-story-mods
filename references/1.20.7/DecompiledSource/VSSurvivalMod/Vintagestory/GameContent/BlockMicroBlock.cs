using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockMicroBlock : Block
{
	public int snowLayerBlockId;

	private bool IsSnowCovered;

	public ThreadLocal<MicroBlockSounds> MBSounds = new ThreadLocal<MicroBlockSounds>(() => new MicroBlockSounds());

	public static int BlockLayerMetaBlockId;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		notSnowCovered = api.World.GetBlock(AssetLocation.Create(FirstCodePart(), Code.Domain));
		snowCovered1 = api.World.GetBlock(AssetLocation.Create(FirstCodePart() + "-snow", Code.Domain));
		snowCovered2 = api.World.GetBlock(AssetLocation.Create(FirstCodePart() + "-snow2", Code.Domain));
		snowCovered3 = api.World.GetBlock(AssetLocation.Create(FirstCodePart() + "-snow3", Code.Domain));
		if (this == snowCovered1)
		{
			snowLevel = 1f;
		}
		if (this == snowCovered2)
		{
			snowLevel = 2f;
		}
		if (this == snowCovered3)
		{
			snowLevel = 3f;
		}
		CustomBlockLayerHandler = true;
		BlockLayerMetaBlockId = api.World.GetBlock(new AssetLocation("meta-blocklayer")).Id;
		snowLayerBlockId = api.World.GetBlock(new AssetLocation("snowlayer-1")).Id;
		IsSnowCovered = Id != notSnowCovered.Id;
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		MBSounds.Dispose();
	}

	public override BlockSounds GetSounds(IBlockAccessor blockAccessor, BlockSelection blockSel, ItemStack stack = null)
	{
		return (blockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityMicroBlock)?.GetSounds() ?? base.GetSounds(blockAccessor, blockSel, stack);
	}

	public override bool DisplacesLiquids(IBlockAccessor blockAccess, BlockPos pos)
	{
		if (blockAccess.GetBlockEntity(pos) is BlockEntityMicroBlock bec)
		{
			return bec.DisplacesLiquid();
		}
		return base.DisplacesLiquids(blockAccess, pos);
	}

	public override float GetLiquidBarrierHeightOnSide(BlockFacing face, BlockPos pos)
	{
		if (api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityMicroBlock bec)
		{
			return bec.sideAlmostSolid[face.Index] ? 1 : 0;
		}
		return base.GetLiquidBarrierHeightOnSide(face, pos);
	}

	public override bool SideIsSolid(BlockPos pos, int faceIndex)
	{
		return SideIsSolid(api.World.BlockAccessor, pos, faceIndex);
	}

	public override bool SideIsSolid(IBlockAccessor blockAccessor, BlockPos pos, int faceIndex)
	{
		if (blockAccessor.GetBlockEntity(pos) is BlockEntityMicroBlock bec)
		{
			return bec.sideAlmostSolid[faceIndex];
		}
		return false;
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		if (pos.X != neibpos.X || pos.Z != neibpos.Z || pos.Y + 1 != neibpos.Y || world.BlockAccessor.GetBlock(neibpos).Id == 0)
		{
			return;
		}
		BEBehaviorMicroblockSnowCover bebeh = GetBEBehavior<BEBehaviorMicroblockSnowCover>(pos);
		if (bebeh != null)
		{
			bool markdirty = bebeh.SnowLevel != 0 || bebeh.SnowCuboids.Count > 0 || bebeh.GroundSnowCuboids.Count > 0;
			if (Id != notSnowCovered.Id)
			{
				world.BlockAccessor.ExchangeBlock(notSnowCovered.Id, pos);
				markdirty = true;
			}
			bebeh.SnowLevel = 0;
			bebeh.SnowCuboids.Clear();
			bebeh.GroundSnowCuboids.Clear();
			if (markdirty)
			{
				world.BlockAccessor.GetBlockEntity(pos).MarkDirty(redrawOnClient: true);
			}
		}
	}

	public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
	{
		if (!(extra is string) || !((string)extra == "melt"))
		{
			return;
		}
		BlockEntity bec = world.BlockAccessor.GetBlockEntity(pos);
		BEBehaviorMicroblockSnowCover bebeh = bec?.GetBehavior<BEBehaviorMicroblockSnowCover>();
		if (bebeh != null)
		{
			if (this == snowCovered3)
			{
				world.BlockAccessor.ExchangeBlock(snowCovered2.Id, pos);
				bebeh.SnowLevel = 0;
				bec.MarkDirty(redrawOnClient: true);
			}
			else if (this == snowCovered2)
			{
				world.BlockAccessor.ExchangeBlock(snowCovered1.Id, pos);
				bebeh.SnowLevel = 0;
				bec.MarkDirty(redrawOnClient: true);
			}
			else if (this == snowCovered1)
			{
				world.BlockAccessor.ExchangeBlock(notSnowCovered.Id, pos);
				bebeh.SnowLevel = 0;
				bec.MarkDirty(redrawOnClient: true);
			}
		}
	}

	public override float GetSnowLevel(BlockPos pos)
	{
		if (!IsSnowCovered)
		{
			return 0f;
		}
		return 0.5f;
	}

	public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
	{
		return true;
	}

	public override Vec4f GetSelectionColor(ICoreClientAPI capi, BlockPos pos)
	{
		if (!(capi.World.Player.InventoryManager.ActiveHotbarSlot?.Itemstack?.Item is ItemChisel) && !BlockEntityChisel.ForceDetailingMode)
		{
			return base.GetSelectionColor(capi, pos);
		}
		BlockEntityMicroBlock bemc = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityMicroBlock;
		if (bemc?.BlockIds == null || bemc.BlockIds.Length == 0)
		{
			return new Vec4f(0f, 0f, 0f, 0.6f);
		}
		int col = api.World.GetBlock(bemc.BlockIds[0]).GetColor(capi, pos);
		if ((double)((float)((col & 0xFF) + ((col >> 8) & 0xFF) + ((col >> 16) & 0xFF)) / 3f) < 102.0)
		{
			return new Vec4f(1f, 1f, 1f, 0.6f);
		}
		return new Vec4f(0f, 0f, 0f, 0.5f);
	}

	public override int GetRetention(BlockPos pos, BlockFacing facing, EnumRetentionType type)
	{
		BlockEntityMicroBlock bemc = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityMicroBlock;
		if (bemc?.BlockIds != null && (bemc.sideAlmostSolid[facing.Index] || bemc.sideAlmostSolid[facing.Opposite.Index]) && bemc.BlockIds.Length != 0 && bemc.VolumeRel >= 0.5f)
		{
			if (type == EnumRetentionType.Sound)
			{
				return 10;
			}
			EnumBlockMaterial mat = api.World.GetBlock(bemc.BlockIds[0]).BlockMaterial;
			if (mat == EnumBlockMaterial.Ore || mat == EnumBlockMaterial.Stone || mat == EnumBlockMaterial.Soil || mat == EnumBlockMaterial.Ceramic)
			{
				return -1;
			}
			return 1;
		}
		return base.GetRetention(pos, facing, type);
	}

	public override byte[] GetLightHsv(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack = null)
	{
		if (pos != null)
		{
			return (blockAccessor.GetBlockEntity(pos) as BlockEntityMicroBlock)?.GetLightHsv(blockAccessor) ?? LightHsv;
		}
		int[] matids = (stack.Attributes?["materials"] as IntArrayAttribute)?.value;
		byte[] hsv = new byte[3];
		if (matids == null)
		{
			return hsv;
		}
		int q = 0;
		for (int i = 0; i < matids.Length; i++)
		{
			Block block = blockAccessor.GetBlock(matids[i]);
			if (block.LightHsv[2] > 0)
			{
				hsv[0] += block.LightHsv[0];
				hsv[1] += block.LightHsv[1];
				hsv[2] += block.LightHsv[2];
				q++;
			}
		}
		if (q == 0)
		{
			return hsv;
		}
		hsv[0] = (byte)(hsv[0] / q);
		hsv[1] = (byte)(hsv[1] / q);
		hsv[2] = (byte)(hsv[2] / q);
		return hsv;
	}

	public override bool MatchesForCrafting(ItemStack inputStack, GridRecipe gridRecipe, CraftingRecipeIngredient ingredient)
	{
		if (((inputStack.Attributes["materials"] as StringArrayAttribute)?.value?.Length).GetValueOrDefault() > 2)
		{
			return false;
		}
		return base.MatchesForCrafting(inputStack, gridRecipe, ingredient);
	}

	public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe)
	{
		List<int> matids = new List<int>();
		List<int> matquantities = new List<int>();
		bool first = false;
		foreach (ItemSlot val in allInputslots)
		{
			if (val.Empty)
			{
				continue;
			}
			if (!first)
			{
				first = true;
				outputSlot.Itemstack.Attributes = val.Itemstack.Attributes.Clone();
			}
			int[] mats = (val.Itemstack.Attributes?["materials"] as IntArrayAttribute)?.value;
			if (mats != null)
			{
				matids.AddRange(mats);
			}
			string[] smats = (val.Itemstack.Attributes?["materials"] as StringArrayAttribute)?.value;
			if (smats != null)
			{
				string[] array = smats;
				foreach (string code in array)
				{
					Block block = api.World.GetBlock(new AssetLocation(code));
					if (block != null)
					{
						matids.Add(block.Id);
					}
				}
			}
			int[] mq = (val.Itemstack.Attributes?["availMaterialQuantities"] as IntArrayAttribute)?.value;
			if (mq != null)
			{
				matquantities.AddRange(mq);
			}
		}
		outputSlot.Itemstack.Attributes["materials"] = new IntArrayAttribute(matids.ToArray());
		outputSlot.Itemstack.Attributes["availMaterialQuantities"] = new IntArrayAttribute(matquantities.ToArray());
		base.OnCreatedByCrafting(allInputslots, outputSlot, byRecipe);
	}

	public override int GetLightAbsorption(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return GetLightAbsorption(blockAccessor.GetChunkAtBlockPos(pos), pos);
	}

	public override int GetLightAbsorption(IWorldChunk chunk, BlockPos pos)
	{
		return (chunk?.GetLocalBlockEntityAtBlockPos(pos) as BlockEntityMicroBlock)?.GetLightAbsorption() ?? 0;
	}

	public override EnumBlockMaterial GetBlockMaterial(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack = null)
	{
		if (blockAccessor is IWorldGenBlockAccessor)
		{
			return base.GetBlockMaterial(blockAccessor, pos, stack);
		}
		if (pos != null)
		{
			if (IsSoilNonSoilMix(blockAccessor, pos))
			{
				return EnumBlockMaterial.Soil;
			}
			BlockEntityMicroBlock be = blockAccessor.GetBlockEntity(pos) as BlockEntityMicroBlock;
			if (be?.BlockIds != null && be.BlockIds.Length != 0)
			{
				return api.World.Blocks[be.GetMajorityMaterialId()].BlockMaterial;
			}
		}
		else
		{
			int[] mats = (stack.Attributes?["materials"] as IntArrayAttribute)?.value;
			if (mats != null && mats.Length != 0)
			{
				return api.World.GetBlock(mats[0]).BlockMaterial;
			}
		}
		return base.GetBlockMaterial(blockAccessor, pos, stack);
	}

	public virtual bool IsSoilNonSoilMix(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return IsSoilNonSoilMix(GetBlockEntity<BlockEntityMicroBlock>(pos));
	}

	public virtual bool IsSoilNonSoilMix(BlockEntityMicroBlock be)
	{
		if (be?.BlockIds == null)
		{
			return false;
		}
		bool hasSoil = false;
		bool hasNonSoil = false;
		for (int i = 0; i < be.BlockIds.Length; i++)
		{
			Block block = api.World.Blocks[be.BlockIds[i]];
			hasSoil |= block.BlockMaterial == EnumBlockMaterial.Soil || block.BlockMaterial == EnumBlockMaterial.Sand || block.BlockMaterial == EnumBlockMaterial.Gravel;
			hasNonSoil |= block.BlockMaterial != EnumBlockMaterial.Soil && block.BlockMaterial != EnumBlockMaterial.Sand && block.BlockMaterial != EnumBlockMaterial.Gravel;
		}
		return hasSoil && hasNonSoil;
	}

	public override bool DoEmitSideAo(IGeometryTester caller, BlockFacing facing)
	{
		return (caller.GetCurrentBlockEntityOnSide(facing.Opposite) as BlockEntityMicroBlock)?.DoEmitSideAo(facing.Index) ?? base.DoEmitSideAo(caller, facing);
	}

	public override bool DoEmitSideAoByFlag(IGeometryTester caller, Vec3iAndFacingFlags vec, int flags)
	{
		return (caller.GetCurrentBlockEntityOnSide(vec) as BlockEntityMicroBlock)?.DoEmitSideAoByFlag(flags) ?? base.DoEmitSideAoByFlag(caller, vec, flags);
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		if (!(world.BlockAccessor.GetBlockEntity(pos) is BlockEntityMicroBlock bec))
		{
			return null;
		}
		TreeAttribute tree = new TreeAttribute();
		bec.ToTreeAttributes(tree);
		tree.RemoveAttribute("posx");
		tree.RemoveAttribute("posy");
		tree.RemoveAttribute("posz");
		tree.RemoveAttribute("snowcuboids");
		return new ItemStack(notSnowCovered.Id, EnumItemClass.Block, 1, tree, world);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		JsonObject attributes = Attributes;
		if (attributes != null && attributes.IsTrue("dropSelf"))
		{
			return new ItemStack[1] { OnPickBlock(world, pos) };
		}
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityMicroBlock be)
		{
			Block block = world.Blocks[be.GetMajorityMaterialId()];
			string rocktype = block.Variant["rock"];
			if (block.BlockMaterial == EnumBlockMaterial.Stone && rocktype != null)
			{
				int q = GameMath.RoundRandom(world.Rand, be.VolumeRel * 4f * dropQuantityMultiplier);
				if (q <= 0)
				{
					return new ItemStack[0];
				}
				ItemStack stack = new ItemStack(world.GetItem(AssetLocation.Create("stone-" + rocktype, Code.Domain)));
				while (q-- > 0)
				{
					world.SpawnItemEntity(stack.Clone(), pos);
				}
			}
		}
		return new ItemStack[0];
	}

	public override bool CanAttachBlockAt(IBlockAccessor world, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
	{
		if (world.GetBlockEntity(pos) is BlockEntityMicroBlock be)
		{
			return be.CanAttachBlockAt(blockFace, attachmentArea);
		}
		return base.CanAttachBlockAt(world, block, pos, blockFace);
	}

	public override bool AllowSnowCoverage(IWorldAccessor world, BlockPos blockPos)
	{
		if (world.BlockAccessor.GetBlockEntity(blockPos) is BlockEntityMicroBlock be)
		{
			return be.sideAlmostSolid[BlockFacing.UP.Index];
		}
		return base.AllowSnowCoverage(world, blockPos);
	}

	public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack)
	{
		base.OnBlockPlaced(world, blockPos, byItemStack);
		if (world.BlockAccessor.GetBlockEntity(blockPos) is BlockEntityMicroBlock be && byItemStack != null)
		{
			ITreeAttribute tree = byItemStack.Attributes.Clone();
			tree.SetInt("posx", blockPos.X);
			tree.SetInt("posy", blockPos.InternalY);
			tree.SetInt("posz", blockPos.Z);
			be.FromTreeAttributes(tree, world);
			if (world.Side == EnumAppSide.Client)
			{
				be.MarkMeshDirty();
			}
			be.MarkDirty(redrawOnClient: true);
			be.RegenSelectionBoxes(world, null);
		}
	}

	public override float OnGettingBroken(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
	{
		return base.OnGettingBroken(player, blockSel, itemslot, remainingResistance, dt, counter);
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		if (!TryToRemoveSoilFirst(world, pos, byPlayer))
		{
			base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
		}
	}

	public virtual bool TryToRemoveSoilFirst(IWorldAccessor world, BlockPos pos, IPlayer byPlayer)
	{
		if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			BlockEntityMicroBlock be = GetBlockEntity<BlockEntityMicroBlock>(pos);
			if (be == null)
			{
				return false;
			}
			bool removed = false;
			Block block = null;
			if (be.BlockIds.Any((int bid) => world.Blocks[bid].BlockMaterial != EnumBlockMaterial.Soil))
			{
				for (int i = 0; i < be.BlockIds.Length; i++)
				{
					block = world.Blocks[be.BlockIds[i]];
					if (block.BlockMaterial == EnumBlockMaterial.Soil)
					{
						be.RemoveMaterial(block);
						removed = true;
					}
				}
				if (removed)
				{
					world.PlaySoundAt(block.Sounds?.GetBreakSound(byPlayer), pos, 0.0, byPlayer);
					SpawnBlockBrokenParticles(pos);
					be.MarkDirty(redrawOnClient: true);
					return true;
				}
			}
		}
		return false;
	}

	public override float GetResistance(IBlockAccessor blockAccessor, BlockPos pos)
	{
		BlockEntityMicroBlock be = blockAccessor.GetBlockEntity(pos) as BlockEntityMicroBlock;
		if (be?.BlockIds != null && be.BlockIds.Length != 0)
		{
			return api.World.GetBlock(be.BlockIds[0]).Resistance;
		}
		return base.GetResistance(blockAccessor, pos);
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		MicroBlockModelCache cache = capi.ModLoader.GetModSystem<MicroBlockModelCache>();
		renderinfo.ModelRef = cache.GetOrCreateMeshRef(itemstack);
	}

	public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityMicroBlock be)
		{
			blockModelData = be.GenMesh();
			decalModelData = be.CreateDecalMesh(decalTexSource);
		}
		else
		{
			base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
		}
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (blockAccessor.GetBlockEntity(pos) is BlockEntityMicroBlock bec)
		{
			return bec.GetSelectionBoxes(blockAccessor, pos);
		}
		return base.GetSelectionBoxes(blockAccessor, pos);
	}

	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (blockAccessor.GetBlockEntity(pos) is BlockEntityMicroBlock bec)
		{
			return bec.GetCollisionBoxes(blockAccessor, pos);
		}
		return base.GetSelectionBoxes(blockAccessor, pos);
	}

	public override Cuboidf[] GetParticleCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (blockAccessor.GetBlockEntity(pos) is BlockEntityMicroBlock bec)
		{
			return bec.GetCollisionBoxes(blockAccessor, pos);
		}
		return base.GetSelectionBoxes(blockAccessor, pos);
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		BlockEntityMicroBlock be = capi.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityMicroBlock;
		if (be?.BlockIds != null && be.BlockIds.Length != 0)
		{
			int blockid2 = be.GetMajorityMaterialId((int blockid) => capi.World.Blocks[blockid].BlockMaterial != EnumBlockMaterial.Meta);
			Block block = capi.World.Blocks[blockid2];
			if (block is BlockMicroBlock)
			{
				return 0;
			}
			return block.GetRandomColor(capi, pos, facing, rndIndex);
		}
		return base.GetRandomColor(capi, pos, facing, rndIndex);
	}

	public override bool Equals(ItemStack thisStack, ItemStack otherStack, params string[] ignoreAttributeSubTrees)
	{
		List<string> ign = ((ignoreAttributeSubTrees == null) ? new List<string>() : new List<string>(ignoreAttributeSubTrees));
		ign.Add("meshId");
		return base.Equals(thisStack, otherStack, ign.ToArray());
	}

	public override int GetColorWithoutTint(ICoreClientAPI capi, BlockPos pos)
	{
		BlockEntityMicroBlock be = capi.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityMicroBlock;
		if (be?.BlockIds != null && be.BlockIds.Length != 0)
		{
			Block block = capi.World.GetBlock(be.BlockIds[0]);
			if (block is BlockMicroBlock)
			{
				return 0;
			}
			return block.GetColor(capi, pos);
		}
		return base.GetColorWithoutTint(capi, pos);
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		if (itemStack.Attributes.HasAttribute("blockName"))
		{
			string blockName = itemStack.Attributes.GetString("blockName");
			if (blockName != "")
			{
				return blockName.Split('\n')[0];
			}
		}
		int[] blockIds = BlockEntityMicroBlock.MaterialIdsFromAttributes(itemStack.Attributes, api.World);
		int mblockid = BlockEntityMicroBlock.getMajorityMaterial(new List<uint>(BlockEntityMicroBlock.GetVoxelCuboids(itemStack.Attributes)), blockIds);
		Block block = api.World.Blocks[mblockid];
		return block.GetHeldItemName(new ItemStack(block));
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		string blockName = inSlot.Itemstack.Attributes.GetString("blockName");
		int nind = blockName.IndexOf('\n');
		if (nind > 0)
		{
			dsc.AppendLine(blockName.Substring(nind + 1));
		}
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		dsc.AppendLine();
		dsc.AppendLine("<font color=\"#bbbbbb\">" + Lang.Get("block-chiseledblock") + "</font>");
	}

	public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityMicroBlock be)
		{
			return be.GetPlacedBlockName();
		}
		return base.GetPlacedBlockName(world, pos);
	}

	public override void PerformSnowLevelUpdate(IBulkBlockAccessor ba, BlockPos pos, Block newBlock, float snowLevel)
	{
		if (newBlock.Id != Id && (BlockMaterial == EnumBlockMaterial.Snow || BlockId == 0 || FirstCodePart() == newBlock.FirstCodePart()))
		{
			ba.ExchangeBlock(newBlock.Id, pos);
		}
	}

	public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, ItemSlot inSlot, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, bool resolveImports)
	{
		int[] blockIds = BlockEntityMicroBlock.MaterialIdsFromAttributes(inSlot.Itemstack.Attributes, worldForResolve);
		foreach (KeyValuePair<int, AssetLocation> blockCode in oldBlockIdMapping)
		{
			int index = blockIds.IndexOf(blockCode.Key);
			if (index != -1)
			{
				Block block = worldForResolve.GetBlock(blockCode.Value);
				if (block != null)
				{
					blockIds[index] = block.Id;
				}
			}
		}
		inSlot.Itemstack.Attributes["materials"] = new IntArrayAttribute(blockIds);
	}
}
