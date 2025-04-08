using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockCheese : Block
{
	private WorldInteraction[] interactions;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		InteractionHelpYOffset = 0.375f;
		interactions = ObjectCacheUtil.GetOrCreate(api, "cheeseInteractions-", () => new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-cheese-cut",
				MouseButton = EnumMouseButton.Right,
				Itemstacks = BlockUtil.GetKnifeStacks(api),
				GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (api.World.BlockAccessor.GetBlockEntity(bs.Position) is BECheese { SlicesLeft: >1 }) ? wi.Itemstacks : null
			}
		});
	}

	public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
	{
		ICoreClientAPI capi = api as ICoreClientAPI;
		if (world.BlockAccessor.GetBlockEntity(pos) is BECheese bec)
		{
			Shape shape = capi.TesselatorManager.GetCachedShape(bec.Inventory[0].Itemstack.Item.Shape.Base);
			capi.Tesselator.TesselateShape(this, shape, out blockModelData);
			blockModelData.Scale(new Vec3f(0.5f, 0f, 0.5f), 0.75f, 0.75f, 0.75f);
			capi.Tesselator.TesselateShape("cheese decal", shape, out decalModelData, decalTexSource, null, 0, 0, 0);
			decalModelData.Scale(new Vec3f(0.5f, 0f, 0.5f), 0.75f, 0.75f, 0.75f);
		}
		base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
	}

	public override void OnDecalTesselation(IWorldAccessor world, MeshData decalMesh, BlockPos pos)
	{
		base.OnDecalTesselation(world, decalMesh, pos);
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BECheese bec)
		{
			return bec.Inventory[0].Itemstack;
		}
		return base.OnPickBlock(world, pos);
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		EnumTool? tool = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack?.Collectible.Tool;
		if (tool == EnumTool.Knife || tool.GetValueOrDefault() == EnumTool.Sword)
		{
			BECheese bec = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BECheese;
			if (bec.Inventory[0].Itemstack?.Collectible.Variant["type"] == "waxedcheddar")
			{
				ItemStack newStack = new ItemStack(api.World.GetItem(bec.Inventory[0].Itemstack?.Collectible.CodeWithVariant("type", "cheddar")));
				TransitionableProperties perishProps = newStack.Collectible.GetTransitionableProperties(api.World, newStack, null).FirstOrDefault((TransitionableProperties p) => p.Type == EnumTransitionType.Perish);
				perishProps.TransitionedStack.Resolve(api.World, "pie perished stack");
				CollectibleObject.CarryOverFreshness(api, bec.Inventory[0], newStack, perishProps);
				bec.Inventory[0].Itemstack = newStack;
				bec.Inventory[0].MarkDirty();
				bec.MarkDirty(redrawOnClient: true);
				return true;
			}
			ItemStack stack = bec?.TakeSlice();
			if (stack != null)
			{
				if (!byPlayer.InventoryManager.TryGiveItemstack(stack, slotNotifyEffect: true))
				{
					world.SpawnItemEntity(stack, blockSel.Position);
				}
				world.Logger.Audit("{0} Took 1x{1} from Cheese at {2}.", byPlayer.PlayerName, stack.Collectible.Code, blockSel.Position);
			}
			return true;
		}
		ItemStack stack2 = (world.BlockAccessor.GetBlockEntity(blockSel.Position) as BECheese).Inventory[0].Itemstack;
		if (stack2 != null)
		{
			if (!byPlayer.InventoryManager.TryGiveItemstack(stack2, slotNotifyEffect: true))
			{
				world.SpawnItemEntity(stack2, blockSel.Position);
			}
			world.Logger.Audit("{0} Took 1x{1} from Cheese at {2}.", byPlayer.PlayerName, stack2.Collectible.Code, blockSel.Position);
		}
		world.BlockAccessor.SetBlock(0, blockSel.Position);
		return true;
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}
}
