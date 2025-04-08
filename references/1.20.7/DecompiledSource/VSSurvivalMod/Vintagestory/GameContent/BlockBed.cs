using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockBed : Block
{
	public static IMountableSeat GetMountable(IWorldAccessor world, TreeAttribute tree)
	{
		BlockPos pos = new BlockPos(tree.GetInt("posx"), tree.GetInt("posy"), tree.GetInt("posz"));
		Block block = world.BlockAccessor.GetBlock(pos);
		BlockFacing facing = BlockFacing.FromCode(block.LastCodePart());
		return world.BlockAccessor.GetBlockEntity((block.LastCodePart(1) == "feet") ? pos.AddCopy(facing) : pos) as BlockEntityBed;
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
		{
			byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
			return false;
		}
		if (CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			BlockFacing[] horVer = Block.SuggestedHVOrientation(byPlayer, blockSel);
			BlockPos secondPos = blockSel.Position.AddCopy(horVer[0]);
			BlockSelection secondBlockSel = new BlockSelection
			{
				Position = secondPos,
				Face = BlockFacing.UP
			};
			if (!CanPlaceBlock(world, byPlayer, secondBlockSel, ref failureCode))
			{
				return false;
			}
			string code = horVer[0].Opposite.Code;
			world.BlockAccessor.GetBlock(CodeWithParts("head", code)).DoPlaceBlock(world, byPlayer, secondBlockSel, itemstack);
			AssetLocation feetCode = CodeWithParts("feet", code);
			world.BlockAccessor.GetBlock(feetCode).DoPlaceBlock(world, byPlayer, blockSel, itemstack);
			return true;
		}
		return false;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
		{
			return false;
		}
		BlockFacing facing = BlockFacing.FromCode(LastCodePart()).Opposite;
		if (!(world.BlockAccessor.GetBlockEntity((LastCodePart(1) == "feet") ? blockSel.Position.AddCopy(facing) : blockSel.Position) is BlockEntityBed beBed))
		{
			return false;
		}
		if (beBed.MountedBy != null)
		{
			return false;
		}
		if (byPlayer.Entity.GetBehavior("tiredness") is EntityBehaviorTiredness { Tiredness: <=8f })
		{
			if (world.Side == EnumAppSide.Client)
			{
				(api as ICoreClientAPI).TriggerIngameError(this, "nottiredenough", Lang.Get("not-tired-enough"));
			}
			else
			{
				byPlayer.Entity.TryUnmount();
			}
			return false;
		}
		if (api.World.Config.GetString("temporalStormSleeping", "0").ToInt() == 0 && api.ModLoader.GetModSystem<SystemTemporalStability>().StormStrength > 0f)
		{
			if (world.Side == EnumAppSide.Client)
			{
				(api as ICoreClientAPI).TriggerIngameError(this, "cantsleep-tempstorm", Lang.Get("cantsleep-tempstorm"));
			}
			else
			{
				byPlayer.Entity.TryUnmount();
			}
			return false;
		}
		return byPlayer.Entity.TryMount(beBed);
	}

	public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
	{
		string headfoot = LastCodePart(1);
		BlockFacing facing = BlockFacing.FromCode(LastCodePart());
		if (LastCodePart(1) == "feet")
		{
			facing = facing.Opposite;
		}
		else
		{
			(world.BlockAccessor.GetBlockEntity(pos) as BlockEntityBed)?.MountedBy?.TryUnmount();
		}
		Block secondPlock = world.BlockAccessor.GetBlock(pos.AddCopy(facing));
		if (secondPlock is BlockBed && secondPlock.LastCodePart(1) != headfoot)
		{
			world.BlockAccessor.SetBlock(0, pos.AddCopy(facing));
		}
		base.OnBlockRemoved(world, pos);
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return GetHandbookDropsFromBreakDrops(handbookStack, forPlayer);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		return new ItemStack[1]
		{
			new ItemStack(world.BlockAccessor.GetBlock(CodeWithParts("head", "north")))
		};
	}

	public override AssetLocation GetRotatedBlockCode(int angle)
	{
		int rotatedIndex = GameMath.Mod(BlockFacing.FromCode(LastCodePart()).HorizontalAngleIndex - angle / 90, 4);
		BlockFacing nowFacing = BlockFacing.HORIZONTALS_ANGLEORDER[rotatedIndex];
		return CodeWithParts(nowFacing.Code);
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		return new ItemStack(world.BlockAccessor.GetBlock(CodeWithParts("head", "north")));
	}

	public override AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis)
	{
		BlockFacing facing = BlockFacing.FromCode(LastCodePart());
		if (facing.Axis == axis)
		{
			return CodeWithParts(facing.Opposite.Code);
		}
		return Code;
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		double sleephours = inSlot.Itemstack.Collectible.Attributes["sleepEfficiency"].AsDouble() * (double)world.Calendar.HoursPerDay / 2.0;
		dsc.AppendLine("\n" + Lang.Get("Lets you sleep for {0} hours a day", sleephours.ToString("#.#")));
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		float sleepEfficiency = 0.5f;
		if (Attributes?["sleepEfficiency"] != null)
		{
			sleepEfficiency = Attributes["sleepEfficiency"].AsFloat(0.5f);
		}
		return base.GetPlacedBlockInfo(world, pos, forPlayer) + Lang.Get("Lets you sleep for up to {0} hours", Math.Round(sleepEfficiency * world.Calendar.HoursPerDay / 2f, 2));
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-bed-sleep",
				MouseButton = EnumMouseButton.Right
			}
		}.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}

	public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
	{
		if (isImpact && facing.Axis == EnumAxis.Y)
		{
			if (Sounds?.Break != null && Math.Abs(collideSpeed.Y) > 0.2)
			{
				world.PlaySoundAt(Sounds.Break, entity);
			}
			entity.Pos.Motion.Y = GameMath.Clamp((0.0 - entity.Pos.Motion.Y) * 0.8, -0.5, 0.5);
		}
	}
}
