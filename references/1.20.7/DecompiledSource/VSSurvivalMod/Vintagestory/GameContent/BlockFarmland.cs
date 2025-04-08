using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockFarmland : Block
{
	public CodeAndChance[] WeedNames;

	public int DelayGrowthBelowSunLight = 19;

	public float LossPerLevel = 0.1f;

	public float TotalWeedChance;

	public WeatherSystemBase wsys;

	public RoomRegistry roomreg;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		if (Attributes != null)
		{
			DelayGrowthBelowSunLight = Attributes["delayGrowthBelowSunLight"].AsInt(19);
			LossPerLevel = Attributes["lossPerLevel"].AsFloat(0.1f);
			if (WeedNames == null)
			{
				WeedNames = Attributes["weedBlockCodes"].AsObject<CodeAndChance[]>();
				int i = 0;
				while (WeedNames != null && i < WeedNames.Length)
				{
					TotalWeedChance += WeedNames[i].Chance;
					i++;
				}
			}
		}
		wsys = api.ModLoader.GetModSystem<WeatherSystemBase>();
		roomreg = api.ModLoader.GetModSystem<RoomRegistry>();
	}

	public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
	{
		base.OnBlockPlaced(world, blockPos, byItemStack);
		if (byItemStack != null)
		{
			(world.BlockAccessor.GetBlockEntity(blockPos) as BlockEntityFarmland)?.OnCreatedFromSoil(byItemStack.Block);
		}
	}

	public override bool CanAttachBlockAt(IBlockAccessor world, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
	{
		if ((block is BlockCrop || block is BlockDeadCrop) && blockFace == BlockFacing.UP)
		{
			return true;
		}
		if (blockFace.IsHorizontal)
		{
			return false;
		}
		return base.CanAttachBlockAt(world, block, pos, blockFace, attachmentArea);
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityFarmland befarmland && befarmland.OnBlockInteract(byPlayer))
		{
			return true;
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityFarmland befarmland)
		{
			return new ItemStack(api.World.GetBlock(CodeWithVariant("state", befarmland.IsVisiblyMoist ? "moist" : "dry"))).GetName();
		}
		return base.GetPlacedBlockName(world, pos);
	}

	public override int GetRetention(BlockPos pos, BlockFacing facing, EnumRetentionType type)
	{
		return 3;
	}

	public override bool SideIsSolid(BlockPos pos, int faceIndex)
	{
		return faceIndex == 5;
	}
}
