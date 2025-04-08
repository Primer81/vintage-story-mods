using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockPlantContainer : Block
{
	private WorldInteraction[] interactions = new WorldInteraction[0];

	public string ContainerSize => Attributes["plantContainerSize"].AsString();

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		LoadColorMapAnyway = true;
		List<ItemStack> stacks = new List<ItemStack>();
		if (Variant["contents"] != "empty")
		{
			return;
		}
		foreach (Block block in api.World.Blocks)
		{
			if (!block.IsMissing)
			{
				JsonObject attributes = block.Attributes;
				if (attributes != null && attributes["plantContainable"].Exists)
				{
					stacks.Add(new ItemStack(block));
				}
			}
		}
		foreach (Item item in api.World.Items)
		{
			if (!(item.Code == null) && !item.IsMissing)
			{
				JsonObject attributes2 = item.Attributes;
				if (attributes2 != null && attributes2["plantContainable"].Exists)
				{
					stacks.Add(new ItemStack(item));
				}
			}
		}
		interactions = new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-flowerpot-plant",
				MouseButton = EnumMouseButton.Right,
				Itemstacks = stacks.ToArray()
			}
		};
	}

	public ItemStack GetContents(IWorldAccessor world, BlockPos pos)
	{
		return (world.BlockAccessor.GetBlockEntity(pos) as BlockEntityPlantContainer)?.GetContents();
	}

	public override void OnDecalTesselation(IWorldAccessor world, MeshData decalMesh, BlockPos pos)
	{
		base.OnDecalTesselation(world, decalMesh, pos);
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityPlantContainer bept)
		{
			decalMesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, bept.MeshAngle, 0f);
		}
	}

	public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
	{
		bool num = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
		if (num && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityPlantContainer bect)
		{
			BlockPos targetPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
			double y = byPlayer.Entity.Pos.X - ((double)targetPos.X + blockSel.HitPosition.X);
			double dz = (double)(float)byPlayer.Entity.Pos.Z - ((double)targetPos.Z + blockSel.HitPosition.Z);
			float num2 = (float)Math.Atan2(y, dz);
			float deg22dot5rad = (float)Math.PI / 8f;
			float roundRad = (float)(int)Math.Round(num2 / deg22dot5rad) * deg22dot5rad;
			bect.MeshAngle = roundRad;
		}
		return num;
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		base.OnBlockBroken(world, pos, byPlayer);
		ItemStack contents = GetContents(world, pos);
		if (contents != null)
		{
			world.SpawnItemEntity(contents, pos);
		}
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return GetHandbookDropsFromBreakDrops(handbookStack, forPlayer);
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		return base.OnPickBlock(world, pos);
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		BlockEntityPlantContainer be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityPlantContainer;
		IPlayerInventoryManager inventoryManager = byPlayer.InventoryManager;
		if (inventoryManager != null && inventoryManager.ActiveHotbarSlot?.Empty == false && be != null)
		{
			return be.TryPutContents(byPlayer.InventoryManager.ActiveHotbarSlot, byPlayer);
		}
		return false;
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}
}
