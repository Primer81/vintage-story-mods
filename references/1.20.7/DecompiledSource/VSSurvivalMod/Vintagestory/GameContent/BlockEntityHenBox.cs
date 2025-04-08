using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockEntityHenBox : BlockEntity, IAnimalNest, IPointOfInterest
{
	internal InventoryGeneric inventory;

	private string fullCode = "1egg";

	public Entity occupier;

	protected int[] parentGenerations = new int[10];

	protected AssetLocation[] chickNames = new AssetLocation[10];

	protected double timeToIncubate;

	protected double occupiedTimeLast;

	public Size2i AtlasSize => (Api as ICoreClientAPI).BlockTextureAtlas.Size;

	public Vec3d Position => Pos.ToVec3d().Add(0.5, 0.5, 0.5);

	public string Type => "nest";

	public float DistanceWeighting => 2 / (CountEggs() + 2);

	public bool IsSuitableFor(Entity entity)
	{
		if (entity is EntityAgent)
		{
			return entity.Code.Path == "chicken-hen";
		}
		return false;
	}

	public bool Occupied(Entity entity)
	{
		if (occupier != null)
		{
			return occupier != entity;
		}
		return false;
	}

	public void SetOccupier(Entity entity)
	{
		occupier = entity;
	}

	public bool TryAddEgg(Entity entity, string chickCode, double incubationTime)
	{
		if (base.Block.LastCodePart() == fullCode)
		{
			if (timeToIncubate == 0.0)
			{
				timeToIncubate = incubationTime;
				occupiedTimeLast = entity.World.Calendar.TotalDays;
			}
			MarkDirty();
			return false;
		}
		timeToIncubate = 0.0;
		int eggs = CountEggs();
		parentGenerations[eggs] = entity.WatchedAttributes.GetInt("generation");
		chickNames[eggs] = ((chickCode == null) ? null : entity.Code.CopyWithPath(chickCode));
		eggs++;
		Block replacementBlock = Api.World.GetBlock(base.Block.CodeWithVariant("eggCount", eggs + ((eggs > 1) ? "eggs" : "egg")));
		if (replacementBlock == null)
		{
			return false;
		}
		Api.World.BlockAccessor.ExchangeBlock(replacementBlock.Id, Pos);
		base.Block = replacementBlock;
		MarkDirty();
		return true;
	}

	private int CountEggs()
	{
		int eggs = base.Block.LastCodePart()[0];
		if (eggs > 57 || eggs < 48)
		{
			return 0;
		}
		return eggs - 48;
	}

	private void On1500msTick(float dt)
	{
		if (timeToIncubate == 0.0)
		{
			return;
		}
		double newTime = Api.World.Calendar.TotalDays;
		if (occupier != null && occupier.Alive && newTime > occupiedTimeLast)
		{
			timeToIncubate -= newTime - occupiedTimeLast;
			MarkDirty();
		}
		occupiedTimeLast = newTime;
		if (!(timeToIncubate <= 0.0))
		{
			return;
		}
		timeToIncubate = 0.0;
		int eggs = CountEggs();
		Random rand = Api.World.Rand;
		for (int c = 0; c < eggs; c++)
		{
			AssetLocation chickName = chickNames[c];
			if (chickName == null)
			{
				continue;
			}
			int generation = parentGenerations[c];
			EntityProperties childType = Api.World.GetEntityType(chickName);
			if (childType != null)
			{
				Entity childEntity = Api.World.ClassRegistry.CreateEntity(childType);
				if (childEntity != null)
				{
					childEntity.ServerPos.SetFrom(new EntityPos(Position.X + (rand.NextDouble() - 0.5) / 5.0, Position.Y, Position.Z + (rand.NextDouble() - 0.5) / 5.0, (float)rand.NextDouble() * ((float)Math.PI * 2f)));
					childEntity.ServerPos.Motion.X += (rand.NextDouble() - 0.5) / 200.0;
					childEntity.ServerPos.Motion.Z += (rand.NextDouble() - 0.5) / 200.0;
					childEntity.Pos.SetFrom(childEntity.ServerPos);
					childEntity.Attributes.SetString("origin", "reproduction");
					childEntity.WatchedAttributes.SetInt("generation", generation + 1);
					Api.World.SpawnEntity(childEntity);
				}
			}
		}
		Block replacementBlock = Api.World.GetBlock(new AssetLocation(base.Block.FirstCodePart() + "-empty"));
		Api.World.BlockAccessor.ExchangeBlock(replacementBlock.Id, Pos);
		Api.World.SpawnCubeParticles(Pos.ToVec3d().Add(0.5, 0.5, 0.5), new ItemStack(base.Block), 1f, 20);
		base.Block = replacementBlock;
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		fullCode = base.Block.Attributes?["fullVariant"]?.AsString();
		if (fullCode == null)
		{
			fullCode = "1egg";
		}
		if (api.Side == EnumAppSide.Server)
		{
			api.ModLoader.GetModSystem<POIRegistry>().AddPOI(this);
			RegisterGameTickListener(On1500msTick, 1500);
		}
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		base.OnBlockPlaced(byItemStack);
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		if (Api.Side == EnumAppSide.Server)
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

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetDouble("inc", timeToIncubate);
		tree.SetDouble("occ", occupiedTimeLast);
		for (int i = 0; i < 10; i++)
		{
			tree.SetInt("gen" + i, parentGenerations[i]);
			AssetLocation chickName = chickNames[i];
			if (chickName != null)
			{
				tree.SetString("chick" + i, chickName.ToShortString());
			}
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		timeToIncubate = tree.GetDouble("inc");
		occupiedTimeLast = tree.GetDouble("occ");
		for (int i = 0; i < 10; i++)
		{
			parentGenerations[i] = tree.GetInt("gen" + i);
			string chickName = tree.GetString("chick" + i);
			chickNames[i] = ((chickName == null) ? null : new AssetLocation(chickName));
		}
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		int eggCount = CountEggs();
		int fertileCount = 0;
		for (int i = 0; i < eggCount; i++)
		{
			if (chickNames[i] != null)
			{
				fertileCount++;
			}
		}
		if (fertileCount > 0)
		{
			if (fertileCount > 1)
			{
				dsc.AppendLine(Lang.Get("{0} fertile eggs", fertileCount));
			}
			else
			{
				dsc.AppendLine(Lang.Get("1 fertile egg"));
			}
			if (timeToIncubate >= 1.5)
			{
				dsc.AppendLine(Lang.Get("Incubation time remaining: {0:0} days", timeToIncubate));
			}
			else if (timeToIncubate >= 0.75)
			{
				dsc.AppendLine(Lang.Get("Incubation time remaining: 1 day"));
			}
			else if (timeToIncubate > 0.0)
			{
				dsc.AppendLine(Lang.Get("Incubation time remaining: {0:0} hours", timeToIncubate * 24.0));
			}
			if (occupier == null && base.Block.LastCodePart() == fullCode)
			{
				dsc.AppendLine(Lang.Get("A broody hen is needed!"));
			}
		}
		else if (eggCount > 0)
		{
			dsc.AppendLine(Lang.Get("No eggs are fertilized"));
		}
	}
}
