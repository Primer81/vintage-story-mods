using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf;

public class HudElementBlockAndEntityInfo : HudElement
{
	private Block currentBlock;

	private int currentSelectionIndex;

	private Entity currentEntity;

	private BlockPos currentPos;

	private string title;

	private string detail;

	private GuiComposer composer;

	public override string ToggleKeyCombinationCode => "blockinfohud";

	public HudElementBlockAndEntityInfo(ICoreClientAPI capi)
		: base(capi)
	{
		capi.Event.RegisterGameTickListener(Every15ms, 15);
		capi.Event.RegisterGameTickListener(Every500ms, 500);
		capi.Event.BlockChanged += OnBlockChanged;
		ComposeBlockInfoHud();
		if (ClientSettings.ShowBlockInfoHud)
		{
			TryOpen();
		}
		ClientSettings.Inst.AddWatcher("showBlockInfoHud", delegate(bool on)
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
	}

	private void ComposeBlockInfoHud()
	{
		string newTitle = "";
		string newDetail = "";
		if (currentBlock != null)
		{
			if (currentBlock.Code == null)
			{
				newTitle = "Unknown block ID " + capi.World.BlockAccessor.GetBlockId(currentPos);
				newDetail = "";
			}
			else
			{
				newTitle = currentBlock.GetPlacedBlockName(capi.World, currentPos);
				newDetail = currentBlock.GetPlacedBlockInfo(capi.World, currentPos, capi.World.Player);
				if (newDetail == null)
				{
					newDetail = "";
				}
				if (newTitle == null)
				{
					newTitle = "Unknown";
				}
			}
		}
		if (currentEntity != null)
		{
			newTitle = currentEntity.GetName();
			newDetail = currentEntity.GetInfoText();
			if (newDetail == null)
			{
				newDetail = "";
			}
			if (newTitle == null)
			{
				newTitle = "Unknown Entity code " + currentEntity.Code;
			}
		}
		if (!(title == newTitle) || !(detail == newDetail))
		{
			title = newTitle;
			detail = newDetail;
			ElementBounds textBounds = ElementBounds.Fixed(EnumDialogArea.CenterFixed, 0.0, 0.0, 500.0, 24.0);
			ElementBounds detailTextBounds = textBounds.BelowCopy(0.0, 10.0);
			detailTextBounds.Alignment = EnumDialogArea.None;
			ElementBounds overlayBounds = new ElementBounds();
			overlayBounds.BothSizing = ElementSizing.FitToChildren;
			overlayBounds.WithFixedPadding(5.0, 5.0);
			ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterTop).WithFixedAlignmentOffset(0.0, GuiStyle.DialogToScreenPadding);
			LoadedTexture reuseRichTextTexture = null;
			GuiElementRichtext rtElem;
			if (composer == null)
			{
				composer = capi.Gui.CreateCompo("blockinfohud", dialogBounds);
			}
			else
			{
				rtElem = composer.GetRichtext("rt");
				reuseRichTextTexture = rtElem.richtTextTexture;
				rtElem.richtTextTexture = null;
				composer.Clear(dialogBounds);
			}
			Composers["blockinfohud"] = composer;
			composer.AddGameOverlay(overlayBounds).BeginChildElements(overlayBounds).AddStaticTextAutoBoxSize(title, CairoFont.WhiteSmallishText(), EnumTextOrientation.Left, textBounds)
				.AddRichtext(detail, CairoFont.WhiteDetailText(), detailTextBounds, "rt")
				.EndChildElements();
			rtElem = composer.GetRichtext("rt");
			if (detail.Length == 0)
			{
				detailTextBounds.fixedY = 0.0;
				detailTextBounds.fixedHeight = 0.0;
			}
			if (reuseRichTextTexture != null)
			{
				rtElem.richtTextTexture = reuseRichTextTexture;
			}
			rtElem.BeforeCalcBounds();
			detailTextBounds.fixedWidth = Math.Min(500.0, rtElem.MaxLineWidth / (double)RuntimeEnv.GUIScale + 1.0);
			composer.Compose();
		}
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
				return;
			}
			currentEntity = null;
			BlockInView();
		}
		else
		{
			currentBlock = null;
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
		else if (block != currentBlock || !currentPos.Equals(bs.Position) || currentSelectionIndex != bs.SelectionBoxIndex)
		{
			currentBlock = block;
			currentSelectionIndex = bs.SelectionBoxIndex;
			currentPos = (bs.DidOffset ? bs.Position.Copy().Add(bs.Face.Opposite) : bs.Position.Copy());
			ComposeBlockInfoHud();
		}
	}

	private void EntityInView()
	{
		Entity nowEntity = capi.World.Player.CurrentEntitySelection.Entity;
		if (nowEntity != currentEntity)
		{
			currentEntity = nowEntity;
			ComposeBlockInfoHud();
		}
	}

	public override bool ShouldReceiveRenderEvents()
	{
		if (currentBlock == null)
		{
			return currentEntity != null;
		}
		return true;
	}

	private void OnBlockChanged(BlockPos pos, Block oldBlock)
	{
		IPlayer player = capi.World.Player;
		if (player?.CurrentBlockSelection != null && pos.Equals(player.CurrentBlockSelection.Position))
		{
			ComposeBlockInfoHud();
		}
	}

	private void Every500ms(float dt)
	{
		Every15ms(dt);
		ComposeBlockInfoHud();
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
		ClientSettings.ShowBlockInfoHud = true;
	}

	public override void OnGuiClosed()
	{
		base.OnGuiClosed();
		ClientSettings.ShowBlockInfoHud = false;
	}

	public override void Dispose()
	{
		base.Dispose();
		composer?.Dispose();
	}
}
