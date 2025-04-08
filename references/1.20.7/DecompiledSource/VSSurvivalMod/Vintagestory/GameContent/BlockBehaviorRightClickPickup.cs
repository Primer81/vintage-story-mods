using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockBehaviorRightClickPickup : BlockBehavior
{
	private bool dropsPickupMode;

	private AssetLocation pickupSound;

	public BlockBehaviorRightClickPickup(Block block)
		: base(block)
	{
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);
		dropsPickupMode = properties["dropsPickupMode"].AsBool();
		string strloc = properties["sound"].AsString();
		if (strloc == null)
		{
			strloc = block.Attributes?["placeSound"].AsString();
		}
		pickupSound = ((strloc == null) ? null : AssetLocation.Create(strloc, block.Code.Domain));
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
	{
		ItemStack[] dropStacks = new ItemStack[1] { block.OnPickBlock(world, blockSel.Position) };
		ItemSlot activeSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		bool heldSlotSuitable = activeSlot.Empty || (dropStacks.Length >= 1 && activeSlot.Itemstack.Equals(world, dropStacks[0], GlobalConstants.IgnoredStackAttributes));
		if (dropsPickupMode)
		{
			float dropMul = 1f;
			JsonObject attributes = block.Attributes;
			if (attributes != null && attributes.IsTrue("forageStatAffected"))
			{
				dropMul *= byPlayer.Entity.Stats.GetBlended("forageDropRate");
			}
			dropStacks = block.GetDrops(world, blockSel.Position, byPlayer, dropMul);
			BlockDropItemStack[] alldrops = block.GetDropsForHandbook(new ItemStack(block), byPlayer);
			if (!heldSlotSuitable)
			{
				BlockDropItemStack[] array = alldrops;
				foreach (BlockDropItemStack drop in array)
				{
					heldSlotSuitable |= activeSlot.Itemstack.Equals(world, drop.ResolvedItemstack, GlobalConstants.IgnoredStackAttributes);
				}
			}
		}
		if (!heldSlotSuitable || !world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
		{
			return false;
		}
		if (!byPlayer.Entity.Controls.ShiftKey)
		{
			if (world.Side == EnumAppSide.Server && BlockBehaviorReinforcable.AllowRightClickPickup(world, blockSel.Position, byPlayer))
			{
				bool blockToBreak = true;
				ItemStack[] array2 = dropStacks;
				foreach (ItemStack stack in array2)
				{
					ItemStack origStack = stack.Clone();
					if (!byPlayer.InventoryManager.TryGiveItemstack(stack, slotNotifyEffect: true))
					{
						world.SpawnItemEntity(stack, blockSel.Position.ToVec3d().AddCopy(0.5, 0.1, 0.5));
					}
					world.Logger.Audit("{0} Took {1}x{2} from Ground at {3}.", byPlayer.PlayerName, origStack.StackSize, stack.Collectible.Code, blockSel.Position);
					TreeAttribute tree = new TreeAttribute();
					tree["itemstack"] = new ItemstackAttribute(origStack.Clone());
					tree["byentityid"] = new LongAttribute(byPlayer.Entity.EntityId);
					world.Api.Event.PushEvent("onitemcollected", tree);
					if (blockToBreak)
					{
						blockToBreak = false;
						world.BlockAccessor.SetBlock(0, blockSel.Position);
						world.BlockAccessor.TriggerNeighbourBlockUpdate(blockSel.Position);
					}
					world.PlaySoundAt(pickupSound ?? block.GetSounds(world.BlockAccessor, blockSel).Place, byPlayer);
				}
			}
			handling = EnumHandling.PreventDefault;
			return true;
		}
		return false;
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
	{
		return base.OnPickBlock(world, pos, ref handling);
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, ref EnumHandling handled)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-behavior-rightclickpickup",
				MouseButton = EnumMouseButton.Right,
				RequireFreeHand = true
			}
		};
	}
}
