using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockBehaviorUnstableFalling : BlockBehavior
{
	private bool ignorePlaceTest;

	private AssetLocation[] exceptions;

	public bool fallSideways;

	private float dustIntensity;

	private float fallSidewaysChance = 0.3f;

	private AssetLocation fallSound;

	private float impactDamageMul;

	private Cuboidi[] attachmentAreas;

	private BlockFacing[] attachableFaces;

	public BlockBehaviorUnstableFalling(Block block)
		: base(block)
	{
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);
		attachableFaces = null;
		if (properties["attachableFaces"].Exists)
		{
			string[] faces = properties["attachableFaces"].AsArray<string>();
			attachableFaces = new BlockFacing[faces.Length];
			for (int i = 0; i < faces.Length; i++)
			{
				attachableFaces[i] = BlockFacing.FromCode(faces[i]);
			}
		}
		Dictionary<string, RotatableCube> areas = properties["attachmentAreas"].AsObject<Dictionary<string, RotatableCube>>();
		attachmentAreas = new Cuboidi[6];
		if (areas != null)
		{
			foreach (KeyValuePair<string, RotatableCube> val in areas)
			{
				val.Value.Origin.Set(8.0, 8.0, 8.0);
				BlockFacing face = BlockFacing.FromFirstLetter(val.Key[0]);
				attachmentAreas[face.Index] = val.Value.RotatedCopy().ConvertToCuboidi();
			}
		}
		else
		{
			attachmentAreas[4] = properties["attachmentArea"].AsObject<Cuboidi>();
		}
		ignorePlaceTest = properties["ignorePlaceTest"].AsBool();
		exceptions = properties["exceptions"].AsObject(new AssetLocation[0], block.Code.Domain);
		fallSideways = properties["fallSideways"].AsBool();
		dustIntensity = properties["dustIntensity"].AsFloat();
		fallSidewaysChance = properties["fallSidewaysChance"].AsFloat(0.3f);
		string sound = properties["fallSound"].AsString();
		if (sound != null)
		{
			fallSound = AssetLocation.Create(sound, block.Code.Domain);
		}
		impactDamageMul = properties["impactDamageMul"].AsFloat(1f);
	}

	public override bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
	{
		handling = EnumHandling.PassThrough;
		if (ignorePlaceTest)
		{
			return true;
		}
		Cuboidi attachmentArea = attachmentAreas[4];
		BlockPos pos = blockSel.Position.DownCopy();
		Block onBlock = world.BlockAccessor.GetBlock(pos);
		if (blockSel != null && !IsAttached(world.BlockAccessor, blockSel.Position) && !onBlock.CanAttachBlockAt(world.BlockAccessor, block, pos, BlockFacing.UP, attachmentArea))
		{
			JsonObject attributes = block.Attributes;
			if ((attributes == null || !attributes["allowUnstablePlacement"].AsBool()) && !exceptions.Contains(onBlock.Code))
			{
				handling = EnumHandling.PreventSubsequent;
				failureCode = "requiresolidground";
				return false;
			}
		}
		return true;
	}

	public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ref EnumHandling handling)
	{
		TryFalling(world, blockPos, ref handling);
		base.OnBlockPlaced(world, blockPos, ref handling);
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handling)
	{
		base.OnNeighbourBlockChange(world, pos, neibpos, ref handling);
		if (world.Side != EnumAppSide.Client)
		{
			EnumHandling bla = EnumHandling.PassThrough;
			TryFalling(world, pos, ref bla);
		}
	}

	private bool TryFalling(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
	{
		if (world.Side != EnumAppSide.Server)
		{
			return false;
		}
		if (!fallSideways && IsAttached(world.BlockAccessor, pos))
		{
			return false;
		}
		if (!((world as IServerWorldAccessor).Api as ICoreServerAPI).Server.Config.AllowFallingBlocks)
		{
			return false;
		}
		if (IsReplacableBeneath(world, pos) || (fallSideways && world.Rand.NextDouble() < (double)fallSidewaysChance && IsReplacableBeneathAndSideways(world, pos)))
		{
			if (world.GetNearestEntity(pos.ToVec3d().Add(0.5, 0.5, 0.5), 1f, 1.5f, (Entity e) => e is EntityBlockFalling entityBlockFalling && entityBlockFalling.initialPos.Equals(pos)) == null)
			{
				EntityBlockFalling entityblock = new EntityBlockFalling(block, world.BlockAccessor.GetBlockEntity(pos), pos, fallSound, impactDamageMul, canFallSideways: true, dustIntensity);
				world.SpawnEntity(entityblock);
			}
			handling = EnumHandling.PreventSubsequent;
			return true;
		}
		handling = EnumHandling.PassThrough;
		return false;
	}

	public virtual bool IsAttached(IBlockAccessor blockAccessor, BlockPos pos)
	{
		BlockPos tmpPos;
		if (attachableFaces == null)
		{
			tmpPos = pos.DownCopy();
			return blockAccessor.GetBlock(tmpPos).CanAttachBlockAt(blockAccessor, block, tmpPos, BlockFacing.UP, attachmentAreas[5]);
		}
		tmpPos = new BlockPos();
		for (int i = 0; i < attachableFaces.Length; i++)
		{
			BlockFacing face = attachableFaces[i];
			tmpPos.Set(pos).Add(face);
			if (blockAccessor.GetBlock(tmpPos).CanAttachBlockAt(blockAccessor, block, tmpPos, face.Opposite, attachmentAreas[face.Index]))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsReplacableBeneathAndSideways(IWorldAccessor world, BlockPos pos)
	{
		for (int i = 0; i < 4; i++)
		{
			BlockFacing facing = BlockFacing.HORIZONTALS[i];
			Block nBlock = world.BlockAccessor.GetBlockOrNull(pos.X + facing.Normali.X, pos.Y + facing.Normali.Y, pos.Z + facing.Normali.Z);
			if (nBlock != null && nBlock.Replaceable >= 6000)
			{
				nBlock = world.BlockAccessor.GetBlockOrNull(pos.X + facing.Normali.X, pos.Y + facing.Normali.Y - 1, pos.Z + facing.Normali.Z);
				if (nBlock != null && nBlock.Replaceable >= 6000)
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool IsReplacableBeneath(IWorldAccessor world, BlockPos pos)
	{
		return world.BlockAccessor.GetBlockBelow(pos).Replaceable > 6000;
	}
}
