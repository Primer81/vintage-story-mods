using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public abstract class BlockBaseDoor : Block
{
	protected string type;

	protected bool open;

	public abstract string GetKnobOrientation();

	public abstract BlockFacing GetDirection();

	protected abstract BlockPos TryGetConnectedDoorPos(BlockPos pos);

	protected abstract void Open(IWorldAccessor world, IPlayer byPlayer, BlockPos position);

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		type = string.Intern(Code.Path.Substring(0, Code.Path.IndexOf('-')));
		open = Variant["state"] == "opened";
	}

	public bool IsSameDoor(Block block)
	{
		if (block is BlockBaseDoor otherDoor)
		{
			return otherDoor.type == type;
		}
		return false;
	}

	public virtual bool IsOpened()
	{
		return open;
	}

	public bool DoesBehaviorAllow(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		bool preventDefault = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handled2 = EnumHandling.PassThrough;
			obj.OnBlockInteractStart(world, byPlayer, blockSel, ref handled2);
			if (handled2 != 0)
			{
				preventDefault = true;
			}
			if (handled2 == EnumHandling.PreventSubsequent)
			{
				return false;
			}
		}
		if (preventDefault)
		{
			return false;
		}
		if (this is BlockDoor)
		{
			blockSel = blockSel.Clone();
			blockSel.Position = ((this as BlockDoor).IsUpperHalf() ? blockSel.Position.DownCopy() : blockSel.Position.UpCopy());
			blockBehaviors = BlockBehaviors;
			foreach (BlockBehavior obj2 in blockBehaviors)
			{
				EnumHandling handled = EnumHandling.PassThrough;
				obj2.OnBlockInteractStart(world, byPlayer, blockSel, ref handled);
				if (handled != 0)
				{
					preventDefault = true;
				}
				if (handled == EnumHandling.PreventSubsequent)
				{
					return false;
				}
			}
			if (preventDefault)
			{
				return false;
			}
		}
		return true;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (!DoesBehaviorAllow(world, byPlayer, blockSel))
		{
			return true;
		}
		BlockPos pos = blockSel.Position;
		Open(world, byPlayer, pos);
		world.PlaySoundAt(AssetLocation.Create(Attributes["triggerSound"].AsString("sounds/block/door"), Code.Domain), pos, 0.0, byPlayer);
		if (!(FirstCodePart() == "roughhewnfencegate"))
		{
			TryOpenConnectedDoor(world, byPlayer, pos);
		}
		(byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
		return true;
	}

	protected void TryOpenConnectedDoor(IWorldAccessor world, IPlayer byPlayer, BlockPos pos)
	{
		BlockPos door2Pos = TryGetConnectedDoorPos(pos);
		if (door2Pos != null && world.BlockAccessor.GetBlock(door2Pos) is BlockBaseDoor door2 && IsSameDoor(door2) && pos == door2.TryGetConnectedDoorPos(door2Pos))
		{
			door2.Open(world, byPlayer, door2Pos);
		}
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-door-openclose",
				MouseButton = EnumMouseButton.Right
			}
		}.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}
}
