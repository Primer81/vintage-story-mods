using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityMycelium : BlockEntity
{
	private Vec3i[] grownMushroomOffsets = new Vec3i[0];

	private double mushroomsGrownTotalDays;

	private double mushroomsDiedTotalDays = -999999.0;

	private double mushroomsGrowingDays;

	private double lastUpdateTotalDays;

	private AssetLocation mushroomBlockCode;

	private MushroomProps props;

	private Block mushroomBlock;

	private double fruitingDays = 20.0;

	private double growingDays = 20.0;

	private int growRange = 7;

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		if (api.Side != EnumAppSide.Server)
		{
			return;
		}
		int interval = 10000;
		RegisterGameTickListener(onServerTick, interval, -api.World.Rand.Next(interval));
		if (mushroomBlockCode != null && !setMushroomBlock(Api.World.GetBlock(mushroomBlockCode)))
		{
			api.Logger.Error("Invalid mycelium mushroom type '{0}' at {1}. Will delete block entity.", mushroomBlockCode, Pos);
			Api.Event.EnqueueMainThreadTask(delegate
			{
				Api.World.BlockAccessor.RemoveBlockEntity(Pos);
			}, "deletemyceliumBE");
		}
	}

	private void onServerTick(float dt)
	{
		bool isFruiting = grownMushroomOffsets.Length != 0;
		if (isFruiting && props.DieWhenTempBelow > -99f && Api.World.BlockAccessor.GetClimateAt(Pos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, Api.World.Calendar.TotalDays).Temperature < props.DieWhenTempBelow)
		{
			DestroyGrownMushrooms();
		}
		else if (props.DieAfterFruiting && isFruiting && mushroomsGrownTotalDays + fruitingDays < Api.World.Calendar.TotalDays)
		{
			DestroyGrownMushrooms();
		}
		else if (!isFruiting)
		{
			lastUpdateTotalDays = Math.Max(lastUpdateTotalDays, Api.World.Calendar.TotalDays - 50.0);
			while (Api.World.Calendar.TotalDays - lastUpdateTotalDays > 1.0)
			{
				if (Api.World.BlockAccessor.GetClimateAt(Pos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, lastUpdateTotalDays + 0.5).Temperature > 5f)
				{
					mushroomsGrowingDays += Api.World.Calendar.TotalDays - lastUpdateTotalDays;
				}
				lastUpdateTotalDays += 1.0;
			}
			if (mushroomsGrowingDays > growingDays)
			{
				growMushrooms(Api.World.BlockAccessor, MyceliumSystem.rndn);
				mushroomsGrowingDays = 0.0;
			}
		}
		else
		{
			if (!(Api.World.Calendar.TotalDays - lastUpdateTotalDays > 0.1))
			{
				return;
			}
			lastUpdateTotalDays = Api.World.Calendar.TotalDays;
			for (int i = 0; i < grownMushroomOffsets.Length; i++)
			{
				Vec3i offset = grownMushroomOffsets[i];
				BlockPos pos = Pos.AddCopy(offset);
				if (Api.World.BlockAccessor.GetChunkAtBlockPos(pos) == null)
				{
					break;
				}
				if (!Api.World.BlockAccessor.GetBlock(pos).Code.Equals(mushroomBlockCode))
				{
					grownMushroomOffsets = grownMushroomOffsets.RemoveEntry(i);
					i--;
				}
			}
		}
	}

	public void Regrow()
	{
		DestroyGrownMushrooms();
		growMushrooms(Api.World.BlockAccessor, MyceliumSystem.rndn);
	}

	private void DestroyGrownMushrooms()
	{
		mushroomsDiedTotalDays = Api.World.Calendar.TotalDays;
		Vec3i[] array = grownMushroomOffsets;
		foreach (Vec3i offset in array)
		{
			BlockPos pos = Pos.AddCopy(offset);
			if (Api.World.BlockAccessor.GetBlock(pos).Variant["mushroom"] == mushroomBlock.Variant["mushroom"])
			{
				Api.World.BlockAccessor.SetBlock(0, pos);
			}
		}
		grownMushroomOffsets = new Vec3i[0];
	}

	private bool setMushroomBlock(Block block)
	{
		mushroomBlock = block;
		mushroomBlockCode = block?.Code;
		if (block != null)
		{
			ICoreAPI api = Api;
			if (api != null && api.Side == EnumAppSide.Server)
			{
				JsonObject attributes = block.Attributes;
				if (attributes == null || !attributes["mushroomProps"].Exists)
				{
					return false;
				}
				props = block.Attributes["mushroomProps"].AsObject<MushroomProps>();
				MyceliumSystem.lcgrnd.InitPositionSeed(mushroomBlockCode.GetHashCode(), (int)(Api.World.Calendar.GetHemisphere(Pos) + 5));
				fruitingDays = 20.0 + MyceliumSystem.lcgrnd.NextDouble() * 20.0;
				growingDays = 10.0 + MyceliumSystem.lcgrnd.NextDouble() * 10.0;
				return true;
			}
		}
		return false;
	}

	public void OnGenerated(IBlockAccessor blockAccessor, IRandom rnd, BlockMushroom block)
	{
		setMushroomBlock(block);
		MyceliumSystem.lcgrnd.InitPositionSeed(mushroomBlockCode.GetHashCode(), (int)(mushroomBlock as BlockMushroom).Api.World.Calendar.GetHemisphere(Pos));
		if (MyceliumSystem.lcgrnd.NextDouble() < 0.33)
		{
			mushroomsGrowingDays = MyceliumSystem.lcgrnd.NextDouble() * 10.0;
		}
		else
		{
			growMushrooms(blockAccessor, rnd);
		}
	}

	private void growMushrooms(IBlockAccessor blockAccessor, IRandom rnd)
	{
		if (mushroomBlock.Variant.ContainsKey("side"))
		{
			generateSideGrowingMushrooms(blockAccessor, rnd);
		}
		else
		{
			generateUpGrowingMushrooms(blockAccessor, rnd);
		}
		mushroomsGrownTotalDays = (mushroomBlock as BlockMushroom).Api.World.Calendar.TotalDays - rnd.NextDouble() * fruitingDays;
	}

	private void generateUpGrowingMushrooms(IBlockAccessor blockAccessor, IRandom rnd)
	{
		if (mushroomBlock == null)
		{
			return;
		}
		int cnt = 2 + rnd.NextInt(11);
		BlockPos pos = new BlockPos();
		List<Vec3i> offsets = new List<Vec3i>();
		if (!isChunkAreaLoaded(blockAccessor, growRange))
		{
			return;
		}
		while (cnt-- > 0)
		{
			int dx = growRange - rnd.NextInt(2 * growRange + 1);
			int dz = growRange - rnd.NextInt(2 * growRange + 1);
			pos.Set(Pos.X + dx, 0, Pos.Z + dz);
			IMapChunk mapChunk = blockAccessor.GetMapChunkAtBlockPos(pos);
			if (mapChunk != null)
			{
				int lx = GameMath.Mod(pos.X, 32);
				int lz = GameMath.Mod(pos.Z, 32);
				pos.Y = mapChunk.WorldGenTerrainHeightMap[lz * 32 + lx] + 1;
				Block hereBlock = blockAccessor.GetBlock(pos);
				if (blockAccessor.GetBlockBelow(pos).Fertility >= 10 && hereBlock.LiquidCode == null && ((mushroomsGrownTotalDays == 0.0 && hereBlock.Replaceable >= 6000) || hereBlock.Id == 0))
				{
					blockAccessor.SetBlock(mushroomBlock.Id, pos);
					offsets.Add(new Vec3i(dx, pos.Y - Pos.Y, dz));
				}
			}
		}
		grownMushroomOffsets = offsets.ToArray();
	}

	private bool isChunkAreaLoaded(IBlockAccessor blockAccessor, int growRange)
	{
		int num = (Pos.X - growRange) / 32;
		int maxcx = (Pos.X + growRange) / 32;
		int mincz = (Pos.Z - growRange) / 32;
		int maxcz = (Pos.Z + growRange) / 32;
		for (int cx = num; cx <= maxcx; cx++)
		{
			for (int cz = mincz; cz <= maxcz; cz++)
			{
				if (blockAccessor.GetChunk(cx, Pos.InternalY / 32, cz) == null)
				{
					return false;
				}
			}
		}
		return true;
	}

	private void generateSideGrowingMushrooms(IBlockAccessor blockAccessor, IRandom rnd)
	{
		int cnt = 1 + rnd.NextInt(5);
		BlockPos mpos = new BlockPos();
		List<Vec3i> offsets = new List<Vec3i>();
		while (cnt-- > 0)
		{
			int dx = 0;
			int dy = rnd.NextInt(5) - 2;
			int dz = 0;
			mpos.Set(Pos.X + dx, Pos.Y + dy, Pos.Z + dz);
			Block block = blockAccessor.GetBlock(mpos);
			if (!(block is BlockLog) || block.Variant["type"] == "resin")
			{
				continue;
			}
			BlockFacing facing = null;
			int rndside = rnd.NextInt(4);
			for (int i = 0; i < 4; i++)
			{
				BlockFacing f = BlockFacing.HORIZONTALS[(i + rndside) % 4];
				mpos.Set(Pos.X + dx, Pos.Y + dy, Pos.Z + dz).Add(f);
				if (blockAccessor.GetBlock(mpos).Id == 0)
				{
					facing = f.Opposite;
					break;
				}
			}
			if (facing != null)
			{
				Block mblock = blockAccessor.GetBlock(mushroomBlock.CodeWithVariant("side", facing.Code));
				blockAccessor.SetBlock(mblock.Id, mpos);
				offsets.Add(new Vec3i(mpos.X - Pos.X, mpos.Y - Pos.Y, mpos.Z - Pos.Z));
			}
		}
		grownMushroomOffsets = offsets.ToArray();
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		mushroomBlockCode = new AssetLocation(tree.GetString("mushroomBlockCode"));
		grownMushroomOffsets = tree.GetVec3is("grownMushroomOffsets");
		mushroomsGrownTotalDays = tree.GetDouble("mushromsGrownTotalDays");
		mushroomsDiedTotalDays = tree.GetDouble("mushroomsDiedTotalDays");
		lastUpdateTotalDays = tree.GetDouble("lastUpdateTotalDays");
		mushroomsGrowingDays = tree.GetDouble("mushroomsGrowingDays");
		setMushroomBlock(worldAccessForResolve.GetBlock(mushroomBlockCode));
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetString("mushroomBlockCode", mushroomBlockCode.ToShortString());
		tree.SetVec3is("grownMushroomOffsets", grownMushroomOffsets);
		tree.SetDouble("mushromsGrownTotalDays", mushroomsGrownTotalDays);
		tree.SetDouble("mushroomsDiedTotalDays", mushroomsDiedTotalDays);
		tree.SetDouble("lastUpdateTotalDays", lastUpdateTotalDays);
		tree.SetDouble("mushroomsGrowingDays", mushroomsGrowingDays);
	}
}
