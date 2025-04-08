using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent.Mechanics;

public class BlockAngledGears : BlockMPBase
{
	public string Orientation;

	public BlockFacing[] Facings
	{
		get
		{
			string dirs = Orientation;
			BlockFacing[] facings = new BlockFacing[dirs.Length];
			for (int i = 0; i < dirs.Length; i++)
			{
				facings[i] = BlockFacing.FromFirstLetter(dirs[i]);
			}
			if (facings.Length == 2 && facings[1] == facings[0])
			{
				facings[1] = facings[1].Opposite;
			}
			return facings;
		}
	}

	public override void OnLoaded(ICoreAPI api)
	{
		Orientation = Variant["orientation"];
		base.OnLoaded(api);
	}

	public bool IsDeadEnd()
	{
		return Orientation.Length == 1;
	}

	public bool IsOrientedTo(BlockFacing facing)
	{
		string dirs = Orientation;
		if (dirs[0] == facing.Code[0])
		{
			return true;
		}
		if (dirs.Length == 1)
		{
			return false;
		}
		if (dirs[0] == dirs[1])
		{
			return dirs[0] == facing.Opposite.Code[0];
		}
		return dirs[1] == facing.Code[0];
	}

	public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
	{
		if (IsDeadEnd() && BlockFacing.FromFirstLetter(Orientation[0]).IsAdjacent(face))
		{
			return true;
		}
		return IsOrientedTo(face);
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		return new ItemStack(world.GetBlock(new AssetLocation("angledgears-s")));
	}

	public Block getGearBlock(IWorldAccessor world, bool cageGear, BlockFacing facing, BlockFacing adjFacing = null)
	{
		char reference;
		char reference2;
		char reference3;
		if (adjFacing == null)
		{
			char orient = facing.Code[0];
			string text = FirstCodePart();
			string text2;
			if (!cageGear)
			{
				reference = '-';
				ReadOnlySpan<char> readOnlySpan = new ReadOnlySpan<char>(in reference);
				reference2 = orient;
				text2 = string.Concat(readOnlySpan, new ReadOnlySpan<char>(in reference2));
			}
			else
			{
				reference2 = '-';
				ReadOnlySpan<char> readOnlySpan2 = new ReadOnlySpan<char>(in reference2);
				reference = orient;
				ReadOnlySpan<char> readOnlySpan3 = new ReadOnlySpan<char>(in reference);
				reference3 = orient;
				text2 = string.Concat(readOnlySpan2, readOnlySpan3, new ReadOnlySpan<char>(in reference3));
			}
			return world.GetBlock(new AssetLocation(text + text2));
		}
		ReadOnlySpan<char> readOnlySpan4 = FirstCodePart();
		reference3 = '-';
		ReadOnlySpan<char> readOnlySpan5 = new ReadOnlySpan<char>(in reference3);
		reference = adjFacing.Code[0];
		ReadOnlySpan<char> readOnlySpan6 = new ReadOnlySpan<char>(in reference);
		reference2 = facing.Code[0];
		AssetLocation loc = new AssetLocation(string.Concat(readOnlySpan4, readOnlySpan5, readOnlySpan6, new ReadOnlySpan<char>(in reference2)));
		Block toPlaceBlock = world.GetBlock(loc);
		if (toPlaceBlock == null)
		{
			ReadOnlySpan<char> readOnlySpan7 = FirstCodePart();
			reference2 = '-';
			ReadOnlySpan<char> readOnlySpan8 = new ReadOnlySpan<char>(in reference2);
			reference = facing.Code[0];
			ReadOnlySpan<char> readOnlySpan9 = new ReadOnlySpan<char>(in reference);
			reference3 = adjFacing.Code[0];
			loc = new AssetLocation(string.Concat(readOnlySpan7, readOnlySpan8, readOnlySpan9, new ReadOnlySpan<char>(in reference3)));
			toPlaceBlock = world.GetBlock(loc);
		}
		return toPlaceBlock;
	}

	public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
	{
		if (IsDeadEnd() && BlockFacing.FromFirstLetter(Orientation[0]).IsAdjacent(face))
		{
			(getGearBlock(world, cageGear: false, Facings[0], face) as BlockMPBase).ExchangeBlockAt(world, pos);
		}
	}

	public bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref string failureCode, Block blockExisting)
	{
		if (blockExisting is BlockMPMultiblockGear testMultiblock && !testMultiblock.IsReplacableByGear(world, blockSel.Position))
		{
			failureCode = "notreplaceable";
			return false;
		}
		return base.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode);
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		Block blockExisting = world.BlockAccessor.GetBlock(blockSel.Position);
		if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode, blockExisting))
		{
			return false;
		}
		BlockFacing firstFace = null;
		BlockFacing secondFace = null;
		BlockMPMultiblockGear largeGearEdge = blockExisting as BlockMPMultiblockGear;
		bool validLargeGear = false;
		if (largeGearEdge != null && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEMPMultiblock be)
		{
			validLargeGear = be.Principal != null;
		}
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing face in aLLFACES)
		{
			if (validLargeGear && (face == BlockFacing.UP || face == BlockFacing.DOWN))
			{
				continue;
			}
			BlockPos pos = blockSel.Position.AddCopy(face);
			if (world.BlockAccessor.GetBlock(pos) is IMechanicalPowerBlock block && block.HasMechPowerConnectorAt(world, pos, face.Opposite))
			{
				if (firstFace == null)
				{
					firstFace = face;
				}
				else if (face.IsAdjacent(firstFace))
				{
					secondFace = face;
					break;
				}
			}
		}
		if (firstFace != null)
		{
			BlockPos firstPos = blockSel.Position.AddCopy(firstFace);
			BlockEntity blockEntity = world.BlockAccessor.GetBlockEntity(firstPos);
			IMechanicalPowerBlock neighbour = blockEntity?.Block as IMechanicalPowerBlock;
			if (blockEntity?.GetBehavior<BEBehaviorMPAxle>() != null && !BEBehaviorMPAxle.IsAttachedToBlock(world.BlockAccessor, neighbour as Block, firstPos))
			{
				failureCode = "axlemusthavesupport";
				return false;
			}
			BlockEntity obj = (validLargeGear ? largeGearEdge.GearPlaced(world, blockSel.Position) : null);
			Block toPlaceBlock = getGearBlock(world, validLargeGear, firstFace, secondFace);
			world.BlockAccessor.SetBlock(toPlaceBlock.BlockId, blockSel.Position);
			if (secondFace != null)
			{
				BlockPos secondPos = blockSel.Position.AddCopy(secondFace);
				(world.BlockAccessor.GetBlock(secondPos) as IMechanicalPowerBlock)?.DidConnectAt(world, secondPos, secondFace.Opposite);
			}
			BEBehaviorMPAngledGears beAngledGear = world.BlockAccessor.GetBlockEntity(blockSel.Position)?.GetBehavior<BEBehaviorMPAngledGears>();
			if (obj?.GetBehavior<BEBehaviorMPBase>() is BEBehaviorMPLargeGear3m largeGear)
			{
				beAngledGear.AddToLargeGearNetwork(largeGear, firstFace);
			}
			neighbour?.DidConnectAt(world, firstPos, firstFace.Opposite);
			beAngledGear.newlyPlaced = true;
			if (!beAngledGear.tryConnect(firstFace) && secondFace != null)
			{
				beAngledGear.tryConnect(secondFace);
			}
			beAngledGear.newlyPlaced = false;
			return true;
		}
		failureCode = "requiresaxle";
		return false;
	}

	public override void WasPlaced(IWorldAccessor world, BlockPos ownPos, BlockFacing connectedOnFacing)
	{
		if (connectedOnFacing != null)
		{
			(world.BlockAccessor.GetBlockEntity(ownPos)?.GetBehavior<BEBehaviorMPAngledGears>())?.tryConnect(connectedOnFacing);
		}
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		string orients = Orientation;
		if (orients.Length == 2 && orients[0] == orients[1])
		{
			orients = orients[0].ToString() ?? "";
		}
		BlockFacing[] obj = ((orients.Length != 1) ? new BlockFacing[2]
		{
			BlockFacing.FromFirstLetter(orients[0]),
			BlockFacing.FromFirstLetter(orients[1])
		} : new BlockFacing[1] { BlockFacing.FromFirstLetter(orients[0]) });
		List<BlockFacing> lostFacings = new List<BlockFacing>();
		BlockFacing[] array = obj;
		foreach (BlockFacing facing in array)
		{
			BlockPos npos = pos.AddCopy(facing);
			if (world.BlockAccessor.GetBlock(npos) is IMechanicalPowerBlock nblock && nblock.HasMechPowerConnectorAt(world, npos, facing.Opposite))
			{
				BlockEntity blockEntity = world.BlockAccessor.GetBlockEntity(pos);
				if (blockEntity == null || !(blockEntity.GetBehavior<BEBehaviorMPBase>()?.disconnected).GetValueOrDefault())
				{
					continue;
				}
			}
			lostFacings.Add(facing);
		}
		if (lostFacings.Count == orients.Length)
		{
			world.BlockAccessor.BreakBlock(pos, null);
		}
		else if (lostFacings.Count > 0)
		{
			orients = orients.Replace(lostFacings[0].Code[0].ToString() ?? "", "");
			(world.GetBlock(new AssetLocation(FirstCodePart() + "-" + orients)) as BlockMPBase).ExchangeBlockAt(world, pos);
			world.BlockAccessor.GetBlockEntity(pos).GetBehavior<BEBehaviorMPBase>().LeaveNetwork();
			BlockFacing firstFace = BlockFacing.FromFirstLetter(orients[0]);
			BlockPos firstPos = pos.AddCopy(firstFace);
			BlockEntity blockEntity2 = world.BlockAccessor.GetBlockEntity(firstPos);
			IMechanicalPowerBlock neighbour = blockEntity2?.Block as IMechanicalPowerBlock;
			if (blockEntity2?.GetBehavior<BEBehaviorMPAxle>() == null || BEBehaviorMPAxle.IsAttachedToBlock(world.BlockAccessor, neighbour as Block, firstPos))
			{
				neighbour?.DidConnectAt(world, firstPos, firstFace.Opposite);
				WasPlaced(world, pos, firstFace);
			}
		}
	}

	public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
	{
		bool preventDefault = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			obj.OnBlockRemoved(world, pos, ref handled);
			switch (handled)
			{
			case EnumHandling.PreventSubsequent:
				return;
			case EnumHandling.PreventDefault:
				preventDefault = true;
				break;
			}
		}
		if (!preventDefault)
		{
			world.BlockAccessor.RemoveBlockEntity(pos);
			string orient = Variant["orientation"];
			if (orient.Length == 2 && orient[1] == orient[0])
			{
				BlockMPMultiblockGear.OnGearDestroyed(world, pos, orient[0]);
			}
		}
	}

	internal void ToPegGear(IWorldAccessor world, BlockPos pos)
	{
		string orient = Variant["orientation"];
		if (orient.Length == 2 && orient[1] == orient[0])
		{
			ReadOnlySpan<char> readOnlySpan = FirstCodePart();
			char reference = '-';
			ReadOnlySpan<char> readOnlySpan2 = new ReadOnlySpan<char>(in reference);
			char reference2 = orient[0];
			((BlockMPBase)world.GetBlock(new AssetLocation(string.Concat(readOnlySpan, readOnlySpan2, new ReadOnlySpan<char>(in reference2))))).ExchangeBlockAt(world, pos);
			(world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorMPAngledGears>())?.ClearLargeGear();
		}
	}
}
