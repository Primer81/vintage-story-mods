using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class BlockEntityBeeHiveKiln : BlockEntity, IRotatable
{
	public BlockFacing Orientation;

	private MultiblockStructure structure;

	private MultiblockStructure highlightedStructure;

	private BlockPos CenterPos;

	private bool receivesHeat;

	private float receivesHeatSmooth;

	public double TotalHoursLastUpdate;

	public double TotalHoursHeatReceived;

	public bool StructureComplete;

	private int tickCounter;

	private bool wasNotProcessing;

	private BlockPos[] particlePositions;

	private BEBehaviorDoor beBehaviorDoor;

	public static int KilnBreakAfterHours = 168;

	public static int ItemBurnTimeHours = 9;

	public static int ItemBurnTemperature = 950;

	public static int ItemMaxTemperature = 1200;

	public static int ItemTemperatureGainPerHour = 500;

	private static AdvancedParticleProperties smokeParticles = new AdvancedParticleProperties
	{
		HsvaColor = new NatFloat[4]
		{
			NatFloat.createUniform(0f, 0f),
			NatFloat.createUniform(0f, 0f),
			NatFloat.createUniform(40f, 30f),
			NatFloat.createUniform(220f, 50f)
		},
		OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
		GravityEffect = NatFloat.createUniform(0f, 0f),
		Velocity = new NatFloat[3]
		{
			NatFloat.createUniform(0f, 0.05f),
			NatFloat.createUniform(0.2f, 0.3f),
			NatFloat.createUniform(0f, 0.05f)
		},
		Size = NatFloat.createUniform(0.3f, 0.05f),
		Quantity = NatFloat.createUniform(0.25f, 0f),
		SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1.5f),
		LifeLength = NatFloat.createUniform(4.5f, 0f),
		ParticleModel = EnumParticleModel.Quad,
		SelfPropelled = true
	};

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		structure = base.Block.Attributes["multiblockStructure"].AsObject<MultiblockStructure>();
		if (Orientation != null)
		{
			Init();
		}
	}

	public override void OnPlacementBySchematic(ICoreServerAPI api, IBlockAccessor blockAccessor, BlockPos pos, Dictionary<int, Dictionary<int, int>> replaceBlocks, int centerrockblockid, Block layerBlock, bool resolveImports)
	{
		base.OnPlacementBySchematic(api, blockAccessor, pos, replaceBlocks, centerrockblockid, layerBlock, resolveImports);
		if (Orientation != null && CenterPos == null)
		{
			Init();
		}
	}

	public void Init()
	{
		if (Api.Side == EnumAppSide.Client)
		{
			RegisterGameTickListener(OnClientTick50ms, 50);
		}
		else
		{
			RegisterGameTickListener(OnServerTick1s, 1000);
		}
		int rotYDeg = 0;
		switch (Orientation.Code)
		{
		case "east":
			rotYDeg = 270;
			break;
		case "west":
			rotYDeg = 90;
			break;
		case "south":
			rotYDeg = 180;
			break;
		}
		structure.InitForUse(rotYDeg);
		CenterPos = Pos.AddCopy(Orientation.Normali * 2);
		particlePositions = new BlockPos[10];
		BlockPos downCenter = CenterPos.Down();
		particlePositions[0] = downCenter;
		particlePositions[1] = downCenter.AddCopy(Orientation.Opposite);
		particlePositions[2] = downCenter.AddCopy(Orientation);
		particlePositions[3] = downCenter.AddCopy(Orientation.GetCW());
		particlePositions[4] = downCenter.AddCopy(Orientation.GetCW()).Add(Orientation.Opposite);
		particlePositions[5] = downCenter.AddCopy(Orientation.GetCW()).Add(Orientation);
		particlePositions[6] = downCenter.AddCopy(Orientation.GetCCW());
		particlePositions[7] = downCenter.AddCopy(Orientation.GetCCW()).Add(Orientation.Opposite);
		particlePositions[8] = downCenter.AddCopy(Orientation.GetCCW()).Add(Orientation);
		particlePositions[9] = downCenter.UpCopy(3);
		beBehaviorDoor = GetBehavior<BEBehaviorDoor>();
	}

	private void OnClientTick50ms(float dt)
	{
		receivesHeatSmooth = GameMath.Clamp(receivesHeatSmooth + (receivesHeat ? (dt / 10f) : ((0f - dt) / 3f)), 0f, 1f);
		if (receivesHeatSmooth == 0f)
		{
			return;
		}
		Random rnd = Api.World.Rand;
		for (int i = 0; i < Entity.FireParticleProps.Length; i++)
		{
			int index = Math.Min(Entity.FireParticleProps.Length - 1, Api.World.Rand.Next(Entity.FireParticleProps.Length + 1));
			for (int j = 0; j < particlePositions.Length; j++)
			{
				AdvancedParticleProperties particles = Entity.FireParticleProps[index];
				BlockPos pos = particlePositions[j];
				if (j == 9)
				{
					particles = smokeParticles;
					particles.Quantity.avg = 0.2f;
					particles.basePos.Set((double)pos.X + 0.5, (double)pos.Y + 0.75, (double)pos.Z + 0.5);
					particles.Velocity[1].avg = (float)(0.3 + 0.3 * rnd.NextDouble()) * 2f;
					particles.PosOffset[1].var = 0.2f;
					particles.Velocity[0].avg = (float)(rnd.NextDouble() - 0.5) / 4f;
					particles.Velocity[2].avg = (float)(rnd.NextDouble() - 0.5) / 4f;
				}
				else
				{
					particles.Quantity.avg = GameMath.Sqrt(0.5f * index switch
					{
						1 => 5f, 
						0 => 0.5f, 
						_ => 0.6f, 
					}) / 4f;
					particles.basePos.Set((double)pos.X + 0.5, (double)pos.Y + 0.5, (double)pos.Z + 0.5);
					particles.Velocity[1].avg = (float)(0.5 + 0.5 * rnd.NextDouble()) * 2f;
					particles.PosOffset[1].var = 1f;
					particles.Velocity[0].avg = (float)(rnd.NextDouble() - 0.5);
					particles.Velocity[2].avg = (float)(rnd.NextDouble() - 0.5);
				}
				particles.PosOffset[0].var = 0.49f;
				particles.PosOffset[2].var = 0.49f;
				Api.World.SpawnParticles(particles);
			}
		}
	}

	private void OnServerTick1s(float dt)
	{
		if (receivesHeat)
		{
			Vec3d pos = CenterPos.ToVec3d().Add(0.5, 0.0, 0.5);
			Entity[] entitiesAround = Api.World.GetEntitiesAround(pos, 1.75f, 3f, (Entity e) => e.Alive && e is EntityAgent);
			for (int i = 0; i < entitiesAround.Length; i++)
			{
				entitiesAround[i].ReceiveDamage(new DamageSource
				{
					DamageTier = 1,
					SourcePos = pos,
					SourceBlock = base.Block,
					Type = EnumDamageType.Fire
				}, 4f);
			}
		}
		if (++tickCounter % 3 == 0)
		{
			OnServerTick3s();
		}
	}

	private void OnServerTick3s()
	{
		bool markDirty = false;
		float minHeatHours = float.MaxValue;
		bool beforeReceiveHeat = receivesHeat;
		bool beforeStructureComplete = StructureComplete;
		if (!receivesHeat)
		{
			TotalHoursLastUpdate = Api.World.Calendar.TotalHours;
		}
		receivesHeat = true;
		for (int i = 0; i < 9; i++)
		{
			BlockPos pos2 = particlePositions[i].DownCopy();
			BlockEntity blockEntity = Api.World.BlockAccessor.GetBlockEntity(pos2);
			float heatHours = 0f;
			if (blockEntity is BlockEntityCoalPile { IsBurning: not false } becp)
			{
				heatHours = becp.GetHoursLeft(TotalHoursLastUpdate);
			}
			else if (blockEntity is BlockEntityGroundStorage { IsBurning: not false } gs)
			{
				heatHours = gs.GetHoursLeft(TotalHoursLastUpdate);
			}
			minHeatHours = Math.Min(minHeatHours, heatHours);
			receivesHeat &= heatHours > 0f;
		}
		StructureComplete = structure.InCompleteBlockCount(Api.World, Pos) == 0;
		if (beforeReceiveHeat != receivesHeat || beforeStructureComplete != StructureComplete)
		{
			markDirty = true;
		}
		if (receivesHeat)
		{
			if (!StructureComplete || beBehaviorDoor.Opened)
			{
				wasNotProcessing = true;
				TotalHoursLastUpdate = Api.World.Calendar.TotalHours;
				MarkDirty();
				return;
			}
			if (wasNotProcessing)
			{
				wasNotProcessing = false;
				TotalHoursLastUpdate = Api.World.Calendar.TotalHours;
			}
			double hoursPassed = Api.World.Calendar.TotalHours - TotalHoursLastUpdate;
			float heatHoursReceived = Math.Max(0f, GameMath.Min((float)hoursPassed, minHeatHours));
			TotalHoursHeatReceived += heatHoursReceived;
			UpdateGroundStorage(heatHoursReceived);
			TotalHoursLastUpdate = Api.World.Calendar.TotalHours;
			markDirty = true;
		}
		if (TotalHoursHeatReceived >= (double)KilnBreakAfterHours)
		{
			TotalHoursHeatReceived = 0.0;
			structure.WalkMatchingBlocks(Api.World, Pos, delegate(Block block, BlockPos pos)
			{
				float num = block.Attributes?["heatResistance"].AsFloat(1f) ?? 1f;
				if (Api.World.Rand.NextDouble() > (double)num)
				{
					Block block2 = Api.World.GetBlock(block.CodeWithVariant("state", "damaged"));
					Api.World.BlockAccessor.SetBlock(block2.Id, pos);
					StructureComplete = false;
					markDirty = true;
				}
			});
		}
		if (markDirty)
		{
			MarkDirty();
		}
	}

	private void UpdateGroundStorage(float hoursHeatReceived)
	{
		int doorOpen = GetOpenDoors();
		for (int j = 0; j < 9; j++)
		{
			for (int i = 1; i < 4; i++)
			{
				BlockPos pos = particlePositions[j].UpCopy(i);
				BlockEntityGroundStorage groundStorage = Api.World.BlockAccessor.GetBlockEntity<BlockEntityGroundStorage>(pos);
				if (groundStorage == null)
				{
					continue;
				}
				for (int index = 0; index < groundStorage.Inventory.Count; index++)
				{
					ItemSlot itemSlot = groundStorage.Inventory[index];
					if (itemSlot.Empty)
					{
						continue;
					}
					float itemHoursHeatReceived = 0f;
					CollectibleObject collectible = itemSlot.Itemstack.Collectible;
					CombustibleProperties combustibleProps = collectible.CombustibleProps;
					if (combustibleProps == null || (combustibleProps.SmeltedStack?.ResolvedItemstack.Block?.BlockMaterial).GetValueOrDefault() != EnumBlockMaterial.Ceramic)
					{
						CombustibleProperties combustibleProps2 = collectible.CombustibleProps;
						if (combustibleProps2 == null || combustibleProps2.SmeltingType != EnumSmeltType.Fire)
						{
							JsonObject attributes = collectible.Attributes;
							if (attributes == null || !attributes["beehivekiln"].Exists)
							{
								goto IL_01fb;
							}
						}
					}
					float temp = itemSlot.Itemstack.Collectible.GetTemperature(Api.World, itemSlot.Itemstack, hoursHeatReceived);
					float tempGain = hoursHeatReceived * (float)ItemTemperatureGainPerHour;
					float hoursHeatingUp = ((float)ItemBurnTemperature - temp) / (float)ItemTemperatureGainPerHour;
					if (hoursHeatingUp < 0f)
					{
						hoursHeatingUp = 0f;
					}
					if (temp < (float)ItemMaxTemperature)
					{
						temp = GameMath.Min(ItemMaxTemperature, temp + tempGain);
						collectible.SetTemperature(Api.World, itemSlot.Itemstack, temp);
					}
					float heatReceived = hoursHeatReceived - hoursHeatingUp;
					if (temp >= (float)ItemBurnTemperature && heatReceived > 0f)
					{
						itemHoursHeatReceived = itemSlot.Itemstack.Attributes.GetFloat("hoursHeatReceived") + heatReceived;
						itemSlot.Itemstack.Attributes.SetFloat("hoursHeatReceived", itemHoursHeatReceived);
					}
					itemSlot.MarkDirty();
					goto IL_01fb;
					IL_01fb:
					if (itemHoursHeatReceived >= (float)ItemBurnTimeHours)
					{
						ConvertItemToBurned(groundStorage, itemSlot, doorOpen);
					}
				}
				groundStorage.MarkDirty();
			}
		}
	}

	private void ConvertItemToBurned(BlockEntityGroundStorage groundStorage, ItemSlot itemSlot, int doorOpen)
	{
		groundStorage.forceStorageProps = true;
		if (itemSlot != null && !itemSlot.Empty)
		{
			ItemStack rawStack = itemSlot.Itemstack;
			ItemStack firedStack = rawStack.Collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack;
			JsonObject obj = rawStack.Collectible.Attributes?["beehivekiln"];
			JsonItemStack stack = obj?[doorOpen.ToString()]?.AsObject<JsonItemStack>();
			float temp = itemSlot.Itemstack.Collectible.GetTemperature(Api.World, itemSlot.Itemstack);
			if (obj != null && obj.Exists && stack != null && stack.Resolve(Api.World, "beehivekiln-burn"))
			{
				itemSlot.Itemstack = stack.ResolvedItemstack.Clone();
				itemSlot.Itemstack.StackSize = rawStack.StackSize / rawStack.Collectible.CombustibleProps.SmeltedRatio;
			}
			else if (firedStack != null)
			{
				itemSlot.Itemstack = firedStack.Clone();
				itemSlot.Itemstack.StackSize = rawStack.StackSize / rawStack.Collectible.CombustibleProps.SmeltedRatio;
			}
			itemSlot.Itemstack.Collectible.SetTemperature(Api.World, itemSlot.Itemstack, temp);
			itemSlot.MarkDirty();
		}
		groundStorage.MarkDirty(redrawOnClient: true);
	}

	private int GetOpenDoors()
	{
		BlockPos backDoorPos = CenterPos.AddCopy(Orientation.Normali * 2).Up();
		BlockPos rightDoorPos = CenterPos.AddCopy(Orientation.GetCW().Normali * 2).Up();
		BlockPos leftDoorPos = CenterPos.AddCopy(Orientation.GetCCW().Normali * 2).Up();
		int doorOpen = 0;
		BlockPos[] array = new BlockPos[3] { backDoorPos, rightDoorPos, leftDoorPos };
		foreach (BlockPos doorPos in array)
		{
			Block door = Api.World.BlockAccessor.GetBlock(doorPos);
			if (door.Variant["state"] != null && door.Variant["state"] == "opened")
			{
				doorOpen++;
			}
		}
		return doorOpen;
	}

	public void Interact(IPlayer byPlayer)
	{
		if (!(Api is ICoreClientAPI capi))
		{
			return;
		}
		bool shiftKey = byPlayer.WorldData.EntityControls.ShiftKey;
		int damagedTiles = 0;
		int wrongTiles = 0;
		int incompleteCount = 0;
		BlockPos posMain = Pos;
		if (!shiftKey)
		{
			incompleteCount = structure.InCompleteBlockCount(Api.World, Pos, delegate(Block haveBlock, AssetLocation wantLoc)
			{
				switch (haveBlock.FirstCodePart())
				{
				case "refractorybricks":
				case "claybricks":
				case "refractorybrickgrating":
					if (haveBlock.Variant["state"] == "damaged")
					{
						damagedTiles++;
						return;
					}
					break;
				}
				wrongTiles++;
			});
			if (incompleteCount > 0)
			{
				highlightedStructure = structure;
			}
		}
		if (!shiftKey && incompleteCount > 0)
		{
			if (wrongTiles > 0 && damagedTiles > 0)
			{
				capi.TriggerIngameError(this, "incomplete", Lang.Get("Structure is not complete, {0} blocks are missing or wrong, {1} tiles are damaged!", wrongTiles, damagedTiles));
			}
			else if (wrongTiles > 0)
			{
				capi.TriggerIngameError(this, "incomplete", Lang.Get("Structure is not complete, {0} blocks are missing or wrong!", wrongTiles));
			}
			else if (damagedTiles == 1)
			{
				capi.TriggerIngameError(this, "incomplete", Lang.Get("Structure is not complete, {0} tile is damaged!", damagedTiles));
			}
			else
			{
				capi.TriggerIngameError(this, "incomplete", Lang.Get("Structure is not complete, {0} tiles are damaged!", damagedTiles));
			}
			highlightedStructure.HighlightIncompleteParts(Api.World, byPlayer, posMain);
		}
		else
		{
			highlightedStructure?.ClearHighlights(Api.World, byPlayer);
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		receivesHeat = tree.GetBool("receivesHeat");
		TotalHoursLastUpdate = tree.GetDouble("totalHoursLastUpdate");
		StructureComplete = tree.GetBool("structureComplete");
		Orientation = BlockFacing.FromFirstLetter(tree.GetString("orientation"));
		TotalHoursHeatReceived = tree.GetDouble("totalHoursHeatReceived");
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBool("receivesHeat", receivesHeat);
		tree.SetDouble("totalHoursLastUpdate", TotalHoursLastUpdate);
		tree.SetBool("structureComplete", StructureComplete);
		tree.SetString("orientation", Orientation.Code);
		tree.SetDouble("totalHoursHeatReceived", TotalHoursHeatReceived);
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		if (Api is ICoreClientAPI capi)
		{
			highlightedStructure?.ClearHighlights(Api.World, capi.World.Player);
		}
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		if (Api is ICoreClientAPI capi)
		{
			highlightedStructure?.ClearHighlights(Api.World, capi.World.Player);
		}
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		if (GetBehavior<BEBehaviorDoor>().Opened)
		{
			dsc.AppendLine(Lang.Get("Door must be closed for firing!"));
		}
		if (!StructureComplete)
		{
			dsc.AppendLine(Lang.Get("Structure incomplete! Can't get hot enough, paused."));
			return;
		}
		if (receivesHeat)
		{
			dsc.AppendLine(Lang.Get("Okay! Receives heat!"));
		}
		else
		{
			dsc.AppendLine(Lang.Get("Ready to be fired. Ignite 3x3 piles of coal below. (progress will proceed once all 9 piles have ignited)"));
		}
		if (TotalHoursHeatReceived > 0.0)
		{
			dsc.AppendLine(Lang.Get("Firing: for {0:0.##} hours", TotalHoursHeatReceived));
		}
	}

	public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
	{
		BlockFacing horizontalRotated = BlockFacing.FromFirstLetter(tree.GetString("orientation")).GetHorizontalRotated(-degreeRotation - 180);
		tree.SetString("orientation", horizontalRotated.Code);
		float rotateYRad = tree.GetFloat("rotateYRad");
		rotateYRad = (rotateYRad - (float)degreeRotation * ((float)Math.PI / 180f)) % ((float)Math.PI * 2f);
		tree.SetFloat("rotateYRad", rotateYRad);
	}
}
