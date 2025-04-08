using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

internal class BlockChandelier : Block
{
	public int CandleCount => LastCodePart() switch
	{
		"candle0" => 0, 
		"candle1" => 1, 
		"candle2" => 2, 
		"candle3" => 3, 
		"candle4" => 4, 
		"candle5" => 5, 
		"candle6" => 6, 
		"candle7" => 7, 
		"candle8" => 8, 
		_ => -1, 
	};

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		_ = CandleCount;
		ItemStack itemstack = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack;
		if (itemstack != null && itemstack.Collectible.Code.Path == "candle" && CandleCount != 8)
		{
			if (byPlayer != null && byPlayer.WorldData.CurrentGameMode == EnumGameMode.Survival)
			{
				byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(1);
			}
			Block block = world.GetBlock(CodeWithParts(GetNextCandleCount()));
			world.BlockAccessor.ExchangeBlock(block.BlockId, blockSel.Position);
			world.BlockAccessor.MarkBlockDirty(blockSel.Position);
			return true;
		}
		return false;
	}

	private string GetNextCandleCount()
	{
		if (CandleCount != 8)
		{
			return $"candle{CandleCount + 1}";
		}
		return "";
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		if (CandleCount == 8)
		{
			return null;
		}
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-chandelier-addcandle",
				MouseButton = EnumMouseButton.Right,
				Itemstacks = new ItemStack[1]
				{
					new ItemStack(world.GetItem(new AssetLocation("candle")))
				}
			}
		}.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}
}
