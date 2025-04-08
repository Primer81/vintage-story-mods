using Vintagestory.API.Common;

namespace Vintagestory.GameContent.Mechanics;

public class BlockHelveHammer : Block
{
	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEHelveHammer { HammerStack: null } beh && !byPlayer.InventoryManager.ActiveHotbarSlot.Empty && byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Collectible.FirstCodePart().Equals("helvehammer"))
		{
			beh.HammerStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Clone();
			beh.MarkDirty();
			if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
			{
				byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(1);
			}
			byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
			api.World.PlaySoundAt(new AssetLocation("sounds/player/build"), (double)blockSel.Position.X + 0.5, (double)blockSel.Position.Y + 0.5, (double)blockSel.Position.Z + 0.5, null, 0.88f + (float)api.World.Rand.NextDouble() * 0.24f, 16f);
			return true;
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}
}
