using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockSupportBeam : Block
{
	private ModSystemSupportBeamPlacer bp;

	public bool PartialEnds;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		bp = api.ModLoader.GetModSystem<ModSystemSupportBeamPlacer>();
		PartialSelection = true;
		PartialEnds = Attributes?["partialEnds"].AsBool() ?? false;
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		BEBehaviorSupportBeam be = api.World.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorSupportBeam>();
		if (be != null)
		{
			return be.GetCollisionBoxes();
		}
		return base.GetSelectionBoxes(blockAccessor, pos);
	}

	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		BEBehaviorSupportBeam be = api.World.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorSupportBeam>();
		if (be != null)
		{
			return be.GetCollisionBoxes();
		}
		return base.GetCollisionBoxes(blockAccessor, pos);
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		if (blockSel != null)
		{
			handling = EnumHandHandling.PreventDefault;
			bp.OnInteract(this, slot, byEntity, blockSel, PartialEnds);
		}
	}

	public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
	{
		if (bp.CancelPlace(this, byEntity))
		{
			handling = EnumHandHandling.PreventDefault;
		}
		else
		{
			base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
		}
	}

	public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
	{
		BEBehaviorSupportBeam be = api.World.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorSupportBeam>();
		if (be != null)
		{
			int beamIndex = ((api as ICoreClientAPI)?.World.Player?.CurrentBlockSelection?.SelectionBoxIndex).GetValueOrDefault();
			if (beamIndex < be.Beams.Length)
			{
				blockModelData = be.genMesh(beamIndex, null, null);
				decalModelData = be.genMesh(beamIndex, decalTexSource, "decal");
			}
		}
		else
		{
			base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
		}
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		int? beamIndex = byPlayer?.CurrentBlockSelection?.SelectionBoxIndex;
		BEBehaviorSupportBeam be = api.World.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorSupportBeam>();
		if (beamIndex.HasValue && be != null && be.Beams.Length > 1)
		{
			be.BreakBeam(beamIndex.Value, byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative);
		}
		else
		{
			base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
		}
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		return false;
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return new WorldInteraction[3]
		{
			new WorldInteraction
			{
				ActionLangCode = "Set Beam Start/End Point (Snap to 4x4 grid)",
				MouseButton = EnumMouseButton.Right
			},
			new WorldInteraction
			{
				ActionLangCode = "Set Beam Start/End Point (Snap to 16x16 grid)",
				MouseButton = EnumMouseButton.Right,
				HotKeyCode = "sprint"
			},
			new WorldInteraction
			{
				ActionLangCode = "Cancel placement",
				MouseButton = EnumMouseButton.Left
			}
		};
	}

	public override bool DisplacesLiquids(IBlockAccessor blockAccess, BlockPos pos)
	{
		return false;
	}
}
