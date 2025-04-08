using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntitySapling : BlockEntity
{
	private double totalHoursTillGrowth;

	private long growListenerId;

	public EnumTreeGrowthStage stage;

	public bool plantedFromSeed;

	private NormalRandom normalRandom;

	private MeshData dirtMoundMesh
	{
		get
		{
			ICoreClientAPI capi = Api as ICoreClientAPI;
			if (capi == null)
			{
				return null;
			}
			return ObjectCacheUtil.GetOrCreate(Api, "dirtMoundMesh", delegate
			{
				MeshData modeldata = null;
				Shape shape = Shape.TryGet(capi, AssetLocation.Create("shapes/block/plant/dirtmound.json", base.Block.Code.Domain));
				capi.Tesselator.TesselateShape(base.Block, shape, out modeldata);
				return modeldata;
			});
		}
	}

	private NatFloat nextStageDaysRnd
	{
		get
		{
			if (stage == EnumTreeGrowthStage.Seed)
			{
				NatFloat sproutDays = NatFloat.create(EnumDistribution.UNIFORM, 1.5f, 0.5f);
				if (base.Block?.Attributes != null)
				{
					return base.Block.Attributes["growthDays"].AsObject(sproutDays);
				}
				return sproutDays;
			}
			NatFloat matureDays = NatFloat.create(EnumDistribution.UNIFORM, 7f, 2f);
			if (base.Block?.Attributes != null)
			{
				return base.Block.Attributes["matureDays"].AsObject(matureDays);
			}
			return matureDays;
		}
	}

	private float GrowthRateMod => Api.World.Config.GetString("saplingGrowthRate").ToFloat(1f);

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		if (api is ICoreServerAPI)
		{
			normalRandom = new NormalRandom(api.World.Seed);
			growListenerId = RegisterGameTickListener(CheckGrow, 2000);
		}
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		stage = ((!(byItemStack?.Collectible is ItemTreeSeed)) ? EnumTreeGrowthStage.Sapling : EnumTreeGrowthStage.Seed);
		plantedFromSeed = stage == EnumTreeGrowthStage.Seed;
		totalHoursTillGrowth = Api.World.Calendar.TotalHours + (double)(nextStageDaysRnd.nextFloat(1f, Api.World.Rand) * 24f * GrowthRateMod);
	}

	private void CheckGrow(float dt)
	{
		if (Api.World.Calendar.TotalHours < totalHoursTillGrowth || Api.World.BlockAccessor.GetClimateAt(Pos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, Api.World.Calendar.TotalDays).Temperature < 5f)
		{
			return;
		}
		if (stage == EnumTreeGrowthStage.Seed)
		{
			stage = EnumTreeGrowthStage.Sapling;
			totalHoursTillGrowth = Api.World.Calendar.TotalHours + (double)(nextStageDaysRnd.nextFloat(1f, Api.World.Rand) * 24f * GrowthRateMod);
			MarkDirty(redrawOnClient: true);
			return;
		}
		BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
		for (int i = 0; i < hORIZONTALS.Length; i++)
		{
			Vec3i dir = hORIZONTALS[i].Normali;
			int x = Pos.X + dir.X * 32;
			int z = Pos.Z + dir.Z * 32;
			if (Api.World.BlockAccessor.IsValidPos(x, Pos.InternalY, z) && Api.World.BlockAccessor.GetChunkAtBlockPos(x, Pos.InternalY, z) == null)
			{
				return;
			}
		}
		string treeGenCode = Api.World.BlockAccessor.GetBlock(Pos).Attributes?["treeGen"].AsString();
		if (treeGenCode == null)
		{
			UnregisterGameTickListener(growListenerId);
			growListenerId = 0L;
			return;
		}
		AssetLocation code = new AssetLocation(treeGenCode);
		if (!(Api as ICoreServerAPI).World.TreeGenerators.TryGetValue(code, out var gen))
		{
			UnregisterGameTickListener(growListenerId);
			growListenerId = 0L;
			return;
		}
		Api.World.BlockAccessor.SetBlock(0, Pos);
		Api.World.BulkBlockAccessor.ReadFromStagedByDefault = true;
		float size = 0.6f + (float)Api.World.Rand.NextDouble() * 0.5f;
		TreeGenParams pa = new TreeGenParams
		{
			skipForestFloor = true,
			size = size,
			otherBlockChance = 0f,
			vinesGrowthChance = 0f,
			mossGrowthChance = 0f
		};
		gen.GrowTree(Api.World.BulkBlockAccessor, Pos.DownCopy(), pa, normalRandom);
		Api.World.BulkBlockAccessor.Commit();
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetDouble("totalHoursTillGrowth", totalHoursTillGrowth);
		tree.SetInt("growthStage", (int)stage);
		tree.SetBool("plantedFromSeed", plantedFromSeed);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		totalHoursTillGrowth = tree.GetDouble("totalHoursTillGrowth");
		stage = (EnumTreeGrowthStage)tree.GetInt("growthStage", 1);
		plantedFromSeed = tree.GetBool("plantedFromSeed");
	}

	public ItemStack[] GetDrops()
	{
		if (stage == EnumTreeGrowthStage.Seed)
		{
			Item item = Api.World.GetItem(AssetLocation.Create("treeseed-" + base.Block.Variant["wood"], base.Block.Code.Domain));
			return new ItemStack[1]
			{
				new ItemStack(item)
			};
		}
		return new ItemStack[1]
		{
			new ItemStack(base.Block)
		};
	}

	public string GetBlockName()
	{
		if (stage == EnumTreeGrowthStage.Seed)
		{
			return Lang.Get("treeseed-planted-" + base.Block.Variant["wood"]);
		}
		return base.Block.OnPickBlock(Api.World, Pos).GetName();
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		base.GetBlockInfo(forPlayer, dsc);
		double daysleft = (totalHoursTillGrowth - Api.World.Calendar.TotalHours) / (double)Api.World.Calendar.HoursPerDay;
		if (stage == EnumTreeGrowthStage.Seed)
		{
			if (daysleft <= 1.0)
			{
				dsc.AppendLine(Lang.Get("Will sprout in less than a day"));
				return;
			}
			dsc.AppendLine(Lang.Get("Will sprout in about {0} days", (int)daysleft));
		}
		else if (daysleft <= 1.0)
		{
			dsc.AppendLine(Lang.Get("Will mature in less than a day"));
		}
		else
		{
			dsc.AppendLine(Lang.Get("Will mature in about {0} days", (int)daysleft));
		}
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (plantedFromSeed)
		{
			mesher.AddMeshData(dirtMoundMesh);
		}
		if (stage == EnumTreeGrowthStage.Seed)
		{
			return true;
		}
		return base.OnTesselation(mesher, tessThreadTesselator);
	}
}
