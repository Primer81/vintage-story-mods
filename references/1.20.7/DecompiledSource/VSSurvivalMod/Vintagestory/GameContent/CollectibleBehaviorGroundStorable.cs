using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class CollectibleBehaviorGroundStorable : CollectibleBehavior
{
	public GroundStorageProperties StorageProps { get; protected set; }

	public CollectibleBehaviorGroundStorable(CollectibleObject collObj)
		: base(collObj)
	{
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);
		StorageProps = properties.AsObject<GroundStorageProperties>(null, collObj.Code.Domain);
		if (StorageProps.SprintKey)
		{
			StorageProps.CtrlKey = true;
		}
	}

	public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
	{
		Interact(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
	{
		handling = EnumHandling.PassThrough;
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				HotKeyCodes = ((!StorageProps.CtrlKey) ? new string[1] { "shift" } : new string[2] { "ctrl", "shift" }),
				ActionLangCode = "heldhelp-place",
				MouseButton = EnumMouseButton.Right
			}
		};
	}

	public static void Interact(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
	{
		IWorldAccessor world = byEntity?.World;
		if (blockSel == null || world == null || !byEntity.Controls.ShiftKey)
		{
			return;
		}
		IPlayer byPlayer = null;
		if (byEntity is EntityPlayer)
		{
			byPlayer = world.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		if (byPlayer == null)
		{
			return;
		}
		if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
		{
			itemslot.MarkDirty();
			world.BlockAccessor.MarkBlockDirty(blockSel.Position.UpCopy());
		}
		else
		{
			if (!(world.GetBlock(new AssetLocation("groundstorage")) is BlockGroundStorage blockgs))
			{
				return;
			}
			BlockEntity be = world.BlockAccessor.GetBlockEntity(blockSel.Position);
			BlockEntity beAbove = world.BlockAccessor.GetBlockEntity(blockSel.Position.UpCopy());
			if (be is BlockEntityGroundStorage || beAbove is BlockEntityGroundStorage)
			{
				if (((be as BlockEntityGroundStorage) ?? (beAbove as BlockEntityGroundStorage)).OnPlayerInteractStart(byPlayer, blockSel))
				{
					handHandling = EnumHandHandling.PreventDefault;
				}
			}
			else if (blockSel.Face == BlockFacing.UP && world.BlockAccessor.GetBlock(blockSel.Position).CanAttachBlockAt(world.BlockAccessor, blockgs, blockSel.Position, BlockFacing.UP))
			{
				BlockPos pos = blockSel.Position.AddCopy(blockSel.Face);
				if (world.BlockAccessor.GetBlock(pos).Replaceable >= 6000 && blockgs.CreateStorage(byEntity.World, blockSel, byPlayer))
				{
					handHandling = EnumHandHandling.PreventDefault;
				}
			}
		}
	}
}
