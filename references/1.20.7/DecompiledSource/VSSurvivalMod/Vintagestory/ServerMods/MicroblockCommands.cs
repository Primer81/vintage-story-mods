using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Vintagestory.ServerMods;

public class MicroblockCommands
{
	private ICoreServerAPI sapi;

	private Block materialBlock;

	public void Start(ICoreServerAPI api)
	{
		sapi = api;
		CommandArgumentParsers parsers = sapi.ChatCommands.Parsers;
		api.ChatCommands.GetOrCreate("we").BeginSub("microblock").WithDesc("Microblock operations")
			.BeginSub("fill")
			.WithDesc("Fill empty space of microblocks with held block")
			.HandleWith((TextCommandCallingArgs args) => onCmdFill(args, delete: false))
			.EndSub()
			.BeginSub("clearname")
			.WithDesc("Delete all block names")
			.HandleWith((TextCommandCallingArgs args) => onCmdClearName(args, v: false))
			.EndSub()
			.BeginSub("setname")
			.WithDesc("Set multiple block names")
			.WithArgs(parsers.All("name"))
			.HandleWith((TextCommandCallingArgs args) => onCmdSetName(args, v: false))
			.EndSub()
			.BeginSub("delete")
			.WithDesc("Delete a material from microblocks (select material with held block)")
			.HandleWith((TextCommandCallingArgs args) => onCmdFill(args, delete: true))
			.EndSub()
			.BeginSub("deletemat")
			.WithDesc("Delete a named material from microblocks")
			.WithArgs(parsers.Word("material code"))
			.HandleWith(onCmdDeleteMat)
			.EndSub()
			.BeginSub("removeunused")
			.WithDesc("Remove any unused materials from microblocks")
			.HandleWith(onCmdRemoveUnused)
			.EndSub()
			.BeginSubCommand("editable")
			.WithDescription("Upgrade/Downgrade chiseled blocks to an editable/non-editable state in given area")
			.WithArgs(parsers.Bool("editable"))
			.HandleWith(onCmdEditable)
			.EndSubCommand()
			.EndSub();
	}

	private int WalkMicroBlocks(BlockPos startPos, BlockPos endPos, ActionBoolReturn<BlockEntityMicroBlock> action)
	{
		int cnt = 0;
		IBlockAccessor ba = sapi.World.BlockAccessor;
		BlockPos tmpPos = new BlockPos(startPos.dimension);
		BlockPos.Walk(startPos, endPos, ba.MapSize, delegate(int x, int y, int z)
		{
			tmpPos.Set(x, y, z);
			BlockEntityMicroBlock blockEntity = ba.GetBlockEntity<BlockEntityMicroBlock>(tmpPos);
			if (blockEntity != null && action(blockEntity))
			{
				cnt++;
			}
		});
		return cnt;
	}

	private void GetMarkedArea(Caller caller, out BlockPos startPos, out BlockPos endPos)
	{
		string uid = caller.Player.PlayerUID;
		startPos = null;
		endPos = null;
		if (sapi.ObjectCache.TryGetValue("weStartMarker-" + uid, out var start))
		{
			startPos = start as BlockPos;
		}
		if (sapi.ObjectCache.TryGetValue("weEndMarker-" + uid, out var end))
		{
			endPos = end as BlockPos;
		}
	}

	private TextCommandResult onCmdSetName(TextCommandCallingArgs args, bool v)
	{
		string name = args[0] as string;
		GetMarkedArea(args.Caller, out var startPos, out var endPos);
		if (startPos == null || endPos == null)
		{
			return TextCommandResult.Error("Please mark area with world edit");
		}
		return TextCommandResult.Success(WalkMicroBlocks(startPos, endPos, delegate(BlockEntityMicroBlock be)
		{
			be.BlockName = name;
			be.MarkDirty(redrawOnClient: true);
			return true;
		}) + " microblocks modified");
	}

	private TextCommandResult onCmdClearName(TextCommandCallingArgs args, bool v)
	{
		GetMarkedArea(args.Caller, out var startPos, out var endPos);
		if (startPos == null || endPos == null)
		{
			return TextCommandResult.Error("Please mark area with world edit");
		}
		return TextCommandResult.Success(WalkMicroBlocks(startPos, endPos, delegate(BlockEntityMicroBlock be)
		{
			if (be.BlockName != null && be.BlockName != "")
			{
				be.BlockName = null;
				be.MarkDirty(redrawOnClient: true);
				return true;
			}
			return false;
		}) + " microblocks modified");
	}

	private TextCommandResult onCmdFill(TextCommandCallingArgs args, bool delete)
	{
		GetMarkedArea(args.Caller, out var startPos, out var endPos);
		if (startPos == null || endPos == null)
		{
			return TextCommandResult.Error("Please mark area with world edit");
		}
		Block fillWitBlock = args.Caller.Player?.InventoryManager.ActiveHotbarSlot.Itemstack?.Block;
		if (fillWitBlock == null)
		{
			return TextCommandResult.Error("Please hold replacement material in active hands");
		}
		if (fillWitBlock is BlockMicroBlock)
		{
			return TextCommandResult.Error("Cannot use micro block as a material inside microblocks");
		}
		materialBlock = fillWitBlock;
		return TextCommandResult.Success(WalkMicroBlocks(startPos, endPos, delete ? new ActionBoolReturn<BlockEntityMicroBlock>(DeleteMaterial) : new ActionBoolReturn<BlockEntityMicroBlock>(FillMaterial)) + " microblocks modified");
	}

	private TextCommandResult onCmdDeleteMat(TextCommandCallingArgs args)
	{
		GetMarkedArea(args.Caller, out var startPos, out var endPos);
		if (startPos == null || endPos == null)
		{
			return TextCommandResult.Error("Please mark area with world edit");
		}
		string matCode = (string)args[0];
		Block fillWitBlock = sapi.World.BlockAccessor.GetBlock(new AssetLocation(matCode));
		if (fillWitBlock == null)
		{
			return TextCommandResult.Error("Unknown block code: " + matCode);
		}
		if (fillWitBlock is BlockMicroBlock)
		{
			return TextCommandResult.Error("Cannot use micro block as a material inside microblocks");
		}
		materialBlock = fillWitBlock;
		return TextCommandResult.Success(WalkMicroBlocks(startPos, endPos, DeleteMaterial) + " microblocks modified");
	}

	private TextCommandResult onCmdRemoveUnused(TextCommandCallingArgs args)
	{
		GetMarkedArea(args.Caller, out var startPos, out var endPos);
		if (startPos == null || endPos == null)
		{
			return TextCommandResult.Error("Please mark area with world edit");
		}
		HashSet<string> removedMaterials = new HashSet<string>();
		string message = WalkMicroBlocks(startPos, endPos, (BlockEntityMicroBlock be) => RemoveUnused(be, removedMaterials)) + " microblocks modified";
		if (removedMaterials.Count > 0)
		{
			message += ", removed materials: ";
			bool comma = false;
			foreach (string mat in removedMaterials)
			{
				if (comma)
				{
					message += ", ";
				}
				else
				{
					comma = true;
				}
				message += mat;
			}
		}
		return TextCommandResult.Success(message);
	}

	private bool DeleteMaterial(BlockEntityMicroBlock be)
	{
		int delmatindex = be.BlockIds.IndexOf(materialBlock.Id);
		if (delmatindex < 0)
		{
			return false;
		}
		List<uint> cwms = new List<uint>();
		List<int> blockids = new List<int>();
		CuboidWithMaterial cwm = new CuboidWithMaterial();
		for (int i = 0; i < be.VoxelCuboids.Count; i++)
		{
			BlockEntityMicroBlock.FromUint(be.VoxelCuboids[i], cwm);
			if (delmatindex != cwm.Material)
			{
				int blockId = be.BlockIds[cwm.Material];
				int matindex = blockids.IndexOf(blockId);
				if (matindex < 0)
				{
					blockids.Add(blockId);
					matindex = blockids.Count - 1;
				}
				cwm.Material = (byte)matindex;
				cwms.Add(BlockEntityMicroBlock.ToUint(cwm));
			}
		}
		be.VoxelCuboids = cwms;
		be.BlockIds = blockids.ToArray();
		be.MarkDirty(redrawOnClient: true);
		return true;
	}

	private bool FillMaterial(BlockEntityMicroBlock be)
	{
		be.BeginEdit(out var voxels, out var voxMats);
		if (fillMicroblock(materialBlock, be, voxels, voxMats))
		{
			be.EndEdit(voxels, voxMats);
			be.MarkDirty(redrawOnClient: true);
			return true;
		}
		return false;
	}

	private static bool fillMicroblock(Block fillWitBlock, BlockEntityMicroBlock be, BoolArray16x16x16 voxels, byte[,,] voxMats)
	{
		bool edited = false;
		byte matIndex = 0;
		for (int dx = 0; dx < 16; dx++)
		{
			for (int dy = 0; dy < 16; dy++)
			{
				for (int dz = 0; dz < 16; dz++)
				{
					if (voxels[dx, dy, dz])
					{
						continue;
					}
					if (!edited)
					{
						int index = be.BlockIds.IndexOf(fillWitBlock.Id);
						if (be is BlockEntityChisel bec)
						{
							index = bec.AddMaterial(fillWitBlock);
						}
						else if (index < 0)
						{
							be.BlockIds = be.BlockIds.Append(fillWitBlock.Id);
							index = be.BlockIds.Length - 1;
						}
						matIndex = (byte)index;
					}
					voxels[dx, dy, dz] = true;
					voxMats[dx, dy, dz] = matIndex;
					edited = true;
				}
			}
		}
		return edited;
	}

	private bool RemoveUnused(BlockEntityMicroBlock be, HashSet<string> materialsRemoved)
	{
		bool edited = false;
		for (int i = 0; i < be.BlockIds.Length; i++)
		{
			if (be.NoVoxelsWithMaterial((uint)i))
			{
				Block material = sapi.World.BlockAccessor.GetBlock(be.BlockIds[i]);
				be.RemoveMaterial(material);
				materialsRemoved.Add(material.Code.ToShortString());
				edited = true;
				i--;
			}
		}
		if (edited)
		{
			be.MarkDirty(redrawOnClient: true);
		}
		return edited;
	}

	private TextCommandResult onCmdEditable(TextCommandCallingArgs args)
	{
		GetMarkedArea(args.Caller, out var startPos, out var endPos);
		if (startPos == null || endPos == null)
		{
			return TextCommandResult.Error("Please mark area with world edit");
		}
		bool editable = (bool)args.Parsers[0].GetValue();
		Block chiselBlock = sapi.World.GetBlock(new AssetLocation("chiseledblock"));
		Block microblock = sapi.World.GetBlock(new AssetLocation("microblock"));
		Block targetBlock = (editable ? chiselBlock : microblock);
		IBlockAccessor ba = sapi.World.BlockAccessor;
		return TextCommandResult.Success(WalkMicroBlocks(startPos, endPos, delegate(BlockEntityMicroBlock be)
		{
			BlockPos pos = be.Pos;
			Block block = ba.GetBlock(pos);
			if (block is BlockMicroBlock && block.Id != targetBlock.Id)
			{
				TreeAttribute tree = new TreeAttribute();
				be.ToTreeAttributes(tree);
				sapi.World.BlockAccessor.SetBlock(targetBlock.Id, pos);
				be = sapi.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityMicroBlock;
				be.FromTreeAttributes(tree, sapi.World);
				return true;
			}
			return false;
		}) + " microblocks modified");
	}
}
