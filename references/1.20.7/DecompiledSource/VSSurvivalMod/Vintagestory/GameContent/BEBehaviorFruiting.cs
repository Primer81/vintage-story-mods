using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BEBehaviorFruiting : BlockEntityBehavior
{
	private int positionsCount;

	private int maxFruit = 5;

	private int fruitStages = 6;

	private float maxGerminationDays = 6f;

	private float transitionDays = 1f;

	private float successfulGrowthChance = 0.75f;

	private string[] fruitCodeBases;

	private int ripeStage;

	private AssetLocation dropCode;

	private double[] points;

	private FruitingSystem manager;

	protected Vec3d[] positions;

	protected FruitData[] fruitPoints;

	private double dateLastChecked;

	public static float[] randomRotations;

	public static float[][] randomRotMatrices;

	public Vec4f LightRgba { get; internal set; }

	static BEBehaviorFruiting()
	{
		randomRotations = new float[8] { -22.5f, 22.5f, 67.5f, 112.5f, 157.5f, 202.5f, 247.5f, 292.5f };
		randomRotMatrices = new float[randomRotations.Length][];
		for (int i = 0; i < randomRotations.Length; i++)
		{
			float[] matrix = Mat4f.Create();
			Mat4f.Translate(matrix, matrix, 0.5f, 0.5f, 0.5f);
			Mat4f.RotateY(matrix, matrix, randomRotations[i] * ((float)Math.PI / 180f));
			Mat4f.Translate(matrix, matrix, -0.5f, -0.5f, -0.5f);
			randomRotMatrices[i] = matrix;
		}
	}

	public BEBehaviorFruiting(BlockEntity be)
		: base(be)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		dateLastChecked = Api.World.Calendar.TotalDays;
		fruitCodeBases = properties["fruitCodeBases"].AsArray(new string[0]);
		if (fruitCodeBases.Length == 0)
		{
			return;
		}
		positionsCount = properties["positions"].AsInt();
		if (positionsCount <= 0)
		{
			return;
		}
		string maturePlant = properties["maturePlant"].AsString();
		if (maturePlant == null || !(api.World.GetBlock(new AssetLocation(maturePlant)) is BlockFruiting matureCrop))
		{
			return;
		}
		if (Api.Side == EnumAppSide.Client)
		{
			points = matureCrop.GetFruitingPoints();
		}
		maxFruit = properties["maxFruit"].AsInt(5);
		fruitStages = properties["fruitStages"].AsInt(6);
		maxGerminationDays = properties["maxGerminationDays"].AsFloat(6f);
		transitionDays = properties["transitionDays"].AsFloat(1f);
		successfulGrowthChance = properties["successfulGrowthChance"].AsFloat(0.75f);
		ripeStage = properties["ripeStage"].AsInt(fruitStages - 1);
		dropCode = new AssetLocation(properties["dropCode"].AsString());
		manager = Api.ModLoader.GetModSystem<FruitingSystem>();
		bool addToManager = false;
		if (Api.Side == EnumAppSide.Client && fruitPoints != null)
		{
			LightRgba = Api.World.BlockAccessor.GetLightRGBs(Blockentity.Pos);
			addToManager = true;
		}
		InitializeArrays();
		if (addToManager)
		{
			for (int i = 0; i < positionsCount; i++)
			{
				FruitData val = fruitPoints[i];
				if (val.variant >= fruitCodeBases.Length)
				{
					val.variant %= fruitCodeBases.Length;
				}
				if (val.variant >= 0 && val.currentStage > 0)
				{
					val.SetRandomRotation(Api.World, i, positions[i], Blockentity.Pos);
					manager.AddFruit(new AssetLocation(fruitCodeBases[val.variant] + val.currentStage), positions[i], val);
				}
			}
		}
		if (Api.Side == EnumAppSide.Server)
		{
			Blockentity.RegisterGameTickListener(CheckForGrowth, 2250);
		}
	}

	private void CheckForGrowth(float dt)
	{
		double timeFactor = GameMath.Clamp(Api.World.Calendar.SpeedOfTime / 60f, 0.1, 5.0);
		double now = Api.World.Calendar.TotalDays;
		bool fastForwarded = now > dateLastChecked + 0.5;
		dateLastChecked = now;
		if (Api.World.Rand.NextDouble() > 0.2 * timeFactor && !fastForwarded)
		{
			return;
		}
		int count = 0;
		bool dirty = false;
		FruitData[] array = fruitPoints;
		foreach (FruitData val2 in array)
		{
			if (val2.variant >= 0)
			{
				if (val2.transitionDate == 0.0)
				{
					val2.transitionDate = GetGerminationDate();
					dirty = true;
				}
				if (val2.currentStage > 0)
				{
					count++;
				}
			}
		}
		bool finalStagePlant = false;
		if (Blockentity.Block is BlockCrop crop)
		{
			finalStagePlant = crop.CurrentCropStage == crop.CropProps.GrowthStages;
		}
		array = fruitPoints;
		foreach (FruitData val in array)
		{
			if (val.variant >= 0 && now > val.transitionDate && (val.currentStage != 0 || count < maxFruit) && (!finalStagePlant || val.currentStage >= fruitStages - 3))
			{
				if (++val.currentStage > fruitStages)
				{
					val.transitionDate = double.MaxValue;
					val.currentStage = fruitStages;
				}
				else
				{
					val.transitionDate = now + (double)transitionDays * (1.0 + Api.World.Rand.NextDouble()) / 1.5 / PlantHealth() * ((val.currentStage == fruitStages - 1) ? 2.5 : 1.0);
				}
				dirty = true;
			}
		}
		if (dirty)
		{
			Blockentity.MarkDirty();
		}
	}

	public void InitializeArrays()
	{
		if (fruitPoints == null)
		{
			fruitPoints = new FruitData[positionsCount];
			int randomSelector2 = Math.Abs(Blockentity.Pos.GetHashCode()) % fruitCodeBases.Length;
			for (int j = 0; j < positionsCount; j++)
			{
				int fruitVariant = j;
				if (j >= fruitCodeBases.Length)
				{
					fruitVariant = randomSelector2++ % fruitCodeBases.Length;
				}
				fruitPoints[j] = new FruitData(fruitVariant, GetGerminationDate(), this, null);
			}
		}
		positions = new Vec3d[positionsCount];
		Vec3f temp = new Vec3f();
		float[] matrix = null;
		if (Blockentity.Block.RandomizeRotations)
		{
			int randomSelector = GameMath.MurmurHash3(-Blockentity.Pos.X, (Blockentity.Block.RandomizeAxes == EnumRandomizeAxes.XYZ) ? Blockentity.Pos.Y : 0, Blockentity.Pos.Z);
			matrix = randomRotMatrices[GameMath.Mod(randomSelector, randomRotations.Length)];
		}
		for (int i = 0; i < positionsCount; i++)
		{
			if (Api.Side == EnumAppSide.Client)
			{
				positions[i] = new Vec3d(points[i * 3], points[i * 3 + 1], points[i * 3 + 2]);
				if (matrix != null)
				{
					Mat4f.MulWithVec3_Position(matrix, (float)positions[i].X, (float)positions[i].Y, (float)positions[i].Z, temp);
					positions[i].X = temp.X;
					positions[i].Y = temp.Y;
					positions[i].Z = temp.Z;
				}
			}
			else
			{
				positions[i] = new Vec3d((i + 1) / positionsCount, (i + 1) / positionsCount, (i + 1) / positionsCount);
			}
			positions[i].Add(Blockentity.Pos);
		}
	}

	private double GetGerminationDate()
	{
		double fruitSpawnReduction = (1.0 / PlantHealth() + 0.25) / 1.25;
		double randomNo = Api.World.Rand.NextDouble() / (double)successfulGrowthChance * fruitSpawnReduction;
		if (!(randomNo > 1.0))
		{
			return Api.World.Calendar.TotalDays + randomNo * (double)maxGerminationDays;
		}
		return double.MaxValue;
	}

	private double PlantHealth()
	{
		BlockPos posBelow = Blockentity.Pos.DownCopy();
		if (Blockentity.Api.World.BlockAccessor.GetBlockEntity(posBelow) is BlockEntityFarmland bef)
		{
			return bef.GetGrowthRate();
		}
		return 1.0;
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		base.OnTesselation(mesher, tesselator);
		LightRgba = Api.World.BlockAccessor.GetLightRGBs(Blockentity.Pos);
		return false;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		if (positionsCount == 0)
		{
			positionsCount = tree.GetInt("count");
		}
		if (positionsCount == 0)
		{
			positionsCount = 10;
		}
		if (fruitPoints == null)
		{
			fruitPoints = new FruitData[positionsCount];
		}
		for (int i = 0; i < positionsCount; i++)
		{
			double td = tree.GetDouble("td" + i);
			int var = tree.GetInt("var" + i);
			int tc = tree.GetInt("tc" + i);
			FruitData val = fruitPoints[i];
			if (val == null)
			{
				val = new FruitData(-1, td, this, null);
				fruitPoints[i] = val;
			}
			if (Api is ICoreClientAPI && val.variant >= 0)
			{
				manager.RemoveFruit(fruitCodeBases[val.variant] + val.currentStage, positions[i]);
			}
			val.variant = var;
			val.currentStage = tc;
			val.transitionDate = td;
			if (Api is ICoreClientAPI && val.variant >= 0 && val.currentStage > 0)
			{
				val.SetRandomRotation(Api.World, i, positions[i], Blockentity.Pos);
				manager.AddFruit(new AssetLocation(fruitCodeBases[val.variant] + val.currentStage), positions[i], val);
			}
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetInt("count", positionsCount);
		for (int i = 0; i < positionsCount; i++)
		{
			FruitData val = fruitPoints[i];
			if (val != null)
			{
				tree.SetDouble("td" + i, val.transitionDate);
				tree.SetInt("var" + i, val.variant);
				tree.SetInt("tc" + i, val.currentStage);
			}
		}
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		if (Api is ICoreClientAPI)
		{
			RemoveRenderedFruits();
		}
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		if (Api is ICoreClientAPI)
		{
			RemoveRenderedFruits();
		}
		int dropCount = 0;
		for (int i = 0; i < fruitPoints.Length; i++)
		{
			FruitData val = fruitPoints[i];
			if (val.variant < 0 || val.currentStage == 0)
			{
				continue;
			}
			Item item = Api.World.GetItem(new AssetLocation(fruitCodeBases[val.variant] + val.currentStage));
			if (item != null && (item.Attributes == null || !item.Attributes["onGround"].AsBool()))
			{
				if (val.currentStage == ripeStage)
				{
					dropCount++;
				}
				else if (Math.Abs(val.currentStage - ripeStage) == 1 && Api.World.Rand.NextDouble() > 0.5)
				{
					dropCount++;
				}
			}
		}
		if (dropCount > 0)
		{
			ItemStack stack = new ItemStack(Api.World.GetItem(dropCode), dropCount);
			Api.World.SpawnItemEntity(stack, Blockentity.Pos.ToVec3d().Add(0.5, 0.25, 0.5));
		}
	}

	public virtual void RemoveRenderedFruits()
	{
		if (positions == null || fruitCodeBases == null)
		{
			return;
		}
		for (int i = 0; i < fruitPoints.Length; i++)
		{
			FruitData val = fruitPoints[i];
			if (val.variant >= 0 && val.currentStage > 0)
			{
				manager.RemoveFruit(fruitCodeBases[val.variant] + val.currentStage, positions[i]);
			}
		}
	}

	public virtual bool OnPlayerInteract(float secondsUsed, IPlayer player, Vec3d hit)
	{
		if (player == null || player.InventoryManager?.ActiveTool != EnumTool.Knife)
		{
			return false;
		}
		if (Api.Side == EnumAppSide.Server)
		{
			return true;
		}
		bool hasPickableFruit = false;
		for (int i = 0; i < fruitPoints.Length; i++)
		{
			FruitData val = fruitPoints[i];
			if (val.variant >= 0 && val.currentStage >= ripeStage)
			{
				Item item = Api.World.GetItem(new AssetLocation(fruitCodeBases[val.variant] + val.currentStage));
				if (item != null && (item.Attributes == null || !item.Attributes["onGround"].AsBool()))
				{
					hasPickableFruit = true;
					break;
				}
			}
		}
		if (hasPickableFruit)
		{
			return secondsUsed < 0.3f;
		}
		return false;
	}

	public virtual void OnPlayerInteractStop(float secondsUsed, IPlayer player, Vec3d hit)
	{
		if (secondsUsed < 0.2f)
		{
			return;
		}
		for (int i = 0; i < fruitPoints.Length; i++)
		{
			FruitData val = fruitPoints[i];
			if (val.variant < 0 || val.currentStage < ripeStage)
			{
				continue;
			}
			Item item = Api.World.GetItem(new AssetLocation(fruitCodeBases[val.variant] + val.currentStage));
			if (item == null || (item.Attributes != null && item.Attributes["onGround"].AsBool()))
			{
				continue;
			}
			if (Api.Side == EnumAppSide.Client)
			{
				manager.RemoveFruit(fruitCodeBases[val.variant] + val.currentStage, positions[i]);
			}
			val.variant = -1;
			val.transitionDate = double.MaxValue;
			if (val.currentStage >= ripeStage)
			{
				double posx = (double)Blockentity.Pos.X + hit.X;
				double posy = (double)Blockentity.Pos.Y + hit.Y;
				double posz = (double)Blockentity.Pos.Z + hit.Z;
				player.Entity.World.PlaySoundAt(new AssetLocation("sounds/effect/squish1"), posx, posy, posz, player, 1.1f + (float)Api.World.Rand.NextDouble() * 0.4f, 16f, 0.25f);
				double now = Api.World.Calendar.TotalDays;
				for (int j = 0; j < fruitPoints.Length; j++)
				{
					val = fruitPoints[j];
					if (val.variant >= 0 && val.currentStage == 0 && val.transitionDate < now)
					{
						val.transitionDate = now + Api.World.Rand.NextDouble() * (double)maxGerminationDays / 2.0;
					}
				}
				ItemStack stack = new ItemStack(Api.World.GetItem(dropCode));
				if (!player.InventoryManager.TryGiveItemstack(stack))
				{
					Api.World.SpawnItemEntity(stack, Blockentity.Pos.ToVec3d().Add(0.5, 0.25, 0.5));
				}
				Api.World.Logger.Audit("{0} Took 1x{1} from {2} at {3}.", player.PlayerName, stack.Collectible.Code, Blockentity.Block.Code, Blockentity.Pos);
			}
			Blockentity.MarkDirty();
			break;
		}
	}
}
