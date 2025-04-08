using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityBeehive : BlockEntity, IAnimalFoodSource, IPointOfInterest
{
	private int scanIteration;

	private int quantityNearbyFlowers;

	private int quantityNearbyHives;

	private List<BlockPos> emptySkeps = new List<BlockPos>();

	private bool isWildHive;

	private BlockPos skepToPop;

	private double beginPopStartTotalHours;

	private float popHiveAfterHours;

	private double cooldownUntilTotalHours;

	private double harvestableAtTotalHours;

	public bool Harvestable;

	private int scanQuantityNearbyFlowers;

	private int scanQuantityNearbyHives;

	private List<BlockPos> scanEmptySkeps = new List<BlockPos>();

	private EnumHivePopSize hivePopSize;

	private bool wasPlaced;

	public static SimpleParticleProperties Bees;

	private string orientation;

	private Vec3d startPos = new Vec3d();

	private Vec3d endPos = new Vec3d();

	private Vec3f minVelo = new Vec3f();

	private Vec3f maxVelo = new Vec3f();

	private float activityLevel;

	private RoomRegistry roomreg;

	private float roomness;

	public Vec3d Position => Pos.ToVec3d().Add(0.5, 0.5, 0.5);

	public string Type => "food";

	static BlockEntityBeehive()
	{
		Bees = new SimpleParticleProperties(1f, 1f, ColorUtil.ToRgba(255, 215, 156, 65), new Vec3d(), new Vec3d(), new Vec3f(0f, 0f, 0f), new Vec3f(0f, 0f, 0f), 1f, 0f, 0.5f, 0.5f);
		Bees.RandomVelocityChange = true;
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		RegisterGameTickListener(TestHarvestable, 3000);
		RegisterGameTickListener(OnScanForEmptySkep, api.World.Rand.Next(5000) + 30000);
		roomreg = Api.ModLoader.GetModSystem<RoomRegistry>();
		if (api.Side == EnumAppSide.Client)
		{
			RegisterGameTickListener(SpawnBeeParticles, 300);
		}
		if (wasPlaced)
		{
			harvestableAtTotalHours = api.World.Calendar.TotalHours + 12.0 * (3.0 + api.World.Rand.NextDouble() * 8.0);
		}
		orientation = base.Block.LastCodePart();
		isWildHive = base.Block.FirstCodePart() != "skep";
		if (!isWildHive && api.Side == EnumAppSide.Client)
		{
			ICoreClientAPI obj = api as ICoreClientAPI;
			Block fullSkep = api.World.GetBlock(new AssetLocation("skep-populated-east"));
			obj.Tesselator.TesselateShape(fullSkep, Shape.TryGet(api, "shapes/block/beehive/skep-harvestable.json"), out var mesh, new Vec3f(0f, BlockFacing.FromCode(orientation).HorizontalAngleIndex * 90 - 90, 0f));
			api.ObjectCache["beehive-harvestablemesh-" + orientation] = mesh;
		}
		if (!isWildHive && api.Side == EnumAppSide.Server)
		{
			api.ModLoader.GetModSystem<POIRegistry>().AddPOI(this);
		}
	}

	private void SpawnBeeParticles(float dt)
	{
		float dayLightStrength = Api.World.Calendar.GetDayLightStrength(Pos.X, Pos.Z);
		if (!(Api.World.Rand.NextDouble() > (double)(2f * dayLightStrength) - 0.5))
		{
			Random rand = Api.World.Rand;
			Bees.MinQuantity = activityLevel;
			if (Api.World.Rand.NextDouble() > 0.5)
			{
				startPos.Set((float)Pos.X + 0.5f, (float)Pos.Y + 0.5f, (float)Pos.Z + 0.5f);
				minVelo.Set((float)rand.NextDouble() * 3f - 1.5f, (float)rand.NextDouble() * 1f - 0.5f, (float)rand.NextDouble() * 3f - 1.5f);
				Bees.MinPos = startPos;
				Bees.MinVelocity = minVelo;
				Bees.LifeLength = 1f;
				Bees.WithTerrainCollision = true;
			}
			else
			{
				startPos.Set((double)Pos.X + rand.NextDouble() * 5.0 - 2.5, (double)Pos.Y + rand.NextDouble() * 2.0 - 1.0, (double)Pos.Z + rand.NextDouble() * 5.0 - 2.5);
				endPos.Set((float)Pos.X + 0.5f, (float)Pos.Y + 0.5f, (float)Pos.Z + 0.5f);
				minVelo.Set((float)(endPos.X - startPos.X), (float)(endPos.Y - startPos.Y), (float)(endPos.Z - startPos.Z));
				minVelo /= 2f;
				Bees.MinPos = startPos;
				Bees.MinVelocity = minVelo;
				Bees.WithTerrainCollision = true;
			}
			Api.World.SpawnParticles(Bees);
		}
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		wasPlaced = true;
		if (Api?.World != null)
		{
			harvestableAtTotalHours = Api.World.Calendar.TotalHours + 12.0 * (3.0 + Api.World.Rand.NextDouble() * 8.0);
		}
	}

	private void TestHarvestable(float dt)
	{
		float temp = Api.World.BlockAccessor.GetClimateAt(Pos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, Api.World.Calendar.TotalDays).Temperature;
		if (roomness > 0f)
		{
			temp += 5f;
		}
		activityLevel = GameMath.Clamp(temp / 5f, 0f, 1f);
		if (temp <= -10f)
		{
			harvestableAtTotalHours = Api.World.Calendar.TotalHours + 12.0 * (3.0 + Api.World.Rand.NextDouble() * 8.0);
			cooldownUntilTotalHours = Api.World.Calendar.TotalHours + 48.0;
		}
		if (!Harvestable && !isWildHive && Api.World.Calendar.TotalHours > harvestableAtTotalHours && hivePopSize > EnumHivePopSize.Poor)
		{
			Harvestable = true;
			MarkDirty(redrawOnClient: true);
		}
	}

	private void OnScanForEmptySkep(float dt)
	{
		Room room = roomreg?.GetRoomForPosition(Pos);
		roomness = ((room != null && room.SkylightCount > room.NonSkylightCount && room.ExitCount == 0) ? 1 : 0);
		if (activityLevel == 0f || Api.Side == EnumAppSide.Client || Api.World.Calendar.TotalHours < cooldownUntilTotalHours)
		{
			return;
		}
		if (scanIteration == 0)
		{
			scanQuantityNearbyFlowers = 0;
			scanQuantityNearbyHives = 0;
			scanEmptySkeps.Clear();
		}
		int minX = -8 + 8 * (scanIteration / 2);
		int minZ = -8 + 8 * (scanIteration % 2);
		int size = 8;
		Api.World.BlockAccessor.WalkBlocks(Pos.AddCopy(minX, -5, minZ), Pos.AddCopy(minX + size - 1, 5, minZ + size - 1), delegate(Block block, int x, int y, int z)
		{
			if (block.Id != 0)
			{
				if (block.BlockMaterial == EnumBlockMaterial.Plant || block is BlockPlantContainer)
				{
					JsonObject attributes = block.Attributes;
					if (attributes != null && attributes.IsTrue("beeFeed"))
					{
						scanQuantityNearbyFlowers++;
					}
				}
				else if (block.BlockMaterial == EnumBlockMaterial.Other)
				{
					string path = block.Code.Path;
					if (path.StartsWithOrdinal("skep-empty"))
					{
						scanEmptySkeps.Add(new BlockPos(x, y, z));
					}
					else if (path.StartsWithOrdinal("skep-populated") || path.StartsWithOrdinal("wildbeehive"))
					{
						scanQuantityNearbyHives++;
					}
				}
			}
		});
		scanIteration++;
		if (scanIteration == 4)
		{
			scanIteration = 0;
			OnScanComplete();
		}
	}

	private void OnScanComplete()
	{
		quantityNearbyFlowers = scanQuantityNearbyFlowers;
		quantityNearbyHives = scanQuantityNearbyHives;
		emptySkeps = new List<BlockPos>(scanEmptySkeps);
		if (emptySkeps.Count == 0)
		{
			skepToPop = null;
		}
		hivePopSize = (EnumHivePopSize)GameMath.Clamp(quantityNearbyFlowers - 3 * quantityNearbyHives, 0, 2);
		MarkDirty();
		if (3 * quantityNearbyHives + 3 > quantityNearbyFlowers)
		{
			skepToPop = null;
			MarkDirty();
			return;
		}
		if (skepToPop != null && Api.World.Calendar.TotalHours > beginPopStartTotalHours + (double)popHiveAfterHours)
		{
			TryPopCurrentSkep();
			cooldownUntilTotalHours = Api.World.Calendar.TotalHours + 48.0;
			MarkDirty();
			return;
		}
		float swarmability = (float)GameMath.Clamp(quantityNearbyFlowers - 3 - 3 * quantityNearbyHives, 0, 20) / 5f;
		float swarmInDays = (4f - swarmability) * 2.5f;
		if (swarmability <= 0f)
		{
			skepToPop = null;
		}
		if (skepToPop != null)
		{
			float newPopHours = 24f * swarmInDays;
			popHiveAfterHours = (float)(0.75 * (double)popHiveAfterHours + 0.25 * (double)newPopHours);
			if (!emptySkeps.Contains(skepToPop))
			{
				skepToPop = null;
				MarkDirty();
			}
			return;
		}
		popHiveAfterHours = 24f * swarmInDays;
		beginPopStartTotalHours = Api.World.Calendar.TotalHours;
		float mindistance = 999f;
		BlockPos closestPos = new BlockPos();
		foreach (BlockPos pos in emptySkeps)
		{
			float dist = pos.DistanceTo(Pos);
			if (dist < mindistance)
			{
				mindistance = dist;
				closestPos = pos;
			}
		}
		skepToPop = closestPos;
	}

	private void TryPopCurrentSkep()
	{
		Block skepToPopBlock = Api.World.BlockAccessor.GetBlock(skepToPop);
		if (skepToPopBlock == null || !(skepToPopBlock is BlockSkep))
		{
			skepToPop = null;
			return;
		}
		string orient = skepToPopBlock.LastCodePart();
		string blockcode = "skep-populated-" + orient;
		Block fullSkep = Api.World.GetBlock(new AssetLocation(blockcode));
		if (fullSkep == null)
		{
			Api.World.Logger.Warning("BEBeehive.TryPopSkep() - block with code {0} does not exist?", blockcode);
		}
		else
		{
			Api.World.BlockAccessor.SetBlock(fullSkep.BlockId, skepToPop);
			hivePopSize = EnumHivePopSize.Poor;
			skepToPop = null;
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetInt("scanIteration", scanIteration);
		tree.SetInt("quantityNearbyFlowers", quantityNearbyFlowers);
		tree.SetInt("quantityNearbyHives", quantityNearbyHives);
		TreeAttribute emptyskepTree = new TreeAttribute();
		for (int j = 0; j < emptySkeps.Count; j++)
		{
			emptyskepTree.SetInt("posX-" + j, emptySkeps[j].X);
			emptyskepTree.SetInt("posY-" + j, emptySkeps[j].Y);
			emptyskepTree.SetInt("posZ-" + j, emptySkeps[j].Z);
		}
		tree["emptyskeps"] = emptyskepTree;
		tree.SetInt("scanQuantityNearbyFlowers", scanQuantityNearbyFlowers);
		tree.SetInt("scanQuantityNearbyHives", scanQuantityNearbyHives);
		TreeAttribute scanEmptyskepTree = new TreeAttribute();
		for (int i = 0; i < scanEmptySkeps.Count; i++)
		{
			scanEmptyskepTree.SetInt("posX-" + i, scanEmptySkeps[i].X);
			scanEmptyskepTree.SetInt("posY-" + i, scanEmptySkeps[i].Y);
			scanEmptyskepTree.SetInt("posZ-" + i, scanEmptySkeps[i].Z);
		}
		tree["scanEmptySkeps"] = scanEmptyskepTree;
		tree.SetInt("isWildHive", isWildHive ? 1 : 0);
		tree.SetInt("harvestable", Harvestable ? 1 : 0);
		tree.SetInt("skepToPopX", (!(skepToPop == null)) ? skepToPop.X : 0);
		tree.SetInt("skepToPopY", (!(skepToPop == null)) ? skepToPop.Y : 0);
		tree.SetInt("skepToPopZ", (!(skepToPop == null)) ? skepToPop.Z : 0);
		tree.SetDouble("beginPopStartTotalHours", beginPopStartTotalHours);
		tree.SetFloat("popHiveAfterHours", popHiveAfterHours);
		tree.SetDouble("cooldownUntilTotalHours", cooldownUntilTotalHours);
		tree.SetDouble("harvestableAtTotalHours", harvestableAtTotalHours);
		tree.SetInt("hiveHealth", (int)hivePopSize);
		tree.SetFloat("roomness", roomness);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		bool wasHarvestable = Harvestable;
		scanIteration = tree.GetInt("scanIteration");
		quantityNearbyFlowers = tree.GetInt("quantityNearbyFlowers");
		quantityNearbyHives = tree.GetInt("quantityNearbyHives");
		emptySkeps.Clear();
		TreeAttribute emptySkepTree = tree["emptyskeps"] as TreeAttribute;
		for (int j = 0; j < emptySkepTree.Count / 3; j++)
		{
			emptySkeps.Add(new BlockPos(emptySkepTree.GetInt("posX-" + j), emptySkepTree.GetInt("posY-" + j), emptySkepTree.GetInt("posZ-" + j)));
		}
		scanQuantityNearbyFlowers = tree.GetInt("scanQuantityNearbyFlowers");
		scanQuantityNearbyHives = tree.GetInt("scanQuantityNearbyHives");
		scanEmptySkeps.Clear();
		TreeAttribute scanEmptySkepTree = tree["scanEmptySkeps"] as TreeAttribute;
		int i = 0;
		while (scanEmptySkepTree != null && i < scanEmptySkepTree.Count / 3)
		{
			scanEmptySkeps.Add(new BlockPos(scanEmptySkepTree.GetInt("posX-" + i), scanEmptySkepTree.GetInt("posY-" + i), scanEmptySkepTree.GetInt("posZ-" + i)));
			i++;
		}
		isWildHive = tree.GetInt("isWildHive") > 0;
		Harvestable = tree.GetInt("harvestable") > 0;
		int x = tree.GetInt("skepToPopX");
		int y = tree.GetInt("skepToPopY");
		int z = tree.GetInt("skepToPopZ");
		if (x != 0 || y != 0 || z != 0)
		{
			skepToPop = new BlockPos(x, y, z);
		}
		else
		{
			skepToPop = null;
		}
		beginPopStartTotalHours = tree.GetDouble("beginPopStartTotalHours");
		popHiveAfterHours = tree.GetFloat("popHiveAfterHours");
		cooldownUntilTotalHours = tree.GetDouble("cooldownUntilTotalHours");
		harvestableAtTotalHours = tree.GetDouble("harvestableAtTotalHours");
		hivePopSize = (EnumHivePopSize)tree.GetInt("hiveHealth");
		roomness = tree.GetFloat("roomness");
		if (Harvestable != wasHarvestable && Api != null)
		{
			MarkDirty(redrawOnClient: true);
		}
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		if (Harvestable)
		{
			mesher.AddMeshData(Api.ObjectCache["beehive-harvestablemesh-" + orientation] as MeshData);
			return true;
		}
		return false;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		string popSizeLocalized = Lang.Get("population-" + hivePopSize);
		if (Api.World.EntityDebugMode && forPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative)
		{
			dsc.AppendLine(Lang.Get("Nearby flowers: {0}, Nearby Hives: {1}, Empty Hives: {2}, Pop after hours: {3}. harvest in {4}, repop cooldown: {5}", quantityNearbyFlowers, quantityNearbyHives, emptySkeps.Count, (beginPopStartTotalHours + (double)popHiveAfterHours - Api.World.Calendar.TotalHours).ToString("#.##"), (harvestableAtTotalHours - Api.World.Calendar.TotalHours).ToString("#.##"), (cooldownUntilTotalHours - Api.World.Calendar.TotalHours).ToString("#.##")) + "\n" + Lang.Get("Population Size:") + popSizeLocalized);
		}
		string str = Lang.Get("beehive-flowers-pop", quantityNearbyFlowers, popSizeLocalized);
		if (Harvestable)
		{
			str = str + "\n" + Lang.Get("Harvestable");
		}
		if (skepToPop != null && Api.World.Calendar.TotalHours > cooldownUntilTotalHours)
		{
			double days = (beginPopStartTotalHours + (double)popHiveAfterHours - Api.World.Calendar.TotalHours) / (double)Api.World.Calendar.HoursPerDay;
			str = ((days > 1.5) ? (str + "\n" + Lang.Get("Will swarm in approx. {0} days", Math.Round(days))) : ((!(days > 0.5)) ? (str + "\n" + Lang.Get("Will swarm in less than a day")) : (str + "\n" + Lang.Get("Will swarm in approx. one day"))));
		}
		if (roomness > 0f)
		{
			str = str + "\n" + Lang.Get("greenhousetempbonus");
		}
		dsc.AppendLine(str);
	}

	public bool IsSuitableFor(Entity entity, CreatureDiet diet)
	{
		if (isWildHive || !Harvestable)
		{
			return false;
		}
		return (diet?.FoodTags?.Contains("lootableSweet")).GetValueOrDefault();
	}

	public float ConsumeOnePortion(Entity entity)
	{
		Api.World.BlockAccessor.BreakBlock(Pos, null);
		return 1f;
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		if (!isWildHive && Api.Side == EnumAppSide.Server)
		{
			Api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this);
		}
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Server)
		{
			Api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this);
		}
	}
}
