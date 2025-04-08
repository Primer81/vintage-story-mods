using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockEntityLayer : BlockEntity
{
	protected static readonly int WEIGHTLIMIT = 75;

	protected static readonly Vec3d center = new Vec3d(0.5, 0.125, 0.5);

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		RegisterGameTickListener(OnEvery250Ms, 250);
	}

	private void OnEvery250Ms(float dt)
	{
		IWorldAccessor world = Api.World;
		Vec3d pos3d = center.AddCopy(Pos);
		BlockPos down = Pos.DownCopy();
		if (CheckSupport(world.BlockAccessor, down))
		{
			return;
		}
		Entity[] entities = world.GetEntitiesAround(pos3d, 1f, 1.5f, (Entity e) => e?.Properties.Weight > (float)WEIGHTLIMIT);
		foreach (Entity entity in entities)
		{
			Cuboidd eBox = new Cuboidd();
			EntityPos pos = entity.Pos;
			eBox.Set(entity.SelectionBox).Translate(pos.X, pos.Y, pos.Z);
			Cuboidf bBox = new Cuboidf();
			bBox.Set(base.Block.CollisionBoxes[0]);
			bBox.Translate(Pos.X, Pos.Y, Pos.Z);
			if (eBox.MinY <= (double)bBox.MaxY + 0.01 && eBox.MinY >= (double)bBox.MinY - 0.01)
			{
				bool checkSouth = eBox.MaxZ > (double)bBox.Z2;
				bool checkNorth = eBox.MinZ < (double)bBox.Z1;
				bool num = eBox.MinX < (double)bBox.X1;
				bool num2 = eBox.MinZ > (double)bBox.X2;
				bool supported = false;
				IBlockAccessor access = world.BlockAccessor;
				if (num2)
				{
					supported |= CheckSupport(access, down.EastCopy());
				}
				if (num2 && checkNorth)
				{
					supported |= CheckSupport(access, down.EastCopy().North());
				}
				if (num2 && checkSouth)
				{
					supported |= CheckSupport(access, down.EastCopy().South());
				}
				if (num)
				{
					supported |= CheckSupport(access, down.WestCopy());
				}
				if (num && checkNorth)
				{
					supported |= CheckSupport(access, down.WestCopy().North());
				}
				if (num && checkSouth)
				{
					supported |= CheckSupport(access, down.WestCopy().South());
				}
				if (checkNorth)
				{
					supported |= CheckSupport(access, down.NorthCopy());
				}
				if (checkSouth)
				{
					supported |= CheckSupport(access, down.SouthCopy());
				}
			}
		}
	}

	protected bool CheckSupport(IBlockAccessor access, BlockPos pos)
	{
		return access.GetBlock(pos).Replaceable < 6000;
	}
}
