using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockSkep : Block
{
	private float beemobSpawnChance = 0.4f;

	public bool IsEmpty()
	{
		return Variant["type"] == "empty";
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		string collectibleCode = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack?.Collectible.Code.Path;
		if (collectibleCode == "beenade-opened" || collectibleCode == "beenade-closed")
		{
			return false;
		}
		if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative)
		{
			ItemStack itemstack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
			if (itemstack != null && itemstack.Collectible.Code.Path.Contains("honeycomb"))
			{
				if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityBeehive { Harvestable: false } beh)
				{
					beh.Harvestable = true;
					beh.MarkDirty(redrawOnClient: true);
				}
				return true;
			}
		}
		if (byPlayer.InventoryManager.TryGiveItemstack(new ItemStack(world.BlockAccessor.GetBlock(CodeWithVariant("side", "east")))))
		{
			world.BlockAccessor.SetBlock(0, blockSel.Position);
			world.PlaySoundAt(new AssetLocation("sounds/block/planks"), blockSel.Position, -0.5, byPlayer, randomizePitch: false);
			return true;
		}
		return false;
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		beemobSpawnChance = Attributes?["beemobSpawnChance"].AsFloat(0.4f) ?? 0.4f;
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
		if (world.Side == EnumAppSide.Server && !IsEmpty() && world.Rand.NextDouble() < (double)beemobSpawnChance)
		{
			EntityProperties type = world.GetEntityType(new AssetLocation("beemob"));
			Entity entity = world.ClassRegistry.CreateEntity(type);
			if (entity != null)
			{
				entity.ServerPos.X = (float)pos.X + 0.5f;
				entity.ServerPos.Y = (float)pos.Y + 0.5f;
				entity.ServerPos.Z = (float)pos.Z + 0.5f;
				entity.ServerPos.Yaw = (float)world.Rand.NextDouble() * 2f * (float)Math.PI;
				entity.Pos.SetFrom(entity.ServerPos);
				entity.Attributes.SetString("origin", "brokenbeehive");
				world.SpawnEntity(entity);
			}
		}
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return GetHandbookDropsFromBreakDrops(handbookStack, forPlayer);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		if (IsEmpty())
		{
			return new ItemStack[1]
			{
				new ItemStack(world.BlockAccessor.GetBlock(CodeWithVariant("side", "east")))
			};
		}
		if (!(world.BlockAccessor.GetBlockEntity(pos) is BlockEntityBeehive { Harvestable: not false }))
		{
			return new ItemStack[1]
			{
				new ItemStack(world.BlockAccessor.GetBlock(CodeWithVariant("side", "east")))
			};
		}
		if (Drops == null)
		{
			return null;
		}
		List<ItemStack> todrop = new List<ItemStack>();
		for (int i = 0; i < Drops.Length; i++)
		{
			if (Drops[i].Tool.HasValue && (byPlayer == null || Drops[i].Tool != byPlayer.InventoryManager.ActiveTool))
			{
				continue;
			}
			ItemStack stack = Drops[i].GetNextItemStack(dropQuantityMultiplier);
			if (stack != null)
			{
				todrop.Add(stack);
				if (Drops[i].LastDrop)
				{
					break;
				}
			}
		}
		return todrop.ToArray();
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		WorldInteraction wi = new WorldInteraction
		{
			ActionLangCode = ((Variant["type"] == "populated") ? "blockhelp-skep-putinbagslot" : "blockhelp-skep-pickup"),
			MouseButton = EnumMouseButton.Right
		};
		BlockEntityBeehive obj = world.BlockAccessor.GetBlockEntity(selection.Position) as BlockEntityBeehive;
		if (obj != null && obj.Harvestable)
		{
			return new WorldInteraction[2]
			{
				wi,
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-skep-harvest",
					MouseButton = EnumMouseButton.Left
				}
			}.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
		}
		return new WorldInteraction[1] { wi }.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}
}
