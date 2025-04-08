using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.ServerMods.WorldEdit;

namespace Vintagestory.ServerMods;

internal class MicroblockTool : PaintBrushTool
{
	private Dictionary<BlockPos, ChiselBlockInEdit> blocksInEdit;

	public MicroblockTool()
	{
	}

	public MicroblockTool(WorldEditWorkspace workspace, IBlockAccessorRevertable blockAccess)
		: base(workspace, blockAccess)
	{
		blocksInEdit = new Dictionary<BlockPos, ChiselBlockInEdit>();
	}

	public override void Load(ICoreAPI api)
	{
		api.ModLoader.GetModSystem<ModSystemDetailModeSync>().Toggle(workspace.PlayerUID, on: true);
	}

	public override void Unload(ICoreAPI api)
	{
		api.ModLoader.GetModSystem<ModSystemDetailModeSync>().Toggle(workspace.PlayerUID, on: false);
	}

	public override void HighlightBlocks(IPlayer player, ICoreServerAPI sapi, EnumHighlightBlocksMode mode)
	{
		sapi.World.HighlightBlocks(player, 1, GetBlockHighlights(), GetBlockHighlightColors(), (workspace.ToolOffsetMode == EnumToolOffsetMode.Center) ? EnumHighlightBlocksMode.CenteredToBlockSelectionIndex : EnumHighlightBlocksMode.AttachedToBlockSelectionIndex, GetBlockHighlightShape(), 0.0625f);
	}

	public override void OnBreak(Vintagestory.ServerMods.WorldEdit.WorldEdit worldEdit, BlockSelection blockSel, ref EnumHandling handling)
	{
		OnBuild(worldEdit, ba.GetBlock(blockSel.Position).Id, blockSel, null);
	}

	public override void OnBuild(Vintagestory.ServerMods.WorldEdit.WorldEdit worldEdit, int oldBlockId, BlockSelection blockSel, ItemStack withItemStack)
	{
		base.OnBuild(worldEdit, oldBlockId, blockSel, withItemStack);
		foreach (KeyValuePair<BlockPos, ChiselBlockInEdit> val in blocksInEdit)
		{
			if (val.Value.isNew)
			{
				BlockEntityChisel obj = ba.GetBlockEntity(val.Value.be.Pos) as BlockEntityChisel;
				TreeAttribute tree = new TreeAttribute();
				val.Value.be.ToTreeAttributes(tree);
				obj.FromTreeAttributes(tree, worldEdit.sapi.World);
				obj.RebuildCuboidList();
				obj.MarkDirty(redrawOnClient: true);
			}
			else
			{
				val.Value.be.MarkDirty(redrawOnClient: true);
			}
		}
	}

	public override void PerformBrushAction(Vintagestory.ServerMods.WorldEdit.WorldEdit worldEdit, Block placedBlock, int oldBlockId, BlockSelection blockSel, BlockPos targetPos, ItemStack withItemStack)
	{
		if (base.BrushDim1 <= 0f)
		{
			return;
		}
		BlockFacing blockSelFace = blockSel.Face;
		targetPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSelFace.Opposite) : blockSel.Position);
		Block hereBlock = ba.GetBlock(targetPos);
		BlockChisel selectedBlock = hereBlock as BlockChisel;
		if (selectedBlock == null)
		{
			selectedBlock = ba.GetBlock(new AssetLocation("chiseledblock")) as BlockChisel;
		}
		if (withItemStack != null && withItemStack.Block != null && !ItemChisel.IsValidChiselingMaterial(worldEdit.sapi, targetPos, withItemStack.Block, worldEdit.sapi.World.PlayerByUid(workspace.PlayerUID)))
		{
			(worldEdit.sapi.World.PlayerByUid(workspace.PlayerUID) as IServerPlayer).SendIngameError("notmicroblock", Lang.Get("Must have a chisel material in hands"));
			return;
		}
		BlockEntityChisel targetBe = ba.GetBlockEntity(targetPos) as BlockEntityChisel;
		Vec3i voxelpos = new Vec3i(Math.Min(16, (int)(blockSel.HitPosition.X * 16.0)), Math.Min(16, (int)(blockSel.HitPosition.Y * 16.0)), Math.Min(16, (int)(blockSel.HitPosition.Z * 16.0)));
		Vec3i matpos = new Vec3i(Math.Min(15, (int)(blockSel.HitPosition.X * 16.0)), Math.Min(15, (int)(blockSel.HitPosition.Y * 16.0)), Math.Min(15, (int)(blockSel.HitPosition.Z * 16.0)));
		BlockPos[] attachmentPoints = new BlockPos[6];
		for (int j = 0; j < 6; j++)
		{
			Vec3i k = BlockFacing.ALLNORMALI[j];
			attachmentPoints[j] = new BlockPos((int)((float)size.X / 2f * (float)k.X), (int)((float)size.Y / 2f * (float)k.Y), (int)((float)size.Z / 2f * (float)k.Z));
		}
		if (workspace.ToolOffsetMode == EnumToolOffsetMode.Attach)
		{
			voxelpos.X += attachmentPoints[blockSel.Face.Index].X;
			voxelpos.Y += attachmentPoints[blockSel.Face.Index].Y;
			voxelpos.Z += attachmentPoints[blockSel.Face.Index].Z;
			if (attachmentPoints[blockSel.Face.Index].X < 0)
			{
				voxelpos.X--;
			}
			if (base.BrushShape == EnumBrushShape.Cuboid)
			{
				if (attachmentPoints[blockSel.Face.Index].X < 0 && base.BrushDim1 % 2f == 0f)
				{
					voxelpos.X++;
				}
				if (attachmentPoints[blockSel.Face.Index].Y < 0 && base.BrushDim2 % 2f != 0f)
				{
					voxelpos.Y--;
				}
				if (attachmentPoints[blockSel.Face.Index].Z < 0 && base.BrushDim3 % 2f != 0f)
				{
					voxelpos.Z--;
				}
				if (size.Y == 1 && blockSel.Face.Index == BlockFacing.DOWN.Index)
				{
					voxelpos.Y--;
				}
				if (size.X == 1 && blockSel.Face.Index == BlockFacing.WEST.Index)
				{
					voxelpos.X--;
				}
				if (size.Z == 1 && blockSel.Face.Index == BlockFacing.NORTH.Index)
				{
					voxelpos.Z--;
				}
			}
			else if (base.BrushShape == EnumBrushShape.Cylinder)
			{
				if (attachmentPoints[blockSel.Face.Index].Y < 0 && size.Y > 2)
				{
					voxelpos.Y--;
				}
				if (attachmentPoints[blockSel.Face.Index].Z < 0)
				{
					voxelpos.Z--;
				}
				if (size.Y == 1 && blockSel.Face.Index == BlockFacing.DOWN.Index)
				{
					voxelpos.Y--;
				}
			}
			else
			{
				if (attachmentPoints[blockSel.Face.Index].Y < 0)
				{
					voxelpos.Y--;
				}
				if (attachmentPoints[blockSel.Face.Index].Z < 0)
				{
					voxelpos.Z--;
				}
			}
		}
		if (oldBlockId >= 0)
		{
			if (placedBlock.ForFluidsLayer)
			{
				worldEdit.sapi.World.BlockAccessor.SetBlock(oldBlockId, blockSel.Position, 2);
			}
			else
			{
				worldEdit.sapi.World.BlockAccessor.SetBlock(oldBlockId, blockSel.Position);
			}
		}
		EnumBrushMode brushMode = base.BrushMode;
		int blockId = (withItemStack?.Block?.BlockId).GetValueOrDefault();
		if (!workspace.MayPlace(ba.GetBlock(blockId), brushPositions.Length))
		{
			return;
		}
		BlockPos tmpPos = new BlockPos();
		Vec3i dvoxelpos = new Vec3i();
		blocksInEdit.Clear();
		int selectedMatId = targetBe?.GetVoxelMaterialAt(matpos) ?? hereBlock.Id;
		for (int i = 0; i < brushPositions.Length; i++)
		{
			BlockPos brushPos = brushPositions[i];
			long voxelWorldX = targetPos.X * 16 + brushPos.X + voxelpos.X;
			long voxelWorldY = targetPos.Y * 16 + brushPos.Y + voxelpos.Y;
			long voxelWorldZ = targetPos.Z * 16 + brushPos.Z + voxelpos.Z;
			BlockPos dpos = tmpPos.Set((int)(voxelWorldX / 16), (int)(voxelWorldY / 16), (int)(voxelWorldZ / 16));
			dvoxelpos.Set((int)GameMath.Mod(voxelWorldX, 16f), (int)GameMath.Mod(voxelWorldY, 16f), (int)GameMath.Mod(voxelWorldZ, 16f));
			if (!blocksInEdit.TryGetValue(dpos, out var editData))
			{
				bool isNew = false;
				Block hereblock = ba.GetBlock(dpos);
				BlockEntityChisel be = ba.GetBlockEntity(dpos) as BlockEntityChisel;
				if (be == null)
				{
					if (withItemStack == null || ((brushMode != EnumBrushMode.ReplaceAir || hereblock.Id != 0) && brushMode != 0))
					{
						continue;
					}
					ba.SetBlock(selectedBlock.Id, dpos);
					string blockName = withItemStack.GetName();
					be = new BlockEntityChisel();
					be.Pos = dpos.Copy();
					be.CreateBehaviors(selectedBlock, worldEdit.sapi.World);
					be.Initialize(worldEdit.sapi);
					be.WasPlaced(withItemStack.Block, blockName);
					be.VoxelCuboids = new List<uint>();
					isNew = true;
				}
				int hereblockId = hereblock.Id;
				if (!isNew)
				{
					ba.SetHistoryStateBlock(dpos.X, dpos.Y, dpos.Z, hereblockId, hereblockId);
				}
				else
				{
					ba.SetHistoryStateBlock(dpos.X, dpos.Y, dpos.Z, 0, selectedBlock.Id);
				}
				be.BeginEdit(out var voxels, out var voxMats);
				editData = (blocksInEdit[dpos.Copy()] = new ChiselBlockInEdit
				{
					voxels = voxels,
					voxelMaterial = voxMats,
					be = be,
					isNew = isNew
				});
			}
			int hereMatBlockId = 0;
			if (editData.voxels[dvoxelpos.X, dvoxelpos.Y, dvoxelpos.Z])
			{
				hereMatBlockId = editData.be.BlockIds[editData.voxelMaterial[dvoxelpos.X, dvoxelpos.Y, dvoxelpos.Z]];
			}
			if (brushMode switch
			{
				EnumBrushMode.ReplaceAir => (hereMatBlockId == 0) ? 1 : 0, 
				EnumBrushMode.ReplaceNonAir => (hereMatBlockId != 0) ? 1 : 0, 
				EnumBrushMode.ReplaceSelected => (hereMatBlockId == selectedMatId) ? 1 : 0, 
				_ => 1, 
			} == 0)
			{
				continue;
			}
			if (blockId == 0)
			{
				editData.voxels[dvoxelpos.X, dvoxelpos.Y, dvoxelpos.Z] = false;
				continue;
			}
			int matId = editData.be.BlockIds.IndexOf(blockId);
			if (matId < 0)
			{
				matId = editData.be.AddMaterial(ba.GetBlock(blockId));
			}
			editData.voxels[dvoxelpos.X, dvoxelpos.Y, dvoxelpos.Z] = true;
			editData.voxelMaterial[dvoxelpos.X, dvoxelpos.Y, dvoxelpos.Z] = (byte)matId;
		}
		foreach (KeyValuePair<BlockPos, ChiselBlockInEdit> val in blocksInEdit)
		{
			val.Value.be.EndEdit(val.Value.voxels, val.Value.voxelMaterial);
			if (val.Value.be.VoxelCuboids.Count == 0)
			{
				ba.SetBlock(0, val.Key);
				ba.RemoveBlockLight(val.Value.be.GetLightHsv(ba), val.Key);
			}
		}
	}
}
