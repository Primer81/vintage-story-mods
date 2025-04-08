using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityBlockRandomizer : BlockEntityContainer
{
	private const int quantitySlots = 10;

	private ICoreClientAPI capi;

	public float[] Chances = new float[10];

	private InventoryGeneric inventory;

	private static AssetLocation airFillerblockCode = new AssetLocation("meta-filler");

	public override InventoryBase Inventory => inventory;

	public override string InventoryClassName => "randomizer";

	public BlockEntityBlockRandomizer()
	{
		inventory = new InventoryGeneric(10, null, null);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		capi = api as ICoreClientAPI;
		if (inventory == null)
		{
			InitInventory(base.Block);
		}
	}

	protected virtual void InitInventory(Block block)
	{
		inventory = new InventoryGeneric(10, null, null);
		inventory.BaseWeight = 1f;
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		if (byItemStack != null && byItemStack.Attributes.HasAttribute("chances"))
		{
			Chances = (byItemStack.Attributes["chances"] as FloatArrayAttribute).value;
			inventory.FromTreeAttributes(byItemStack.Attributes);
		}
	}

	protected override void OnTick(float dt)
	{
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		BlockCrate block = worldForResolving.GetBlock(new AssetLocation(tree.GetString("blockCode"))) as BlockCrate;
		if (inventory == null)
		{
			if (tree.HasAttribute("blockCode"))
			{
				InitInventory(block);
			}
			else
			{
				InitInventory(null);
			}
		}
		Chances = (tree["chances"] as FloatArrayAttribute).value;
		if (Chances == null)
		{
			Chances = new float[10];
		}
		if (Chances.Length < 10)
		{
			Chances = Chances.Append(ArrayUtil.CreateFilled(10 - Chances.Length, (int i) => 0f));
		}
		base.FromTreeAttributes(tree, worldForResolving);
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree["chances"] = new FloatArrayAttribute(Chances);
	}

	public void OnInteract(IPlayer byPlayer)
	{
		if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative && Api.Side == EnumAppSide.Client)
		{
			GuiDialogItemLootRandomizer dlg = new GuiDialogItemLootRandomizer(inventory, Chances, capi, "Block randomizer");
			dlg.TryOpen();
			dlg.OnClosed += delegate
			{
				DidCloseLootRandomizer(dlg);
			};
		}
	}

	private void DidCloseLootRandomizer(GuiDialogItemLootRandomizer dialog)
	{
		ITreeAttribute attr = dialog.Attributes;
		if (attr.GetInt("save") == 0)
		{
			return;
		}
		using MemoryStream ms = new MemoryStream();
		BinaryWriter writer = new BinaryWriter(ms);
		attr.ToBytes(writer);
		capi.Network.SendBlockEntityPacket(Pos, 1130, ms.ToArray());
	}

	public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
	{
		base.OnReceivedClientPacket(fromPlayer, packetid, data);
		if (packetid != 1130)
		{
			return;
		}
		TreeAttribute tree = new TreeAttribute();
		tree.FromBytes(data);
		for (int i = 0; i < 10; i++)
		{
			if (tree["stack" + i] is TreeAttribute stree)
			{
				Chances[i] = stree.GetFloat("chance");
				ItemStack stack = stree.GetItemstack("stack");
				stack.ResolveBlockOrItem(Api.World);
				inventory[i].Itemstack = stack;
			}
		}
		MarkDirty();
	}

	public override void OnPlacementBySchematic(ICoreServerAPI api, IBlockAccessor blockAccessor, BlockPos pos, Dictionary<int, Dictionary<int, int>> replaceBlocks, int centerrockblockid, Block layerBlock, bool resolveImports)
	{
		base.OnPlacementBySchematic(api, blockAccessor, pos, replaceBlocks, centerrockblockid, layerBlock, resolveImports);
		if (!resolveImports)
		{
			return;
		}
		IBlockAccessor ba = ((blockAccessor is IBlockAccessorRevertable) ? api.World.BlockAccessor : blockAccessor);
		float sumchance = 0f;
		for (int j = 0; j < 10; j++)
		{
			sumchance += Chances[j];
		}
		double rnd = api.World.Rand.NextDouble() * (double)Math.Max(100f, sumchance);
		for (int i = 0; i < 10; i++)
		{
			Block block = inventory[i].Itemstack?.Block;
			rnd -= (double)Chances[i];
			if (!(rnd <= 0.0) || block == null)
			{
				continue;
			}
			if (block.Code == airFillerblockCode)
			{
				ba.SetBlock(0, pos);
				return;
			}
			if (block.Id == BlockMicroBlock.BlockLayerMetaBlockId)
			{
				ba.SetBlock(layerBlock?.Id ?? 0, pos);
				return;
			}
			if (replaceBlocks != null && replaceBlocks.TryGetValue(block.Id, out var replaceByBlock) && replaceByBlock.TryGetValue(centerrockblockid, out var newBlockId))
			{
				block = blockAccessor.GetBlock(newBlockId);
			}
			if (block.GetBehavior<BlockBehaviorHorizontalAttachable>() != null)
			{
				int offset = api.World.Rand.Next(BlockFacing.HORIZONTALS.Length);
				for (int index = 0; index < BlockFacing.HORIZONTALS.Length; index++)
				{
					BlockFacing facing = BlockFacing.HORIZONTALS[(index + offset) % BlockFacing.HORIZONTALS.Length];
					Block newBlock = ba.GetBlock(block.CodeWithParts(facing.Code));
					BlockPos attachingBlockPos = pos.AddCopy(facing);
					if (ba.GetBlock(attachingBlockPos).CanAttachBlockAt(ba, newBlock, pos, facing))
					{
						ba.SetBlock(newBlock.Id, pos);
						break;
					}
				}
			}
			else
			{
				ba.SetBlock(block.Id, pos);
				if (blockAccessor is IWorldGenBlockAccessor && block.EntityClass != null)
				{
					blockAccessor.SpawnBlockEntity(block.EntityClass, pos);
				}
				BlockEntity blockEntity = blockAccessor.GetBlockEntity(pos);
				blockEntity?.Initialize(api);
				blockEntity?.OnBlockPlaced(inventory[i].Itemstack);
			}
			return;
		}
		ba.SetBlock(0, pos);
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
	}
}
