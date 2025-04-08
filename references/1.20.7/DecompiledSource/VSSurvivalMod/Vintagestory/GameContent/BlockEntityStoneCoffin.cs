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

public class BlockEntityStoneCoffin : BlockEntityContainer
{
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

	private MultiblockStructure ms;

	private MultiblockStructure msOpp;

	private MultiblockStructure msHighlighted;

	private BlockStoneCoffinSection blockScs;

	private InventoryStoneCoffin inv;

	private ICoreClientAPI capi;

	private bool receivesHeat;

	private float receivesHeatSmooth;

	private double progress;

	private double totalHoursLastUpdate;

	private bool processComplete;

	public bool StructureComplete;

	private int tickCounter;

	private int tempStoneCoffin;

	private BlockPos tmpPos = new BlockPos();

	private BlockPos[] particlePositions = new BlockPos[7];

	private string[] selectiveElementsMain = new string[0];

	private string[] selectiveElementsSecondary = new string[0];

	public override InventoryBase Inventory => inv;

	public override string InventoryClassName => "stonecoffin";

	public bool IsFull
	{
		get
		{
			if (IngotCount == 16)
			{
				return CoalLayerCount == 5;
			}
			return false;
		}
	}

	public int IngotCount => inv[1].StackSize;

	public int CoalLayerCount => inv[0].StackSize / 8;

	public int CoffinTemperature => tempStoneCoffin;

	public BlockEntityStoneCoffin()
	{
		inv = new InventoryStoneCoffin(2, null, null);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		inv.LateInitialize(InventoryClassName + "-" + Pos, api);
		capi = api as ICoreClientAPI;
		if (api.Side == EnumAppSide.Client)
		{
			RegisterGameTickListener(onClientTick50ms, 50);
		}
		else
		{
			RegisterGameTickListener(onServerTick1s, 1000);
		}
		ms = base.Block.Attributes["multiblockStructure"].AsObject<MultiblockStructure>();
		msOpp = base.Block.Attributes["multiblockStructure"].AsObject<MultiblockStructure>();
		int rotYDeg = 0;
		int rotYDegOpp = 180;
		if (base.Block.Variant["side"] == "east")
		{
			rotYDeg = 270;
			rotYDegOpp = 90;
		}
		ms.InitForUse(rotYDeg);
		msOpp.InitForUse(rotYDegOpp);
		blockScs = base.Block as BlockStoneCoffinSection;
		updateSelectiveElements();
		particlePositions[0] = Pos.DownCopy(2);
		particlePositions[1] = particlePositions[0].AddCopy(blockScs.Orientation.Opposite);
		particlePositions[2] = Pos.AddCopy(blockScs.Orientation.GetCW());
		particlePositions[3] = Pos.AddCopy(blockScs.Orientation.GetCCW());
		particlePositions[4] = Pos.AddCopy(blockScs.Orientation.GetCW()).Add(blockScs.Orientation.Opposite);
		particlePositions[5] = Pos.AddCopy(blockScs.Orientation.GetCCW()).Add(blockScs.Orientation.Opposite);
		particlePositions[6] = Pos.UpCopy().Add(blockScs.Orientation.Opposite);
		inv.SetSecondaryPos(Pos.AddCopy(blockScs.Orientation.Opposite));
	}

	public bool Interact(IPlayer byPlayer, bool preferThis)
	{
		bool sneaking = byPlayer.WorldData.EntityControls.ShiftKey;
		int damagedTiles = 0;
		int wrongTiles = 0;
		int incompleteCount = 0;
		BlockPos posMain = Pos;
		if (sneaking)
		{
			int ic = 0;
			int icOpp = int.MaxValue;
			int dt = 0;
			int wt = 0;
			int dtOpp = 0;
			int wtOpp = 0;
			ic = ms.InCompleteBlockCount(Api.World, Pos, delegate(Block haveBlock, AssetLocation wantLoc)
			{
				string text2 = haveBlock.FirstCodePart();
				if ((text2 == "refractorybricks" || text2 == "refractorybrickgrating") && haveBlock.Variant["state"] == "damaged")
				{
					dt++;
				}
				else
				{
					wt++;
				}
			});
			if (ic > 0 && blockScs.IsCompleteCoffin(Pos))
			{
				icOpp = msOpp.InCompleteBlockCount(Api.World, Pos.AddCopy(blockScs.Orientation.Opposite), delegate(Block haveBlock, AssetLocation wantLoc)
				{
					string text = haveBlock.FirstCodePart();
					if ((text == "refractorybricks" || text == "refractorybrickgrating") && haveBlock.Variant["state"] == "damaged")
					{
						dt++;
					}
					else
					{
						wtOpp++;
					}
				});
			}
			if ((wtOpp <= 3 && wt < wtOpp) || (wtOpp > 3 && wt < wtOpp - 3) || (preferThis && wt <= wtOpp) || (preferThis && wt > 3 && wt <= wtOpp + 3))
			{
				incompleteCount = ic;
				damagedTiles = dt;
				wrongTiles = wt;
				if (ic > 0)
				{
					msHighlighted = ms;
				}
			}
			else
			{
				incompleteCount = icOpp;
				damagedTiles = dtOpp;
				wrongTiles = wtOpp;
				msHighlighted = msOpp;
				posMain = Pos.AddCopy(blockScs.Orientation.Opposite);
			}
		}
		if (sneaking && incompleteCount > 0)
		{
			if (wrongTiles > 0 && damagedTiles > 0)
			{
				capi?.TriggerIngameError(this, "incomplete", Lang.Get("Structure is not complete, {0} blocks are missing or wrong, {1} tiles are damaged!", wrongTiles, damagedTiles));
			}
			else if (wrongTiles > 0)
			{
				capi?.TriggerIngameError(this, "incomplete", Lang.Get("Structure is not complete, {0} blocks are missing or wrong!", wrongTiles));
			}
			else if (damagedTiles == 1)
			{
				capi?.TriggerIngameError(this, "incomplete", Lang.Get("Structure is not complete, {0} tile is damaged!", damagedTiles));
			}
			else
			{
				capi?.TriggerIngameError(this, "incomplete", Lang.Get("Structure is not complete, {0} tiles are damaged!", damagedTiles));
			}
			if (Api.Side == EnumAppSide.Client)
			{
				msHighlighted.HighlightIncompleteParts(Api.World, byPlayer, posMain);
			}
			return false;
		}
		if (Api.Side == EnumAppSide.Client)
		{
			msHighlighted?.ClearHighlights(Api.World, byPlayer);
		}
		if (!sneaking)
		{
			return false;
		}
		if (!blockScs.IsCompleteCoffin(Pos))
		{
			capi?.TriggerIngameError(this, "incomplete", Lang.Get("Cannot fill an incomplete coffin, place the other half first"));
			return false;
		}
		ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (!slot.Empty)
		{
			if (IngotCount / 4 >= CoalLayerCount)
			{
				return AddCoal(slot);
			}
			return AddIngot(slot);
		}
		return true;
	}

	private bool AddCoal(ItemSlot slot)
	{
		if (CoalLayerCount >= 5)
		{
			capi?.TriggerIngameError(this, "notenoughfuel", Lang.Get("This stone coffin is full already"));
			return false;
		}
		CombustibleProperties props = slot.Itemstack.Collectible.CombustibleProps;
		if (props == null || props.BurnTemperature < 1300)
		{
			capi?.TriggerIngameError(this, "wrongfuel", Lang.Get("Needs a layer of high-quality carbon-bearing material (coke or charcoal)"));
			return false;
		}
		if (slot.Itemstack.StackSize < 8)
		{
			capi?.TriggerIngameError(this, "notenoughfuel", Lang.Get("Each layer requires 8 pieces of fuel"));
			return false;
		}
		if (slot.TryPutInto(Api.World, inv[0], 8) == 0)
		{
			capi?.TriggerIngameError(this, "cannotmixfuels", Lang.Get("Cannot mix materials, it will mess with the carburisation process!"));
			return false;
		}
		updateSelectiveElements();
		MarkDirty(redrawOnClient: true);
		return true;
	}

	private bool AddIngot(ItemSlot slot)
	{
		if (IngotCount >= 16)
		{
			capi?.TriggerIngameError(this, "notenoughfuel", Lang.Get("This stone coffin is full already"));
			return false;
		}
		JsonObject itemAttributes = slot.Itemstack.ItemAttributes;
		if (itemAttributes != null && !itemAttributes["carburizableProps"].Exists)
		{
			capi?.TriggerIngameError(this, "wrongfuel", Lang.Get("Next add some carburizable metal ingots"));
			return false;
		}
		ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, EnumMouseButton.Left, (EnumModifierKey)0, EnumMergePriority.DirectMerge, 1);
		if (slot.TryPutInto(inv[1], ref op) == 0)
		{
			capi?.TriggerIngameError(this, "cannotmixfuels", Lang.Get("Cannot mix ingots, it will mess with the carburisation process!"));
			return false;
		}
		updateSelectiveElements();
		MarkDirty(redrawOnClient: true);
		return true;
	}

	private void updateSelectiveElements()
	{
		List<string> main = new List<string>();
		List<string> secondary = new List<string>();
		bool isSteel = inv[1].Itemstack?.Collectible.FirstCodePart(1) == "blistersteel";
		for (int j = 0; j < IngotCount; j++)
		{
			List<string> obj = ((j % 4 >= 2) ? secondary : main);
			int num = 1 + j / 4 * 2 + j % 2;
			obj.Add("Charcoal" + (num + 1) / 2 + "/" + ((j >= 7 && isSteel) ? "Steel" : "Ingot") + num);
		}
		for (int i = 0; i < CoalLayerCount; i++)
		{
			main.Add("Charcoal" + (i + 1));
			secondary.Add("Charcoal" + (i + 1));
		}
		selectiveElementsMain = main.ToArray();
		selectiveElementsSecondary = secondary.ToArray();
	}

	private void onServerTick1s(float dt)
	{
		if (receivesHeat)
		{
			Vec3d pos = Pos.ToVec3d().Add(0.5, 0.5, 0.5).Add(blockScs.Orientation.Opposite.Normalf.X, 0.0, blockScs.Orientation.Opposite.Normalf.Z);
			Entity[] entitiesAround = Api.World.GetEntitiesAround(pos, 2.5f, 1f, (Entity e) => e.Alive && e is EntityAgent);
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
			onServerTick3s(dt);
		}
	}

	private void onServerTick3s(float dt)
	{
		BlockPos coalPilePos = Pos.DownCopy(2);
		BlockPos othercoalPilePos = coalPilePos.AddCopy(blockScs.Orientation.Opposite);
		bool num = receivesHeat;
		bool beforeStructureComplete = StructureComplete;
		if (!receivesHeat)
		{
			totalHoursLastUpdate = Api.World.Calendar.TotalHours;
		}
		float leftHeatHoursLeft = ((Api.World.BlockAccessor.GetBlockEntity(coalPilePos) is BlockEntityCoalPile { IsBurning: not false } becp2) ? becp2.GetHoursLeft(totalHoursLastUpdate) : 0f);
		float rightHeatHoursLeft = ((Api.World.BlockAccessor.GetBlockEntity(othercoalPilePos) is BlockEntityCoalPile { IsBurning: not false } becp) ? becp.GetHoursLeft(totalHoursLastUpdate) : 0f);
		receivesHeat = leftHeatHoursLeft > 0f && rightHeatHoursLeft > 0f;
		MultiblockStructure msInUse = null;
		BlockPos posInUse = null;
		StructureComplete = false;
		if (ms.InCompleteBlockCount(Api.World, Pos) == 0)
		{
			msInUse = ms;
			posInUse = Pos;
			StructureComplete = true;
		}
		else if (msOpp.InCompleteBlockCount(Api.World, Pos.AddCopy(blockScs.Orientation.Opposite)) == 0)
		{
			msInUse = msOpp;
			posInUse = Pos.AddCopy(blockScs.Orientation.Opposite);
			StructureComplete = true;
		}
		if (num != receivesHeat || beforeStructureComplete != StructureComplete)
		{
			MarkDirty();
		}
		if (processComplete || !IsFull || !hasLid())
		{
			return;
		}
		if (receivesHeat)
		{
			if (!StructureComplete)
			{
				return;
			}
			double hoursPassed = Api.World.Calendar.TotalHours - totalHoursLastUpdate;
			double heatHoursReceived = Math.Max(0f, GameMath.Min((float)hoursPassed, leftHeatHoursLeft, rightHeatHoursLeft));
			progress += heatHoursReceived / 160.0;
			totalHoursLastUpdate = Api.World.Calendar.TotalHours;
			float temp2 = inv[1].Itemstack.Collectible.GetTemperature(Api.World, inv[1].Itemstack);
			float tempGain = (float)(hoursPassed * 500.0);
			inv[1].Itemstack.Collectible.SetTemperature(Api.World, inv[1].Itemstack, Math.Min(800f, temp2 + tempGain));
			if (Math.Abs((float)tempStoneCoffin - temp2) > 25f)
			{
				tempStoneCoffin = (int)temp2;
				if (tempStoneCoffin > 500)
				{
					MarkDirty(redrawOnClient: true);
				}
			}
			MarkDirty();
		}
		if (!(progress >= 0.995))
		{
			return;
		}
		int stacksize = inv[1].Itemstack.StackSize;
		JsonItemStack jstack = inv[1].Itemstack.ItemAttributes?["carburizableProps"]["carburizedOutput"].AsObject<JsonItemStack>(null, base.Block.Code.Domain);
		if (jstack.Resolve(Api.World, "carburizable output"))
		{
			float temp = inv[1].Itemstack.Collectible.GetTemperature(Api.World, inv[0].Itemstack);
			inv[0].Itemstack.StackSize -= 8;
			inv[1].Itemstack = jstack.ResolvedItemstack.Clone();
			inv[1].Itemstack.StackSize = stacksize;
			inv[1].Itemstack.Collectible.SetTemperature(Api.World, inv[1].Itemstack, temp);
		}
		MarkDirty();
		msInUse.WalkMatchingBlocks(Api.World, posInUse, delegate(Block block, BlockPos pos)
		{
			float num2 = block.Attributes?["heatResistance"].AsFloat(1f) ?? 1f;
			if (Api.World.Rand.NextDouble() > (double)num2)
			{
				Block block2 = Api.World.GetBlock(block.CodeWithVariant("state", "damaged"));
				Api.World.BlockAccessor.SetBlock(block2.Id, pos);
			}
		});
		processComplete = true;
	}

	private bool hasLid()
	{
		if (Api.World.BlockAccessor.GetBlockAbove(Pos, 1, 1).FirstCodePart() == "stonecoffinlid")
		{
			return Api.World.BlockAccessor.GetBlockAbove(Pos.AddCopy(blockScs.Orientation.Opposite), 1, 1).FirstCodePart() == "stonecoffinlid";
		}
		return false;
	}

	private void onClientTick50ms(float dt)
	{
		if (!receivesHeat)
		{
			return;
		}
		receivesHeatSmooth = GameMath.Clamp(receivesHeatSmooth + (receivesHeat ? (dt / 10f) : ((0f - dt) / 3f)), 0f, 1f);
		if (receivesHeatSmooth == 0f)
		{
			return;
		}
		Random rnd = Api.World.Rand;
		for (int i = 0; i < Entity.FireParticleProps.Length; i++)
		{
			int index = Math.Min(Entity.FireParticleProps.Length - 1, Api.World.Rand.Next(Entity.FireParticleProps.Length + 1));
			AdvancedParticleProperties particles = Entity.FireParticleProps[index];
			for (int j = 0; j < particlePositions.Length; j++)
			{
				BlockPos pos = particlePositions[j];
				if (j >= 6)
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
					}) / 2f;
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

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		int coalLevel = inv[0]?.StackSize ?? 0;
		int ironLevel = inv[1]?.StackSize ?? 0;
		base.FromTreeAttributes(tree, worldAccessForResolve);
		receivesHeat = tree.GetBool("receivesHeat");
		totalHoursLastUpdate = tree.GetDouble("totalHoursLastUpdate");
		progress = tree.GetDouble("progress");
		processComplete = tree.GetBool("processComplete");
		StructureComplete = tree.GetBool("structureComplete");
		tempStoneCoffin = tree.GetInt("tempStoneCoffin");
		if (worldAccessForResolve.Api.Side == EnumAppSide.Client && (coalLevel != (inv[0]?.StackSize ?? 0) || ironLevel != (inv[1]?.StackSize ?? 0)))
		{
			ItemStack ingotStack = inv[1]?.Itemstack;
			if (ingotStack != null && ingotStack.Collectible == null)
			{
				ingotStack.ResolveBlockOrItem(worldAccessForResolve);
			}
			updateSelectiveElements();
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBool("receivesHeat", receivesHeat);
		tree.SetDouble("totalHoursLastUpdate", totalHoursLastUpdate);
		tree.SetDouble("progress", progress);
		tree.SetBool("processComplete", processComplete);
		tree.SetBool("structureComplete", StructureComplete);
		tree.SetInt("tempStoneCoffin", tempStoneCoffin);
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		if (Api.Side == EnumAppSide.Client)
		{
			msHighlighted?.ClearHighlights(Api.World, (Api as ICoreClientAPI).World.Player);
		}
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Client)
		{
			msHighlighted?.ClearHighlights(Api.World, (Api as ICoreClientAPI).World.Player);
		}
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (hasLid())
		{
			return false;
		}
		Shape shape = capi.TesselatorManager.GetCachedShape(base.Block.Shape.Base);
		tessThreadTesselator.TesselateShape(base.Block, shape, out var meshdataMain, null, null, selectiveElementsMain);
		tessThreadTesselator.TesselateShape(base.Block, shape, out var meshdataSecondary, null, null, selectiveElementsSecondary);
		if (blockScs.Orientation == BlockFacing.EAST)
		{
			meshdataMain.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, -(float)Math.PI / 2f, 0f);
			meshdataSecondary.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, -(float)Math.PI / 2f, 0f);
		}
		meshdataSecondary.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, (float)Math.PI, 0f);
		meshdataSecondary.Translate(blockScs.Orientation.Opposite.Normalf);
		mesher.AddMeshData(meshdataMain);
		mesher.AddMeshData(meshdataSecondary);
		return false;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		if (processComplete)
		{
			dsc.AppendLine(Lang.Get("Carburization process complete. Break to retrieve blister steel."));
			return;
		}
		if (IsFull)
		{
			if (!hasLid())
			{
				dsc.AppendLine(Lang.Get("Stone coffin lid is missing"));
			}
			else
			{
				if (!StructureComplete)
				{
					dsc.AppendLine(Lang.Get("Structure incomplete! Can't get hot enough, carburization paused."));
					return;
				}
				if (receivesHeat)
				{
					dsc.AppendLine(Lang.Get("Okay! Receives heat!"));
				}
				else
				{
					dsc.AppendLine(Lang.Get("Ready to be fired. Ignite a pile of coal below each stone coffin half."));
				}
			}
		}
		if (progress > 0.0)
		{
			dsc.AppendLine(Lang.Get("Carburization: {0}% complete", (int)(progress * 100.0)));
		}
	}
}
