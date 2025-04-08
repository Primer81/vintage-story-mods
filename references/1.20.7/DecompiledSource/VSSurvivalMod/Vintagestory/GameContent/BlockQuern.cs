using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent.Mechanics;

namespace Vintagestory.GameContent;

public class BlockQuern : BlockMPBase
{
	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		bool num = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
		if (num && !tryConnect(world, byPlayer, blockSel.Position, BlockFacing.UP))
		{
			tryConnect(world, byPlayer, blockSel.Position, BlockFacing.DOWN);
		}
		return num;
	}

	public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
	{
		return true;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (blockSel != null && !world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
		{
			return false;
		}
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityQuern beQuern && beQuern.CanGrind() && (blockSel.SelectionBoxIndex == 1 || beQuern.Inventory.openedByPlayerGUIds.Contains(byPlayer.PlayerUID)))
		{
			beQuern.SetPlayerGrinding(byPlayer, playerGrinding: true);
			return true;
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityQuern beQuern && (blockSel.SelectionBoxIndex == 1 || beQuern.Inventory.openedByPlayerGUIds.Contains(byPlayer.PlayerUID)))
		{
			beQuern.IsGrinding(byPlayer);
			return beQuern.CanGrind();
		}
		return false;
	}

	public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityQuern beQuern)
		{
			beQuern.SetPlayerGrinding(byPlayer, playerGrinding: false);
		}
	}

	public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
	{
		if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityQuern beQuern)
		{
			beQuern.SetPlayerGrinding(byPlayer, playerGrinding: false);
		}
		return true;
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		if (selection.SelectionBoxIndex == 0)
		{
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-quern-addremoveitems",
					MouseButton = EnumMouseButton.Right
				}
			}.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
		}
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-quern-grind",
				MouseButton = EnumMouseButton.Right,
				ShouldApply = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => world.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityQuern blockEntityQuern && blockEntityQuern.CanGrind()
			}
		}.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}

	public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
	{
	}

	public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
	{
		if (face != BlockFacing.UP)
		{
			return face == BlockFacing.DOWN;
		}
		return true;
	}

	public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
	{
		base.OnEntityCollide(world, entity, pos, facing, collideSpeed, isImpact);
		if (facing != BlockFacing.UP)
		{
			return;
		}
		if (entity.World.Side == EnumAppSide.Server)
		{
			float frameTime = GlobalConstants.PhysicsFrameTime;
			BEBehaviorMPConsumer mpc = GetBEBehavior<BEBehaviorMPConsumer>(pos);
			if (mpc != null)
			{
				entity.SidedPos.Yaw += frameTime * mpc.TrueSpeed * 2.5f * (float)((!mpc.isRotationReversed()) ? 1 : (-1));
			}
			return;
		}
		float frameTime2 = GlobalConstants.PhysicsFrameTime;
		BEBehaviorMPConsumer mpc2 = GetBEBehavior<BEBehaviorMPConsumer>(pos);
		ICoreClientAPI capi = api as ICoreClientAPI;
		if (capi.World.Player.Entity.EntityId == entity.EntityId)
		{
			int sign = ((!mpc2.isRotationReversed()) ? 1 : (-1));
			if (capi.World.Player.CameraMode != EnumCameraMode.Overhead)
			{
				capi.Input.MouseYaw += frameTime2 * mpc2.TrueSpeed * 2.5f * (float)sign;
			}
			capi.World.Player.Entity.BodyYaw += frameTime2 * mpc2.TrueSpeed * 2.5f * (float)sign;
			capi.World.Player.Entity.WalkYaw += frameTime2 * mpc2.TrueSpeed * 2.5f * (float)sign;
			capi.World.Player.Entity.Pos.Yaw += frameTime2 * mpc2.TrueSpeed * 2.5f * (float)sign;
		}
	}
}
