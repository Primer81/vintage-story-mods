using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockBloomery : Block, IIgnitable
{
	private WorldInteraction[] interactions;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		ICoreClientAPI capi = api as ICoreClientAPI;
		interactions = ObjectCacheUtil.GetOrCreate(api, "bloomeryBlockInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			List<ItemStack> list2 = new List<ItemStack>();
			List<ItemStack> list3 = BlockBehaviorCanIgnite.CanIgniteStacks(api, withFirestarter: false);
			foreach (CollectibleObject current in api.World.Collectibles)
			{
				if (current.CombustibleProps != null)
				{
					if (current.CombustibleProps.SmeltedStack != null && current.CombustibleProps.MeltingPoint < 1500)
					{
						List<ItemStack> handBookStacks = current.GetHandBookStacks(capi);
						if (handBookStacks != null)
						{
							list.AddRange(handBookStacks);
						}
					}
					else if (current.CombustibleProps.BurnTemperature >= 1200 && current.CombustibleProps.BurnDuration > 30f)
					{
						List<ItemStack> handBookStacks2 = current.GetHandBookStacks(capi);
						if (handBookStacks2 != null)
						{
							list2.AddRange(handBookStacks2);
						}
					}
				}
			}
			return new WorldInteraction[4]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-bloomery-heatable",
					HotKeyCode = null,
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray(),
					GetMatchingStacks = getMatchingStacks
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-bloomery-heatablex4",
					HotKeyCode = "ctrl",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray(),
					GetMatchingStacks = getMatchingStacks
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-bloomery-fuel",
					HotKeyCode = null,
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list2.ToArray(),
					GetMatchingStacks = getMatchingStacks
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-bloomery-ignite",
					HotKeyCode = "shift",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list3.ToArray(),
					GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityBloomery blockEntityBloomery && blockEntityBloomery.CanIgnite() && !blockEntityBloomery.IsBurning && api.World.BlockAccessor.GetBlock(bs.Position.UpCopy()).Code.Path.Contains("bloomerychimney")) ? wi.Itemstacks : null
				}
			};
		});
	}

	private ItemStack[] getMatchingStacks(WorldInteraction wi, BlockSelection blockSelection, EntitySelection entitySelection)
	{
		if (!(api.World.BlockAccessor.GetBlockEntity(blockSelection.Position) is BlockEntityBloomery beb) || wi.Itemstacks.Length == 0)
		{
			return null;
		}
		List<ItemStack> matchStacks = new List<ItemStack>();
		ItemStack[] itemstacks = wi.Itemstacks;
		foreach (ItemStack stack in itemstacks)
		{
			if (beb.CanAdd(stack))
			{
				matchStacks.Add(stack);
			}
		}
		return matchStacks.ToArray();
	}

	EnumIgniteState IIgnitable.OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting)
	{
		return EnumIgniteState.NotIgnitable;
	}

	public EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
	{
		if (!(byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityBloomery).CanIgnite())
		{
			return EnumIgniteState.NotIgnitablePreventDefault;
		}
		if (!(secondsIgniting > 4f))
		{
			return EnumIgniteState.Ignitable;
		}
		return EnumIgniteState.IgniteNow;
	}

	public void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		(byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityBloomery)?.TryIgnite();
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemStack hotbarstack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
		if (hotbarstack != null && hotbarstack.Class == EnumItemClass.Block && hotbarstack.Collectible.Code.PathStartsWith("bloomerychimney"))
		{
			if (world.BlockAccessor.GetBlock(blockSel.Position.UpCopy()).IsReplacableBy(hotbarstack.Block))
			{
				hotbarstack.Block.DoPlaceBlock(world, byPlayer, new BlockSelection
				{
					Position = blockSel.Position.UpCopy(),
					Face = BlockFacing.UP
				}, hotbarstack);
				world.PlaySoundAt(Sounds?.Place, blockSel.Position, 0.5, byPlayer, randomizePitch: true, 16f);
				if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
				{
					byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(1);
				}
			}
			return true;
		}
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityBloomery beb)
		{
			if (hotbarstack == null)
			{
				return true;
			}
			if (beb.TryAdd(byPlayer, (!byPlayer.Entity.Controls.CtrlKey) ? 1 : 5) && world.Side == EnumAppSide.Client)
			{
				(byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			}
		}
		return true;
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		Block aboveBlock = world.BlockAccessor.GetBlock(pos.UpCopy());
		if (aboveBlock.Code.Path == "bloomerychimney")
		{
			aboveBlock.OnBlockBroken(world, pos.UpCopy(), byPlayer, dropQuantityMultiplier);
		}
		base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return GetHandbookDropsFromBreakDrops(handbookStack, forPlayer);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
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
		return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}
}
