using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf;

public class HudElementInteractionHelp : HudElement
{
	private Block currentBlock;

	private int currentBlockSelectionIndex;

	private Entity currentEntity;

	private Vec3d currentPos;

	private DrawWorldInteractionUtil wiUtil;

	private int entityInViewCount;

	private int entitySelectionBoxIndex = -1;

	private bool wasAlive;

	private ICustomInteractionHelpPositioning cp;

	private bool customCurrentPosSet;

	public override string ToggleKeyCombinationCode => "blockinteractionhelp";

	public override double DrawOrder => 0.05;

	public HudElementInteractionHelp(ICoreClientAPI capi)
		: base(capi)
	{
		wiUtil = new DrawWorldInteractionUtil(capi, Composers, "-placedBlock");
		(capi.World as ClientMain).eventManager?.RegisterPlayerPropertyChangedWatcher(EnumProperty.PlayerPosDiv8, PlayerPosDiv8Changed);
		capi.Event.RegisterGameTickListener(Every15ms, 15);
		capi.Event.BlockChanged += OnBlockChanged;
		ComposeBlockWorldInteractionHelp();
		ClientSettings.Inst.AddWatcher("showBlockInteractionHelp", delegate(bool on)
		{
			if (on)
			{
				TryOpen();
			}
			else
			{
				TryClose();
			}
		});
		if (ClientSettings.ShowBlockInteractionHelp)
		{
			TryOpen();
		}
	}

	private void ComposeBlockWorldInteractionHelp()
	{
		if (IsOpened())
		{
			WorldInteraction[] wis = getWorldInteractions();
			wiUtil.ComposeBlockWorldInteractionHelp(wis);
		}
	}

	private WorldInteraction[] getWorldInteractions()
	{
		if (currentBlock != null)
		{
			EntityPos plrpos = capi.World.Player.Entity.Pos;
			BlockSelection bs = capi.World.Player.CurrentBlockSelection;
			if (bs == null || plrpos.XYZ.AsBlockPos.DistanceTo(bs.Position) > 8f)
			{
				return null;
			}
			return currentBlock.GetPlacedBlockInteractionHelp(capi.World, bs, capi.World.Player);
		}
		if (currentEntity != null)
		{
			EntityPos plrpos2 = capi.World.Player.Entity.Pos;
			EntitySelection es = capi.World.Player.CurrentEntitySelection;
			if (es == null || plrpos2.XYZ.AsBlockPos.DistanceTo(es.Position.AsBlockPos) > 8f)
			{
				return null;
			}
			return es.Entity.GetInteractionHelp(capi.World, es, capi.World.Player);
		}
		return null;
	}

	private void Every15ms(float dt)
	{
		if (!IsOpened())
		{
			return;
		}
		if (capi.World.Player.CurrentEntitySelection == null)
		{
			currentEntity = null;
			if (capi.World.Player.CurrentBlockSelection == null)
			{
				currentBlock = null;
			}
			else
			{
				BlockInView();
			}
		}
		else
		{
			EntityInView();
		}
	}

	private void BlockInView()
	{
		BlockSelection bs = capi.World.Player.CurrentBlockSelection;
		Block block;
		if (bs.DidOffset)
		{
			BlockFacing facing = bs.Face.Opposite;
			block = capi.World.BlockAccessor.GetBlockOnSide(bs.Position, facing);
		}
		else
		{
			block = capi.World.BlockAccessor.GetBlock(bs.Position);
		}
		if (block.BlockId == 0)
		{
			currentBlock = null;
		}
		else if (block != currentBlock || (int)currentPos.X != bs.Position.X || (int)currentPos.Y != bs.Position.Y || (int)currentPos.Z != bs.Position.Z || bs.SelectionBoxIndex != currentBlockSelectionIndex)
		{
			currentBlockSelectionIndex = bs.SelectionBoxIndex;
			currentBlock = block;
			currentEntity = null;
			currentPos = bs.Position.ToVec3d().Add(0.5, block.InteractionHelpYOffset, 0.5);
			if (currentBlock.RandomDrawOffset != 0)
			{
				currentPos.X += (float)(GameMath.oaatHash(bs.Position.X, 0, bs.Position.Z) % 12) / (24f + 12f * (float)currentBlock.RandomDrawOffset);
				currentPos.Z += (float)(GameMath.oaatHash(bs.Position.X, 1, bs.Position.Z) % 12) / (24f + 12f * (float)currentBlock.RandomDrawOffset);
			}
			ComposeBlockWorldInteractionHelp();
		}
	}

	private void EntityInView()
	{
		Entity nowEntity = capi.World.Player.CurrentEntitySelection.Entity;
		int nowSeleBox = capi.World.Player.CurrentEntitySelection.SelectionBoxIndex;
		if (entitySelectionBoxIndex != nowSeleBox || nowEntity != currentEntity || wasAlive != nowEntity?.Alive || entityInViewCount++ > 20)
		{
			entityInViewCount = 0;
			wasAlive = nowEntity.Alive;
			currentEntity = nowEntity;
			currentBlock = null;
			entitySelectionBoxIndex = nowSeleBox;
			cp = nowEntity.GetInterface<ICustomInteractionHelpPositioning>();
			ComposeBlockWorldInteractionHelp();
		}
	}

	public override void OnRenderGUI(float deltaTime)
	{
		if (capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Spectator)
		{
			return;
		}
		ElementBounds bounds = wiUtil.Composer?.Bounds;
		if (currentEntity != null)
		{
			_ = customCurrentPosSet;
			if (cp != null)
			{
				currentPos = cp.GetInteractionHelpPosition();
				customCurrentPosSet = currentPos != null;
			}
			if (cp == null || currentPos == null)
			{
				double offX = currentEntity.SelectionBox.X2 - currentEntity.OriginSelectionBox.X2;
				double offZ = currentEntity.SelectionBox.Z2 - currentEntity.OriginSelectionBox.Z2;
				currentPos = currentEntity.ServerPos.XYZ.Add(offX, currentEntity.SelectionBox.Y2, offZ);
			}
		}
		if (bounds != null)
		{
			Vec3d pos = MatrixToolsd.Project(currentPos, capi.Render.PerspectiveProjectionMat, capi.Render.PerspectiveViewMat, capi.Render.FrameWidth, capi.Render.FrameHeight);
			if (pos.Z < 0.0)
			{
				return;
			}
			bounds.Alignment = EnumDialogArea.None;
			bounds.fixedOffsetX = 0.0;
			bounds.fixedOffsetY = 0.0;
			bounds.absFixedX = pos.X - wiUtil.ActualWidth / 2.0;
			bounds.absFixedY = (double)capi.Render.FrameHeight - pos.Y - bounds.OuterHeight * 0.8;
			bounds.absMarginX = 0.0;
			bounds.absMarginY = 0.0;
		}
		if ((capi.World as ClientMain).MouseGrabbed)
		{
			if (cp == null || cp.TransparentCenter)
			{
				capi.Render.CurrentActiveShader.Uniform("transparentCenter", 1);
			}
			base.OnRenderGUI(deltaTime);
			capi.Render.CurrentActiveShader.Uniform("transparentCenter", 0);
		}
	}

	private void PlayerPosDiv8Changed(TrackedPlayerProperties oldValues, TrackedPlayerProperties newValues)
	{
		ComposeBlockWorldInteractionHelp();
	}

	public override bool ShouldReceiveRenderEvents()
	{
		if (currentBlock == null)
		{
			return currentEntity != null;
		}
		return true;
	}

	public override bool ShouldReceiveKeyboardEvents()
	{
		return false;
	}

	public override bool ShouldReceiveMouseEvents()
	{
		return false;
	}

	private void OnBlockChanged(BlockPos pos, Block oldBlock)
	{
		IPlayer player = capi.World.Player;
		if (player?.CurrentBlockSelection != null && pos.Equals(player.CurrentBlockSelection.Position))
		{
			ComposeBlockWorldInteractionHelp();
		}
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
		ClientSettings.ShowBlockInteractionHelp = true;
	}

	public override void OnGuiClosed()
	{
		base.OnGuiClosed();
		ClientSettings.ShowBlockInteractionHelp = false;
	}

	public override void Dispose()
	{
		base.Dispose();
		wiUtil?.Dispose();
	}
}
