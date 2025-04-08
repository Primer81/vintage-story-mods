using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemAxe : Item
{
	private const int LeafGroups = 7;

	private static SimpleParticleProperties dustParticles;

	static ItemAxe()
	{
		dustParticles = new SimpleParticleProperties
		{
			MinPos = new Vec3d(),
			AddPos = new Vec3d(),
			MinQuantity = 0f,
			AddQuantity = 3f,
			Color = ColorUtil.ToRgba(100, 200, 200, 200),
			GravityEffect = 1f,
			WithTerrainCollision = true,
			ParticleModel = EnumParticleModel.Quad,
			LifeLength = 0.5f,
			MinVelocity = new Vec3f(-1f, 2f, -1f),
			AddVelocity = new Vec3f(2f, 0f, 2f),
			MinSize = 0.07f,
			MaxSize = 0.1f,
			WindAffected = true
		};
		dustParticles.ParticleModel = EnumParticleModel.Quad;
		dustParticles.AddPos.Set(1.0, 1.0, 1.0);
		dustParticles.MinQuantity = 2f;
		dustParticles.AddQuantity = 12f;
		dustParticles.LifeLength = 4f;
		dustParticles.MinSize = 0.2f;
		dustParticles.MaxSize = 0.5f;
		dustParticles.MinVelocity.Set(-0.4f, -0.4f, -0.4f);
		dustParticles.AddVelocity.Set(0.8f, 1.2f, 0.8f);
		dustParticles.DieOnRainHeightmap = false;
		dustParticles.WindAffectednes = 0.5f;
	}

	public override string GetHeldTpHitAnimation(ItemSlot slot, Entity byEntity)
	{
		if ((byEntity as EntityPlayer)?.EntitySelection != null)
		{
			return "axehit";
		}
		return base.GetHeldTpHitAnimation(slot, byEntity);
	}

	public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
	{
		base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
	}

	public override bool OnHeldAttackStep(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
	{
		return base.OnHeldAttackStep(secondsPassed, slot, byEntity, blockSelection, entitySel);
	}

	public override float OnBlockBreaking(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
	{
		ITreeAttribute tempAttr = itemslot.Itemstack.TempAttributes;
		int posx = tempAttr.GetInt("lastposX", -1);
		int posy = tempAttr.GetInt("lastposY", -1);
		int posz = tempAttr.GetInt("lastposZ", -1);
		BlockPos pos = blockSel.Position;
		float treeResistance;
		if (pos.X != posx || pos.Y != posy || pos.Z != posz || counter % 30 == 0)
		{
			FindTree(player.Entity.World, pos, out var baseResistance, out var woodTier);
			if (ToolTier < woodTier - 3)
			{
				return remainingResistance;
			}
			treeResistance = (float)Math.Max(1.0, Math.Sqrt((double)baseResistance / 1.45));
			tempAttr.SetFloat("treeResistance", treeResistance);
		}
		else
		{
			treeResistance = tempAttr.GetFloat("treeResistance", 1f);
		}
		tempAttr.SetInt("lastposX", pos.X);
		tempAttr.SetInt("lastposY", pos.Y);
		tempAttr.SetInt("lastposZ", pos.Z);
		return base.OnBlockBreaking(player, blockSel, itemslot, remainingResistance, dt / treeResistance, counter);
	}

	public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier = 1f)
	{
		IPlayer byPlayer = null;
		if (byEntity is EntityPlayer)
		{
			byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		double windspeed = api.ModLoader.GetModSystem<WeatherSystemBase>()?.WeatherDataSlowAccess.GetWindSpeed(byEntity.SidedPos.XYZ) ?? 0.0;
		int resistance;
		int woodTier;
		Stack<BlockPos> foundPositions = FindTree(world, blockSel.Position, out resistance, out woodTier);
		if (foundPositions.Count == 0)
		{
			return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier);
		}
		bool damageable = DamagedBy != null && DamagedBy.Contains(EnumItemDamageSource.BlockBreaking);
		float leavesMul = 1f;
		float leavesBranchyMul = 0.8f;
		int blocksbroken = 0;
		bool axeHasDurability = true;
		while (foundPositions.Count > 0)
		{
			BlockPos pos = foundPositions.Pop();
			Block block = world.BlockAccessor.GetBlock(pos);
			bool isLog = block.BlockMaterial == EnumBlockMaterial.Wood;
			if (isLog && !axeHasDurability)
			{
				continue;
			}
			blocksbroken++;
			bool isBranchy = block.Code.Path.Contains("branchy");
			bool isLeaves = block.BlockMaterial == EnumBlockMaterial.Leaves;
			world.BlockAccessor.BreakBlock(pos, byPlayer, isLeaves ? leavesMul : (isBranchy ? leavesBranchyMul : 1f));
			if (world.Side == EnumAppSide.Client)
			{
				dustParticles.Color = block.GetRandomColor(world.Api as ICoreClientAPI, pos, BlockFacing.UP);
				dustParticles.Color |= -16777216;
				dustParticles.MinPos.Set(pos.X, pos.Y, pos.Z);
				if (block.BlockMaterial == EnumBlockMaterial.Leaves)
				{
					dustParticles.GravityEffect = (float)world.Rand.NextDouble() * 0.1f + 0.01f;
					dustParticles.ParticleModel = EnumParticleModel.Quad;
					dustParticles.MinVelocity.Set(-0.4f + 4f * (float)windspeed, -0.4f, -0.4f);
					dustParticles.AddVelocity.Set(0.8f + 4f * (float)windspeed, 1.2f, 0.8f);
				}
				else
				{
					dustParticles.GravityEffect = 0.8f;
					dustParticles.ParticleModel = EnumParticleModel.Cube;
					dustParticles.MinVelocity.Set(-0.4f + (float)windspeed, -0.4f, -0.4f);
					dustParticles.AddVelocity.Set(0.8f + (float)windspeed, 1.2f, 0.8f);
				}
				world.SpawnParticles(dustParticles);
			}
			if (damageable && isLog)
			{
				DamageItem(world, byEntity, itemslot);
				if (itemslot.Itemstack == null)
				{
					axeHasDurability = false;
				}
			}
			if (isLeaves && leavesMul > 0.03f)
			{
				leavesMul *= 0.85f;
			}
			if (isBranchy && leavesBranchyMul > 0.015f)
			{
				leavesBranchyMul *= 0.7f;
			}
		}
		if (blocksbroken > 35 && axeHasDurability)
		{
			api.World.PlaySoundAt(new AssetLocation("sounds/effect/treefell"), blockSel.Position, -0.25, byPlayer, randomizePitch: false, 32f, GameMath.Clamp((float)blocksbroken / 100f, 0.25f, 1f));
		}
		return true;
	}

	public Stack<BlockPos> FindTree(IWorldAccessor world, BlockPos startPos, out int resistance, out int woodTier)
	{
		Queue<Vec4i> queue = new Queue<Vec4i>();
		Queue<Vec4i> leafqueue = new Queue<Vec4i>();
		HashSet<BlockPos> checkedPositions = new HashSet<BlockPos>();
		Stack<BlockPos> foundPositions = new Stack<BlockPos>();
		resistance = 0;
		woodTier = 0;
		Block block = world.BlockAccessor.GetBlock(startPos);
		if (block.Code == null)
		{
			return foundPositions;
		}
		string treeFellingGroupCode = block.Attributes?["treeFellingGroupCode"].AsString();
		int spreadIndex = block.Attributes?["treeFellingGroupSpreadIndex"].AsInt() ?? 0;
		JsonObject attributes = block.Attributes;
		if (attributes != null && !attributes["treeFellingCanChop"].AsBool(defaultValue: true))
		{
			return foundPositions;
		}
		EnumTreeFellingBehavior bh = EnumTreeFellingBehavior.Chop;
		if (block is ICustomTreeFellingBehavior ctfbh)
		{
			bh = ctfbh.GetTreeFellingBehavior(startPos, null, spreadIndex);
			if (bh == EnumTreeFellingBehavior.NoChop)
			{
				resistance = foundPositions.Count;
				return foundPositions;
			}
		}
		if (spreadIndex < 2)
		{
			return foundPositions;
		}
		if (treeFellingGroupCode == null)
		{
			return foundPositions;
		}
		queue.Enqueue(new Vec4i(startPos, spreadIndex));
		checkedPositions.Add(startPos);
		int[] adjacentLeafGroupsCounts = new int[7];
		while (queue.Count > 0)
		{
			Vec4i pos2 = queue.Dequeue();
			foundPositions.Push(new BlockPos(pos2.X, pos2.Y, pos2.Z));
			resistance += pos2.W + 1;
			if (woodTier == 0)
			{
				woodTier = pos2.W;
			}
			if (foundPositions.Count > 2500)
			{
				break;
			}
			block = world.BlockAccessor.GetBlockRaw(pos2.X, pos2.Y, pos2.Z, 1);
			if (block is ICustomTreeFellingBehavior ctfbhh)
			{
				bh = ctfbhh.GetTreeFellingBehavior(startPos, null, spreadIndex);
			}
			if (bh != 0)
			{
				onTreeBlock(pos2, world.BlockAccessor, checkedPositions, startPos, bh == EnumTreeFellingBehavior.ChopSpreadVertical, treeFellingGroupCode, queue, leafqueue, adjacentLeafGroupsCounts);
			}
		}
		int maxCount = 0;
		int maxI = -1;
		for (int i = 0; i < adjacentLeafGroupsCounts.Length; i++)
		{
			if (adjacentLeafGroupsCounts[i] > maxCount)
			{
				maxCount = adjacentLeafGroupsCounts[i];
				maxI = i;
			}
		}
		if (maxI >= 0)
		{
			treeFellingGroupCode = maxI + 1 + treeFellingGroupCode;
		}
		while (leafqueue.Count > 0)
		{
			Vec4i pos = leafqueue.Dequeue();
			foundPositions.Push(new BlockPos(pos.X, pos.Y, pos.Z));
			resistance += pos.W + 1;
			if (foundPositions.Count > 2500)
			{
				break;
			}
			onTreeBlock(pos, world.BlockAccessor, checkedPositions, startPos, bh == EnumTreeFellingBehavior.ChopSpreadVertical, treeFellingGroupCode, leafqueue, null, null);
		}
		return foundPositions;
	}

	private void onTreeBlock(Vec4i pos, IBlockAccessor blockAccessor, HashSet<BlockPos> checkedPositions, BlockPos startPos, bool chopSpreadVertical, string treeFellingGroupCode, Queue<Vec4i> queue, Queue<Vec4i> leafqueue, int[] adjacentLeaves)
	{
		for (int i = 0; i < Vec3i.DirectAndIndirectNeighbours.Length; i++)
		{
			Vec3i facing = Vec3i.DirectAndIndirectNeighbours[i];
			BlockPos neibPos = new BlockPos(pos.X + facing.X, pos.Y + facing.Y, pos.Z + facing.Z);
			float num = GameMath.Sqrt(neibPos.HorDistanceSqTo(startPos.X, startPos.Z));
			float vertdist = neibPos.Y - startPos.Y;
			float f = (chopSpreadVertical ? 0.5f : 2f);
			if (num - 1f >= f * vertdist || checkedPositions.Contains(neibPos))
			{
				continue;
			}
			Block block = blockAccessor.GetBlock(neibPos, 1);
			if (block.Code == null || block.Id == 0)
			{
				continue;
			}
			string ngcode = block.Attributes?["treeFellingGroupCode"].AsString();
			Queue<Vec4i> outqueue;
			if (ngcode != treeFellingGroupCode)
			{
				if (ngcode == null || leafqueue == null || block.BlockMaterial != EnumBlockMaterial.Leaves || ngcode.Length != treeFellingGroupCode.Length + 1 || !ngcode.EndsWithOrdinal(treeFellingGroupCode))
				{
					continue;
				}
				outqueue = leafqueue;
				int leafGroup = GameMath.Clamp(ngcode[0] - 48, 1, 7);
				adjacentLeaves[leafGroup - 1]++;
			}
			else
			{
				outqueue = queue;
			}
			int nspreadIndex = block.Attributes?["treeFellingGroupSpreadIndex"].AsInt() ?? 0;
			if (pos.W >= nspreadIndex)
			{
				checkedPositions.Add(neibPos);
				if (!chopSpreadVertical || facing.Equals(0, 1, 0) || nspreadIndex <= 0)
				{
					outqueue.Enqueue(new Vec4i(neibPos, nspreadIndex));
				}
			}
		}
	}
}
