using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class PumpkinCropBehavior : CropBehavior
{
	private int vineGrowthStage = 3;

	private float vineGrowthQuantity;

	private AssetLocation vineBlockLocation;

	private NatFloat vineGrowthQuantityGen;

	public PumpkinCropBehavior(Block block)
		: base(block)
	{
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);
		vineGrowthStage = properties["vineGrowthStage"].AsInt();
		vineGrowthQuantityGen = properties["vineGrowthQuantity"].AsObject<NatFloat>();
		vineBlockLocation = new AssetLocation("pumpkin-vine-1-normal");
	}

	public override void OnPlanted(ICoreAPI api)
	{
		vineGrowthQuantity = vineGrowthQuantityGen.nextFloat(1f, api.World.Rand);
	}

	public override bool TryGrowCrop(ICoreAPI api, IFarmlandBlockEntity farmland, double currentTotalHours, int newGrowthStage, ref EnumHandling handling)
	{
		if (vineGrowthQuantity == 0f)
		{
			vineGrowthQuantity = farmland.CropAttributes.GetFloat("vineGrowthQuantity", vineGrowthQuantityGen.nextFloat(1f, api.World.Rand));
			farmland.CropAttributes.SetFloat("vineGrowthQuantity", vineGrowthQuantity);
		}
		handling = EnumHandling.PassThrough;
		if (newGrowthStage >= vineGrowthStage)
		{
			if (newGrowthStage == 8)
			{
				bool allWithered = true;
				BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
				foreach (BlockFacing facing in hORIZONTALS)
				{
					Block block = api.World.BlockAccessor.GetBlock(farmland.Pos.AddCopy(facing).Up());
					if (block.Code.PathStartsWith("pumpkin-vine"))
					{
						allWithered &= block.LastCodePart() == "withered";
					}
				}
				if (!allWithered)
				{
					handling = EnumHandling.PreventDefault;
				}
				return false;
			}
			if (api.World.Rand.NextDouble() < (double)vineGrowthQuantity)
			{
				return TrySpawnVine(api, farmland, currentTotalHours);
			}
		}
		return false;
	}

	private bool TrySpawnVine(ICoreAPI api, IFarmlandBlockEntity farmland, double currentTotalHours)
	{
		BlockPos motherplantPos = farmland.UpPos;
		BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
		foreach (BlockFacing facing in hORIZONTALS)
		{
			BlockPos candidatePos = motherplantPos.AddCopy(facing);
			Block block = api.World.BlockAccessor.GetBlock(candidatePos);
			if (CanReplace(block) && CanSupportPumpkin(api, candidatePos.DownCopy()))
			{
				DoSpawnVine(api, candidatePos, motherplantPos, facing, currentTotalHours);
				return true;
			}
		}
		return false;
	}

	private void DoSpawnVine(ICoreAPI api, BlockPos vinePos, BlockPos motherplantPos, BlockFacing facing, double currentTotalHours)
	{
		Block vineBlock = api.World.GetBlock(vineBlockLocation);
		api.World.BlockAccessor.SetBlock(vineBlock.BlockId, vinePos);
		if (api.World is IServerWorldAccessor)
		{
			BlockEntity be = api.World.BlockAccessor.GetBlockEntity(vinePos);
			if (be is BlockEntityPumpkinVine)
			{
				((BlockEntityPumpkinVine)be).CreatedFromParent(motherplantPos, facing, currentTotalHours);
			}
		}
	}

	private bool CanReplace(Block block)
	{
		if (block == null)
		{
			return true;
		}
		if (block.Replaceable >= 6000)
		{
			return !block.Code.GetName().Contains("pumpkin");
		}
		return false;
	}

	public static bool CanSupportPumpkin(ICoreAPI api, BlockPos pos)
	{
		if (api.World.BlockAccessor.GetBlock(pos, 2).IsLiquid())
		{
			return false;
		}
		return api.World.BlockAccessor.GetBlock(pos).Replaceable <= 5000;
	}
}
