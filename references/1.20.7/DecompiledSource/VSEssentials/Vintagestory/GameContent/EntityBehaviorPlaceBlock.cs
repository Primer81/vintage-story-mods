using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityBehaviorPlaceBlock : EntityBehavior
{
	private ITreeAttribute createBlockTree;

	private JsonObject attributes;

	private long callbackId;

	internal float MinHourDelay => attributes["minHourDelay"].AsFloat(192f);

	internal float MaxHourDelay => attributes["maxHourDelay"].AsFloat(360f);

	internal float RndHourDelay
	{
		get
		{
			float min = MinHourDelay;
			float max = MaxHourDelay;
			return min + (float)entity.World.Rand.NextDouble() * (max - min);
		}
	}

	internal AssetLocation[] BlockCodes
	{
		get
		{
			string[] codes = attributes["blockCodes"].AsArray(new string[0]);
			AssetLocation[] locs = new AssetLocation[codes.Length];
			for (int i = 0; i < locs.Length; i++)
			{
				locs[i] = new AssetLocation(codes[i]);
			}
			return locs;
		}
	}

	internal double TotalHoursUntilPlace
	{
		get
		{
			return createBlockTree.GetDouble("TotalHoursUntilPlace");
		}
		set
		{
			createBlockTree.SetDouble("TotalHoursUntilPlace", value);
		}
	}

	public override string PropertyName()
	{
		return "createblock";
	}

	public EntityBehaviorPlaceBlock(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
		this.attributes = attributes;
		createBlockTree = entity.WatchedAttributes.GetTreeAttribute("behaviorCreateBlock");
		if (createBlockTree == null)
		{
			entity.WatchedAttributes.SetAttribute("behaviorCreateBlock", createBlockTree = new TreeAttribute());
			TotalHoursUntilPlace = entity.World.Calendar.TotalHours + (double)RndHourDelay;
		}
		callbackId = entity.World.RegisterCallback(CheckShouldPlace, 3000);
	}

	private void CheckShouldPlace(float dt)
	{
		if (!entity.Alive || entity.Swimming || entity.FeetInLiquid)
		{
			callbackId = 0L;
			return;
		}
		callbackId = entity.World.RegisterCallback(CheckShouldPlace, 3000);
		if (entity.World.Calendar == null)
		{
			return;
		}
		while (entity.World.Calendar.TotalHours > TotalHoursUntilPlace && entity.World.Rand.NextDouble() < 0.5)
		{
			AssetLocation[] codes = BlockCodes;
			Block block = entity.World.GetBlock(codes[entity.World.Rand.Next(codes.Length)]);
			if (block == null)
			{
				return;
			}
			int num;
			if (!TryPlace(block, 0, 0, 0) && !TryPlace(block, 1, 0, 0) && !TryPlace(block, 0, 0, -1) && !TryPlace(block, -1, 0, 0))
			{
				num = (TryPlace(block, 0, 0, 1) ? 1 : 0);
				if (num == 0)
				{
					goto IL_00f7;
				}
			}
			else
			{
				num = 1;
			}
			TotalHoursUntilPlace += RndHourDelay;
			goto IL_00f7;
			IL_00f7:
			if (num == 0 || MinHourDelay <= 0f)
			{
				break;
			}
		}
		entity.World.FrameProfiler.Mark("createblock");
	}

	private bool TryPlace(Block block, int dx, int dy, int dz)
	{
		IBlockAccessor blockAccess = entity.World.BlockAccessor;
		BlockPos pos = entity.ServerPos.XYZ.AsBlockPos.Add(dx, dy, dz);
		Block block2 = blockAccess.GetBlock(pos);
		pos.Y--;
		if (block2.IsReplacableBy(block) && blockAccess.GetMostSolidBlock(pos).CanAttachBlockAt(blockAccess, block, pos, BlockFacing.UP))
		{
			pos.Y++;
			blockAccess.SetBlock(block.BlockId, pos);
			BlockEntityTransient obj = blockAccess.GetBlockEntity(pos) as BlockEntityTransient;
			obj?.SetPlaceTime(TotalHoursUntilPlace);
			if (obj != null && obj.IsDueTransition())
			{
				blockAccess.SetBlock(0, pos);
			}
			return true;
		}
		return false;
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		entity.World.UnregisterCallback(callbackId);
	}
}
